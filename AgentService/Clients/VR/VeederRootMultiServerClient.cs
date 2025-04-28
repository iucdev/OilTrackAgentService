using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Opc.Cpx;
using Service.Clients.Client;
using Service.Clients.RSMDB;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Common;
using Sunp.Api.Client;
using static System.Net.Mime.MediaTypeNames;
using static Dapper.SqlMapper;

namespace Service.Clients.VR {
    public class VeederRootMultiServerClient : AMultiServerClient {
        public VeederRootMultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker) {
            ObjectSettings = objectSettings;
            Worker = worker;
            Reconnect();
        }

        protected override void Reconnect() {
            Logger.Error("Reconnecting...");
            StopCollection();

            if (Connect()) {
                StartCollection();
            } else {
                Logger.Warn("Не удалось подключиться. Повтор через 60 секунд.");
                Thread.Sleep(60000);
                Reconnect();
            }
        }

        protected override void CollectData() {
            Logger.Debug("Начало сбора данных.");

            var tanksMeasurements = new List<TankMeasurements>();
            var tanksTransfers = new List<TankTransfers>();

            foreach (var source in ObjectSettings.Objects.First().ObjectSources) {
                try {
                    if (int.TryParse(source.InternalId, out int tankNumber)) {
                        Logger.Info($"Запрос данных для резервуара №{tankNumber}");

                        var measurements = Send231(source);
                        if (measurements != null) {
                            tanksMeasurements.Add(measurements);
                        }

                        Thread.Sleep(500);
                        Logger.Debug($"End Collecting Tank Measurements Tank Number {source.InternalId} (i231)");
                        Logger.Debug("-------------------------------------------------------------------------------------------");
                        var transfers = Send215(source);
                        if (transfers != null) {
                            tanksTransfers.Add(transfers);
                        }
                        Logger.Debug($"End Collecting Tank Transfer Tank Number {source.InternalId} (i215)");
                        Logger.Debug("-------------------------------------------------------------------------------------------");
                        Thread.Sleep(500);
                    }
                } catch (Exception ex) {
                    Logger.Error($"Ошибка при сборе данных: {ex}");
                }
            }

            QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
            QueueTaskService.Instance.SaveTransfersAsTask(tanksTransfers.ToArray());
            StopCollection();
            Logger.Debug($"Closing connection after collecting data");
        }

        protected TankMeasurements Send231(ObjectSource objectSource, string password = null) {
            string internalTankNumber = objectSource.InternalId;
            char SOH = (char)0x01;
            string command = string.IsNullOrEmpty(password)
                ? $"{SOH}i2310{internalTankNumber}"
                : $"{SOH}{password}i2310{internalTankNumber}";

            if (SendCommand(command, out var response)) {
                return Fill231(objectSource, response);
            }

            return null;
        }

        protected TankMeasurements Fill231(ObjectSource objectSource, string response) {
            /*
                Typical Response Message
                <SOH>i231TTYYMMDDHHmmTTpssssNNFFFFFFFF...
                                     TTpssssNNFFFFFFFF&&CCCC<ETX>

                1. YYMMDDHHmm - Current Date and Time
                2. TT - Tank Number (Decimal, 00=all)
                3. p - Product Code (single ASCII character [20h-7Eh])
                4. ssss - Tank Status Bits:
                Bit 1=(LSB) Delivery in Progress
                Bit 2=Leak Test in Progress
                Bit 3=Invalid Fuel Height Alarm (MAG Probes Only)
                Bit 4-16 - Unused
                5. NN - Number of eight character Data Fields to follow (Hex)
                6. FFFFFFFF - ASCII Hex IEEE float:
                1. Volume
                2. TC Volume
                3. Ullage
                4. Height
                5. Water
                6. Temperature
                7. Water Volume
                8. Full Volume
                9. Net Volume 10. TC Net Volume 11. Mass 12. Density 13. TC Density
                7. && - Data Termination Flag
                8. CCCC - Message Checksum
            */

            //Deviding response on usefull data and checksum by Data Termination Flag
            response = response.Split(new[] { "&&" }, StringSplitOptions.None)[0];
            // Removing command 'i231TT'
            var command = response.Substring(0, 7);
            Logger.Debug($"Command: {command} (Excpecting 231)");
            response = response.Substring(7);

            var tankMeasurementDataList = new List<TankMeasurementData>();

            try {
                //1.YYMMDDHHmm - Current Date and Time
                var currentDateRaw = response.Substring(0, 10);
                Logger.Debug("CurrentDateTime: {0} ({1})", ParseDateTime(currentDateRaw), currentDateRaw);
                response = response.Substring(10);
                //2.TT - Tank Number(Decimal, 00 = all)
                var tankNumberRaw = response.Substring(0, 2);
                Logger.Debug("TankNumber: {0}", tankNumberRaw);
                response = response.Substring(2);
                //3.p - Product Code(single ASCII character[20h - 7Eh])
                var productCodeRaw = response.Substring(0, 1);
                Logger.Debug("ProductCode: {0}", productCodeRaw);
                response = response.Substring(1);
                //4.ssss - Tank Status Bits:
                //    Bit 1 = (LSB)Delivery in Progress
                //    Bit 2 = Leak Test in Progress
                //    Bit 3 = Invalid Fuel Height Alarm(MAG Probes Only)
                //    Bit 4 - 16 - Unused
                var tankStatusBitsRaw = response.Substring(0, 4);
                Logger.Debug("TankStatusBitsRaw: {0}", tankStatusBitsRaw);
                response = response.Substring(4);

                response = GetTankMeasurementData(response, ParseDateTime(currentDateRaw), out var tankMeasurementData);

                tankMeasurementDataList.Add(tankMeasurementData);
            } catch (Exception ex) {
                Logger.Error(ex);
                response = "";
            }

            var tankMeasurements = new TankMeasurements() {
                TankId = objectSource.ExternalId.Value,
                Measurements = tankMeasurementDataList.SetEnums(objectSource).ToArray()
            };

            return tankMeasurements;
        }

        static string GetTankMeasurementData(string response, DateTime measutementDate, out TankMeasurementData tankMeasurementData) {
            tankMeasurementData = new TankMeasurementData() {
                MeasurementDate = measutementDate,
                
            };
            //5.NN - Number of eight character Data Fields to follow(Hex)
            var nnRaw = response.Substring(0, 2);
            int nn = int.Parse(nnRaw, NumberStyles.HexNumber);
            Logger.Debug($"Number of eight character Data Fields to follow: {nn}");
            response = response.Substring(2);

            for (int i = 0; i < nn; i++) {
                //0.Volume
                //1.TC Volume
                //2.Ullage
                //3.Height
                //4.Water
                //5.Temperature
                //6.Water Volume
                //7.Full Volume
                //8.Net Volume
                //9.TC Net Volume
                //10.Mass
                //11.Density
                //12.TC Density
                string hexFieldRaw = response.Substring(0, 8);
                var decodedValue = HexToFloat(hexFieldRaw);
                var result = Convert.ToDecimal(decodedValue);
                switch (i) {
                    case 0: {
                            tankMeasurementData.Volume = result;
                            Logger.Debug($"tankMeasurementData.Volume: {tankMeasurementData.Volume}");
                            break;
                        }
                    case 3: {
                            tankMeasurementData.Level = result;
                            Logger.Debug($"tankMeasurementData.Level: {tankMeasurementData.Level}");
                            break;
                        }
                    case 5: {
                            tankMeasurementData.Temperature = result;
                            Logger.Debug($"tankMeasurementData.Temperature: {tankMeasurementData.Temperature}");
                            break;
                        }
                    case 10: {
                            tankMeasurementData.Mass = result;
                            Logger.Debug($"tankMeasurementData.Mass: {tankMeasurementData.Mass}");
                            break;
                        }
                    case 11: {
                            tankMeasurementData.Density = result;
                            Logger.Debug($"tankMeasurementData.Density: {tankMeasurementData.Density}");
                            break;
                        }
                }
                response = response.Substring(8);
            }

            return response;
        }

        protected TankTransfers Send215(ObjectSource objectSource, string password = null) {
            string internalTankNumber = objectSource.InternalId;
            char SOH = (char)0x01;
            string command = string.IsNullOrEmpty(password)
                ? $"{SOH}i2150{internalTankNumber}"
                : $"{SOH}{password}i2150{internalTankNumber}";

            if(SendCommand(command, out var response)) {
                return Fill215(objectSource, response);
            }

            return null;
        }

        protected TankTransfers Fill215(ObjectSource objectSource, string response) {
            /*
                Typical Response Message
                <SOH>i215TTYYMMDDHHmmTTpddYYMMDDHHmmYYMMDDHHmmNNFFFFFFFFf...
                                     TTpddYYMMDDHHmmYYMMDDHHmmNNFFFFFFFFf&&CCCC<ETX>
                1.YYMMDDHHmm - Current Date and Time
                2.TT - Tank Number(Decimal, 00 = all)
                3.p - Product Code(single ASCII character[20h - 7Eh])
                4.dd - Number of Deliveries to follow(Decimal, 00 = no data)
                5.YYMMDDHHmm - Starting Date / Time
                6.YYMMDDHHmm - Ending Date / Time
                7.NN - Number of eight character Data Fields to follow(Hex)
                8.FFFFFFFF - ASCII Hex IEEE float:
                1.Starting Volume
                2.Starting Mass
                3.Starting Density
                4.Starting Water
                5.Starting Temp
                6.Ending Volume
                7.Ending Mass
                8.Ending Density
                9.Ending Water
                10.Ending Temp
                11.Starting Height
                12.Ending Height
                13.Starting TC Density
                14.Ending TC Density
                15.Starting TC Volume
                16.Ending TC Volume
                17.Starting Total TC Density Offset
                18.Ending Total TC Density Offset
                9.f - Default Density Flag(0 = new value, 1 = default)
                10. && -Data Termination Flag
                11.CCCC - Message Checksum
            */

            //Deviding response on usefull data and checksum by Data Termination Flag
            response = response.Split(new[] { "&&" }, StringSplitOptions.None)[0];
            // Removing command 'i215TT'
            var command = response.Substring(0, 7);
            Logger.Debug($"Command: {command} (Excpecting 215)");
            response = response.Substring(7);

            var transferDataList = new List<TankTransferData>();
            
            try {
                //1.YYMMDDHHmm - Current Date and Time
                var currentDateRaw = response.Substring(0, 10);
                Logger.Debug("CurrentDateTime: {0} ({1})", ParseDateTime(currentDateRaw), currentDateRaw);
                response = response.Substring(10);
                //2.TT - Tank Number(Decimal, 00 = all)
                var tankNumberRaw = response.Substring(0, 2);
                Logger.Debug("TankNumber: {0}", tankNumberRaw);
                response = response.Substring(2);
                //3.p - Product Code(single ASCII character[20h - 7Eh])
                var productCodeRaw = response.Substring(0, 1);
                Logger.Debug("ProductCode: {0}", productCodeRaw);
                response = response.Substring(1);
                //4.dd - Number of Deliveries to follow(Decimal, 00 = no data)
                var numberOfDeliveriesRaw = response.Substring(0, 2);
                Logger.Debug("Number of deliveries to follow: {0}", numberOfDeliveriesRaw);
                response = response.Substring(2);

                for(var i = 0; i < Convert.ToInt32(numberOfDeliveriesRaw); i++) {
                    response = GetTankTransferData(response, out var tankTransferData);
                    transferDataList.Add(tankTransferData);
                    Logger.Debug($"Number of parsed deliveries {transferDataList.Count()}");
                }

            } catch(Exception ex) {
                Logger.Error(ex);
            }

            var tankTransfers = new TankTransfers() {
                TankId = objectSource.ExternalId.Value,
                Transfers = transferDataList.SetEnums(objectSource).ToArray()
            };

            return tankTransfers;
        }

        static string GetTankTransferData(string response, out TankTransferData tankTransferData) {
            tankTransferData = new TankTransferData() { };

            //5.YYMMDDHHmm - Starting Date / Time
            var startingDateRaw = response.Substring(0, 10);
            tankTransferData.StartDate = ParseDateTime(startingDateRaw);
            Logger.Debug($"Starting Date / Time: {ParseDateTime(startingDateRaw)}");
            response = response.Substring(10);
            //6.YYMMDDHHmm - Ending Date / Time
            var endingDateRaw = response.Substring(0, 10);
            tankTransferData.EndDate = ParseDateTime(endingDateRaw);
            Logger.Debug($"Ending Date / Time: {ParseDateTime(endingDateRaw)}");
            response = response.Substring(10);
            //7.NN - Number of eight character Data Fields to follow(Hex)
            var nnRaw = response.Substring(0, 2);
            int nn = int.Parse(nnRaw, NumberStyles.HexNumber);
            Logger.Debug($"Number of eight character Data Fields to follow: {nn}");
            response = response.Substring(2);


            for (int i = 0; i < nn; i++) {
                //0.Starting Volume
                //1.Starting Mass
                //2.Starting Density
                //3.Starting Water
                //4.Starting Temp
                //5.Ending Volume
                //6.Ending Mass
                //7.Ending Density
                //8.Ending Water
                //9.Ending Temp
                //10.Starting Height
                //11.Ending Height
                //12.Starting TC Density
                //13.Ending TC Density
                //14.Starting TC Volume
                //15.Ending TC Volume
                //16.Starting Total TC Density Offset
                //17.Ending Total TC Density Offset
                string hexFieldRaw = response.Substring(0, 8);
                var decodedValue = HexToFloat(hexFieldRaw);
                var result = Convert.ToDecimal(decodedValue);
                switch (i) {
                    case 0: tankTransferData.VolumeStart = result; break;
                    case 1: tankTransferData.MassStart = result; break;
                    case 5: tankTransferData.VolumeEnd = result; break;
                    case 6: tankTransferData.MassEnd = result; break;
                    case 10: tankTransferData.LevelStart = result; break;
                    case 11: tankTransferData.LevelEnd = result; break;
                }
                response = response.Substring(8);
            }

            var densityFlag = response.Substring(0, 1);
            Logger.Debug("Density Flag: {0}", (densityFlag == "0" ? "New Value" : "Default"));
            response = response.Substring(1);

            return response;
        }

        static float HexToFloat(string hex) {
            if (hex.Length != 8) return float.NaN;

            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++) {
                bytes[3 - i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            return BitConverter.ToSingle(bytes, 0);
        }

        static DateTime ParseDateTime(string datetime) {
            try {
                if (datetime.Length != 10)
                    throw new Exception("Invalid DateTime");

                int year = Convert.ToInt32("20" + datetime.Substring(0, 2));
                int month = Convert.ToInt32(datetime.Substring(2, 2));
                int day = Convert.ToInt32(datetime.Substring(4, 2));
                int hour = Convert.ToInt32(datetime.Substring(6, 2));
                int minute = Convert.ToInt32(datetime.Substring(8, 2));

                return new DateTime(year, month, day, hour, minute, 0);
            } catch (Exception e) {
                throw new NotImplementedException($"Необработанная дата {datetime}");    
            }
        }
    }
}

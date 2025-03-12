using Newtonsoft.Json;
using NLog;
using Service.Clients.Client;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Common;
using Service.Enums;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Clients.PV4 {
    public class Pv4MultiServerClient : AMultiServerClient2 {
        private List<TankMeasurementDataPv4Dto> _productWeightSourceValue = new List<TankMeasurementDataPv4Dto>();
        private List<TankMeasurementDataPv4Dto> _tankLevelsSourceValue = new List<TankMeasurementDataPv4Dto>();
        private List<TankTransferData> _tankTransfers = new List<TankTransferData>();

        private decimal getDecimal(string val, Logger logger) {
            try {
                return Convert.ToDecimal(val, CultureInfo.InvariantCulture);
            } catch (Exception ex) {
                logger.Error("Error on getDecimal: from string={0}", val);
                throw ex;
            }
        }

        public Pv4MultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker) {
            ObjectSettings = objectSettings;
            Worker = worker;
            Init();
        }

        private new void Init() {
            Logger.Debug("Start Init");
            base.Init();
            try {
                var counter = 0;
                while (!Connect()) {
                    if (Worker.CancellationPending) return;

                    // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, Task.ObjectId);

                    Logger.Info("Try connect to server {0}", counter++);
                    Thread.Sleep(60000);
                }

                SSVersionCMD();
            } catch (Exception e) {
                Logger.Error(e);
            }
        }

        protected override void Reconnect() {
            Logger.Error("Reconnecting...");
            Init();
            timerJob(null, null);
            _timer.Start();
        }

        private TankMeasurements collectMeasurements(ObjectSource source) {
            try {
                Logger.Debug("Collect measurements");

                var tankMeasurements = new TankMeasurements { TankId = source.ExternalId.Value };
                var tankNumber = string.Format("{0}{0}", source.InternalId.PadLeft(2, '0'));

                //Level 6C
                if (TankLevelsExtCMD(tankNumber) <= 0) {
                    Logger.Debug("TankLevelsCMD return False");
                    return null;
                }
                if (_tankLevelsSourceValue.Count <= 0) {
                    Logger.Debug("count of _tankLevelsSourceValue is 0");
                    return null;
                }

                //Weight 77
                if (ProductWeightExtCMD(tankNumber) <= 0) {
                    Logger.Debug("ProductWeightExtCMD return False");
                    return null;
                }
                if (_productWeightSourceValue.Count <= 0) {
                    Logger.Debug("count of _productWeightSourceValue is 0");
                    return null;
                }

                if (_tankLevelsSourceValue.Count != _productWeightSourceValue.Count) {
                    Logger.Error($"Count of _tankLevelsSourceValue({_tankLevelsSourceValue.Count}) != _productWeightSourceValue({_productWeightSourceValue.Count}). Something is wrong here. Please, check!");
                }

                foreach (var value in _tankLevelsSourceValue) {
                    var pr = _productWeightSourceValue.FirstOrDefault(p => p.SourceName == value.SourceName);
                    if (pr == null) {
                        continue;
                    }

                    value.ExternalId = source.ExternalId.Value;
                    value.ObjectSource = source;
                    value.Data.Mass = pr.Data.Mass;

                    ProductDensityCMD(value.SourceName.PadLeft(2, '0'));
                    var tempd = GetValue(13, 10);
                    value.Data.Density = tempd.HasValue ? getDecimal((tempd.Value * 0.000001d).ToString(CultureInfo.InvariantCulture), Logger) : 0m;

                    Logger.Info("Tank {0,2}| Lev {1,8}| Vol {2,5}| Temp {3,4}| Mass {4,8}| Dens {5,11}",
                        value.SourceName, value.Data.Level, value.Data.Volume, value.Data.Temperature, value.Data.Mass, value.Data.Density);
                }

                tankMeasurements.Measurements = _tankLevelsSourceValue.Select(t => t.Data).ToList().SetEnums(source);
                return tankMeasurements;
            } catch (Exception ex) {
                Logger.Error($"Error on collect measurements: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        private TankTransfers collectTransfers(ObjectSource source) {
            try {
                Logger.Debug("Collect transfers");
                var tankTransfers = new TankTransfers {
                    TankId = source.ExternalId.Value
                };
                var tankNumber = source.InternalId.PadLeft(2, '0');
                for (int i = 1; i <= 10; i++) {
                    tankNumber = source.InternalId.PadLeft(2, '0') + i.ToString().PadLeft(2, '0');
                    if (DeliveryEnquiryExt(tankNumber) == 0) {
                        _tankTransfers = null;
                    }
                    if (_tankTransfers != null && _tankTransfers.Count > 0) {
                        tankTransfers.Transfers = _tankTransfers.Where(t => t.EndDate != null).ToArray().SetEnums(source);
                    }
                }
                return tankTransfers;
            } catch (Exception ex) {
                Logger.Error($"Error on collect transfers: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        protected override void CollectData() {
            try {
                Logger.Debug("Collect data");
                foreach (var @object in ObjectSettings.Objects) {
                    var tanksMeasurements = new List<TankMeasurements>();
                    var tanksTransfers = new List<TankTransfers>();

                    foreach (var source in @object.ObjectSources) {
                        _productWeightSourceValue.Clear();
                        _tankLevelsSourceValue.Clear();
                        _tankTransfers.Clear();

                        var measurements = collectMeasurements(source);
                        if (measurements != null) {
                            tanksMeasurements.Add(measurements);
                        }
                        _tankLevelsSourceValue.Clear();
                    }

                    foreach (var source in @object.ObjectSources) {
                        _productWeightSourceValue.Clear();
                        _tankLevelsSourceValue.Clear();
                        _tankTransfers.Clear();

                        var transfers = collectTransfers(source);
                        if (transfers != null) {
                            tanksTransfers.Add(transfers);
                        }
                        _tankTransfers.Clear();
                    }

                    QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
                    QueueTaskService.Instance.SaveTransfersAsTask(tanksTransfers.ToArray());
                }
            } catch (Exception ex) {
                Logger.Error($"Error on collect data {ex.Message + ex.StackTrace}");
            }
        }

        protected override bool Fill(int bytesRec) {
            var result = false;

            switch (LastCom) {
                case Pv4Cmd.SsVersion: {
                        result = FilllSSVersion();
                        break;
                    }
                case Pv4Cmd.ProductWeight: {
                        _productWeightSourceValue = FillProductWeight();
                        if (_productWeightSourceValue.Count > 0) result = true;
                        break;
                    }
                case Pv4Cmd.ProductWeightExt: {
                        _productWeightSourceValue = FillProductWeightExt();
                        if (_productWeightSourceValue.Count > 0) result = true;
                        break;
                    }
                case Pv4Cmd.TankLevels: {
                        _tankLevelsSourceValue = FillTankLevels();
                        if (_tankLevelsSourceValue.Count > 0) result = true;
                        break;
                    }
                case Pv4Cmd.TankLevelsExt: {
                        _tankLevelsSourceValue = FillTankLevelsExt();
                        if (_tankLevelsSourceValue.Count > 0) result = true;
                        break;
                    }
                case Pv4Cmd.DeliveryEnquiry: {
                        _tankTransfers = FillDeliveryEnquiry2();
                        if (_tankTransfers != null && _tankTransfers.Count > 0) result = true;
                        break;
                    }
                case Pv4Cmd.DeliveryEnquiryExt: {
                        _tankTransfers = FillDeliveryEnquiryExt2();
                        if (_tankTransfers != null && _tankTransfers.Count > 0) result = true;
                        break;
                    }
            }

            return result;
        }

        protected override bool SkipCmd() {
            return false;
        }

        protected override bool CheckRb(int bytesRec) {
            if (buf[0] != Pv4Cmd.STX) {
                Logger.Error("---------------------= Error STX =--------------------------");
                return false;
            }

            if (buf[bytesRec - 2] != Pv4Cmd.ETX) {
                Logger.Error("---------------------= Error ETX =--------------------------");
                return false;
            }

            if (!Bcc(bytesRec)) {
                Logger.Error("---------------------= Error BCC =--------------------------");
                return false;
            }

            return true;
        }

        private new int? GetValue(int nom, int kol) {
            int? result = null;

            for (var i = 1; i <= kol; i++) {
                if (buf[i - 1 + nom] == 0x58) {
                    //throw new Exception("Field is not aviable");
                    return null;
                }

                if (result == null)
                    result = 0;

                if (buf[i - 1 + nom] <= 0x39)
                    result = (int?)Math.Round(result.Value + (buf[i - 1 + nom] - 0x30) * Math.Pow(10, kol - i));
                else
                    result = (int?)Math.Round(result.Value + (buf[i - 1 + nom] - 0x37) * Math.Pow(10, kol - i));
            }
            return result;
        }

        private static int? GetValue(byte[] bytes, bool check = false) {
            int? result = null;
            var kol = bytes.Length;
            var sign = bytes[0] == 0x2d ? -1 : 1;

            for (var i = 0; i < kol; i++) {
                if (bytes[i] == 0x58)
                    if (check) throw new Exception("X");
                    else
                        return null;

                if (result == null)
                    result = 0;

                if (bytes[i] <= 0x39)
                    result = (int?)Math.Round(result.Value + (bytes[i] - 0x30) * Math.Pow(10, kol - i - 1));
                else
                    result = (int?)Math.Round(result.Value + (bytes[i] - 0x37) * Math.Pow(10, kol - i - 1));
            }
            return result * sign;
        }

        private static DateTime? fillDT(byte[] bytes) {
            try {
                DateTime result;
                var str = string.Join("", bytes.Select(t => (char)t));
                //result = DateTime.ParseExact(str, "MMddyyHHmm", CultureInfo.InvariantCulture);
                string[] dateFormats = { "dd.MM.yyyy", "dd/MM/yyyy", "MMddyyHHmm" };
                if (DateTime.TryParseExact(str, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) {
                    return result;
                }
                else if (DateTime.TryParse(str, out result))
                    return result;

                Logger.Error("TryParseExact Error {0}", str);
            } catch (Exception e) {
                Logger.Error($"error on fillDT {e.Message + e.StackTrace}");
            }
            return null;
        }

        private string ToStr(int? val, string format) {
            return val.HasValue ? val.Value.ToString(format) : string.Empty;
        }
        private string ToStr(int? val, CultureInfo info) {
            return val.HasValue ? val.Value.ToString(info) : string.Empty;
        }

        #region Commands
        /// <summary>
        /// SsVersion - 0x49 I
        /// </summary>
        /// <returns></returns>
        private int SSVersionCMD() {
            LastCom = Pv4Cmd.SsVersion;
            CmdDelay = 1000;
            cmdBuf = new byte[4];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.SsVersion;
            cmdBuf[2] = Pv4Cmd.ETX;
            cmdBuf[3] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2]); //BCC
            return SendBuf(4);
        }

        /// <summary>
        /// TankInventory - 0x48 H
        /// </summary>
        /// <param name="nom"></param>
        /// <returns></returns>
        private int TankInventoryCMD(string nom = "0100") {
            LastCom = Pv4Cmd.TankInventory;
            CmdDelay = 8000;
            cmdBuf = new byte[8];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.TankInventory;
            cmdBuf[2] = (byte)nom[0];
            cmdBuf[3] = (byte)nom[1];
            cmdBuf[4] = (byte)nom[2];
            cmdBuf[5] = (byte)nom[3];
            cmdBuf[6] = Pv4Cmd.ETX;
            cmdBuf[7] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]);//BCC
            return SendBuf(8);
        }

        /// <summary>
        /// TankInventoryExt - 0x68 h
        /// </summary>
        /// <param name="nom"></param>
        /// <returns></returns>
        private int TankInventoryExtCMD(string nom = "0100") {
            LastCom = Pv4Cmd.TankInventoryExt;
            CmdDelay = 8000;
            cmdBuf = new byte[8];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.TankInventoryExt;
            cmdBuf[2] = (byte)nom[0];
            cmdBuf[3] = (byte)nom[1];
            cmdBuf[4] = (byte)nom[2];
            cmdBuf[5] = (byte)nom[3];
            cmdBuf[6] = Pv4Cmd.ETX;
            cmdBuf[7] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]);//BCC
            return SendBuf(8);
        }

        /// <summary>
        /// TankLevels - 0x4C L 
        /// </summary>
        /// <param name="nom"></param>
        /// <returns></returns>
        private int TankLevelsCMD(string nom = "0100") {
            LastCom = Pv4Cmd.TankLevels;
            CmdDelay = 8000;
            cmdBuf = new byte[8];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.TankLevels;
            cmdBuf[2] = (byte)nom[0];
            cmdBuf[3] = (byte)nom[1];
            cmdBuf[4] = (byte)nom[2];
            cmdBuf[5] = (byte)nom[3];
            cmdBuf[6] = Pv4Cmd.ETX;
            cmdBuf[7] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]);//BCC
            return SendBuf(8);
        }

        /// <summary>
        /// TankLevels - 0x6C l 
        /// </summary>
        /// <param name="nom"></param>
        /// <returns></returns>
        private int TankLevelsExtCMD(string nom = "0100") {
            LastCom = Pv4Cmd.TankLevelsExt;
            CmdDelay = 2000;
            cmdBuf = new byte[8];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.TankLevelsExt;
            cmdBuf[2] = (byte)nom[0];
            cmdBuf[3] = (byte)nom[1];
            cmdBuf[4] = (byte)nom[2];
            cmdBuf[5] = (byte)nom[3];
            cmdBuf[6] = Pv4Cmd.ETX;
            cmdBuf[7] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]);//BCC
            return SendBuf(8);
        }


        /// <summary>
        /// ProductWeight - 0x57 W
        /// </summary>
        /// <returns></returns>
        private int ProductWeightCMD(string nom = "0100") {
            LastCom = Pv4Cmd.ProductWeight;
            CmdDelay = 8000;
            cmdBuf = new byte[8];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.ProductWeight;
            cmdBuf[2] = (byte)nom[0];
            cmdBuf[3] = (byte)nom[1];
            cmdBuf[4] = (byte)nom[2];
            cmdBuf[5] = (byte)nom[3];
            cmdBuf[6] = Pv4Cmd.ETX;
            cmdBuf[7] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]);//BCC
            return SendBuf(8);
        }

        /// <summary>
        /// ProductWeightExt - 0x77 w
        /// </summary>
        /// <returns></returns>
        private int ProductWeightExtCMD(string nom = "0100") {
            LastCom = Pv4Cmd.ProductWeightExt;
            CmdDelay = 2000;
            cmdBuf = new byte[8];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.ProductWeightExt;
            cmdBuf[2] = (byte)nom[0];
            cmdBuf[3] = (byte)nom[1];
            cmdBuf[4] = (byte)nom[2];
            cmdBuf[5] = (byte)nom[3];
            cmdBuf[6] = Pv4Cmd.ETX;
            cmdBuf[7] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]);//BCC
            return SendBuf(8);
        }

        /// <summary>
        /// ProductDensity - 0x47 G
        /// </summary>
        /// <param name="nom"></param>
        /// <returns></returns>
        private int ProductDensityCMD(string nom = "01") {
            LastCom = Pv4Cmd.ProductDensity;
            CmdDelay = 2000;
            cmdBuf = new byte[6];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.ProductDensity;
            cmdBuf[2] = (byte)nom[0];
            cmdBuf[3] = (byte)nom[1];
            cmdBuf[4] = Pv4Cmd.ETX;
            cmdBuf[5] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4]);//BCC
            return SendBuf(6);
        }

        /// <summary>
        /// DeliveryEnquiry - 0x44 D
        /// </summary>
        /// <param name="nom"></param>
        /// <returns></returns>
        private int DeliveryEnquiryCMD(string nom = "0101") {
            LastCom = Pv4Cmd.DeliveryEnquiry;
            CmdDelay = 2000;
            cmdBuf = new byte[8];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.DeliveryEnquiry;
            cmdBuf[2] = (byte)nom[0];
            cmdBuf[3] = (byte)nom[1];
            cmdBuf[4] = (byte)nom[2];
            cmdBuf[5] = (byte)nom[3];
            cmdBuf[6] = Pv4Cmd.ETX;
            cmdBuf[7] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]);//BCC
            return SendBuf(8);
        }

        /// <summary>
        /// DeliveryEnquiryExt - 0x64 d
        /// </summary>
        /// <param name="nom"></param>
        /// <returns></returns>
        private int DeliveryEnquiryExt(string nom = "0101") {
            LastCom = Pv4Cmd.DeliveryEnquiryExt;
            CmdDelay = 8000;
            cmdBuf = new byte[8];
            cmdBuf[0] = Pv4Cmd.STX;
            cmdBuf[1] = Pv4Cmd.DeliveryEnquiryExt;
            cmdBuf[2] = (byte)nom[0];
            cmdBuf[3] = (byte)nom[1];
            cmdBuf[4] = (byte)nom[2];
            cmdBuf[5] = (byte)nom[3];
            cmdBuf[6] = Pv4Cmd.ETX;
            cmdBuf[7] = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]);//BCC
            return SendBuf(8);
        }

        #endregion

        #region Fillers
        /// <summary>
        /// SITE SENTINEL VERSION - 0x49 I
        /// </summary>
        private bool FilllSSVersion() {
            var pos = 1;//<STX>
            var sysVersion = string.Empty;
            var country = string.Empty;
            var brdNumber = string.Empty;
            var ssType = string.Empty;
            DateTime? actualDateTime = null;

            try {
                // for nano
                sysVersion = System.Text.Encoding.Default.GetString(new[] { buf[pos], buf[pos + 1], buf[pos + 2], buf[pos + 3], buf[pos + 4], buf[pos + 5], buf[pos + 6], buf[pos + 7] });
                pos += 9;
                country = System.Text.Encoding.Default.GetString(new[] { buf[pos], buf[pos + 1], buf[pos + 2] });
                pos += 4;
                brdNumber = System.Text.Encoding.Default.GetString(new[] { buf[pos], buf[pos + 1], buf[pos + 2] });
                pos += 4;
                ssType = System.Text.Encoding.Default.GetString(new[] { buf[pos], buf[pos + 1], buf[pos + 2] });
                if (buf[pos + 4] == 0x2d) pos += 4;
                else pos += 3;
                actualDateTime = FillDT(pos);

                if (actualDateTime == null) {
                    Logger.Debug("Not nano");
                    pos = 1;
                    sysVersion = System.Text.Encoding.Default.GetString(new[] { buf[pos], buf[pos + 1], buf[pos + 2], buf[pos + 3], buf[pos + 4], buf[pos + 5] });
                    pos += 7;
                    country = System.Text.Encoding.Default.GetString(new[] { buf[pos], buf[pos + 1], buf[pos + 2] });
                    pos += 4;
                    brdNumber = System.Text.Encoding.Default.GetString(new[] { buf[pos], buf[pos + 1], buf[pos + 2] });
                    pos += 4;
                    ssType = System.Text.Encoding.Default.GetString(new[] { buf[pos], buf[pos + 1], buf[pos + 2] });
                    pos += 4;
                    actualDateTime = FillDT(pos);
                }
            } catch (Exception e) {
                Logger.Error(e);
            }

            Logger.Debug("System version = {0}", sysVersion);
            Logger.Debug("Country = {0}", country);
            Logger.Debug("Board number = {0}", brdNumber);
            Logger.Debug("Site Sentinel type = {0}", ssType);
            Logger.Debug("Actual DateTime = {0}", actualDateTime);

            return true;
        }

        /// <summary>
        /// Tank Levels - 0x4c L
        /// </summary>
        private List<TankMeasurementDataPv4Dto> FillTankLevels() {
            var sourceValues = new List<TankMeasurementDataPv4Dto>();
            var pos = 1;//<STX>

            try {
                var b = GetValue(pos, 1);
                pos += 2;//b + '='

                for (var i = 0; i < b; i++) {
                    var sourceValue = new TankMeasurementDataPv4Dto {
                        Data = new TankMeasurementData {
                            MeasurementDate = DateTime.Now
                        }
                    };

                    var tanknum = GetValue(pos, 2);
                    sourceValue.SourceName = ToStr(tanknum, CultureInfo.InvariantCulture);
                    pos += 2;

                    pos++;

                    var s = GetValue(pos, 1);
                    var st = s.HasValue ? (TankInventoryTdStatus)s : TankInventoryTdStatus.TankNotFound;
                    pos++;

                    if (st != TankInventoryTdStatus.TankOk) {
                        Logger.Error("TankNotFound {0} ", tanknum);
                        pos += 34;
                        continue;
                    }

                    var u = ((char)buf[pos]).ToString(CultureInfo.InvariantCulture).ToUpper();
                    pos++;

                    var mul = u.ToString(CultureInfo.InvariantCulture).ToUpper() == "L" ? 0.01f : 0.001f;
                    var ppppppp = GetValue(pos, 7);
                    sourceValue.Data.Level = ppppppp.HasValue ? getDecimal((ppppppp.Value * mul).ToString(CultureInfo.InvariantCulture), Logger) : 0m;

                    pos += 14;

                    var PPPPP = GetValue(pos, 5);
                    sourceValue.Data.Volume = getDecimal(ToStr(PPPPP, CultureInfo.InstalledUICulture), Logger);

                    pos += 14;

                    var tttt = 0.0f;

                    if (buf[pos] == 0x2d) {
                        var tempt = GetValue(pos + 1, 3);
                        tttt = tempt.HasValue ? tempt.Value * -0.1f : 0.0f;
                    } else {
                        var tempt = GetValue(pos, 4);
                        tttt = tempt.HasValue ? tempt.Value * 0.1f : 0.0f;
                    }
                    pos += 5;//+'='

                    sourceValue.Data.Temperature = getDecimal(tttt.ToString(CultureInfo.InvariantCulture), Logger);
                    sourceValues.Add(sourceValue);
                }
            } catch (Exception e) {
                Logger.Error(e);
            }

            return sourceValues;
        }

        class TankMeasurementDataPv4Dto {
            public long ExternalId { get; set; }
            public string SourceName { get; set; }
            public ObjectSource ObjectSource { get; set; }
            public TankMeasurementData Data { get; set; }
        }

        /// <summary>
        /// Tank Levels Ext - 0x6c l
        /// </summary>
        private List<TankMeasurementDataPv4Dto> FillTankLevelsExt() {
            var sourceValues = new List<TankMeasurementDataPv4Dto>();
            var pos = 1;

            try {
                var b = GetValue(pos, 1);
                pos += 2;//b + '='

                for (var i = 0; i < b; i++) {
                    var sourceValue = new TankMeasurementDataPv4Dto {
                        Data = new TankMeasurementData {
                            MeasurementDate = DateTime.Now
                        }
                    };
                    var nn = GetValue(pos, 2);
                    sourceValue.SourceName = ToStr(nn, CultureInfo.InvariantCulture);
                    pos += 2;
                    var x = GetValue(pos, 1);
                    pos++;
                    var s = GetValue(pos, 1);
                    var st = s.HasValue ? (TankInventoryTdStatus)s : TankInventoryTdStatus.TankNotFound;
                    pos++;

                    if (st != TankInventoryTdStatus.TankOk) {
                        Logger.Error("TankNotFound {0} Status {1}", nn, st);
                        pos += 47;
                        continue;
                    }

                    var u = ((char)buf[pos]).ToString(CultureInfo.InvariantCulture).ToUpper();
                    pos++;

                    var mul = u.ToString(CultureInfo.InvariantCulture).ToUpper() == "L" ? 0.01f : 0.001f;
                    var ppppppp = GetValue(pos, 7);
                    sourceValue.Data.Level = ppppppp.HasValue ? getDecimal((ppppppp.Value * mul).ToString(CultureInfo.InvariantCulture), Logger) : 0m;

                    pos += 14;

                    var PPPPP = GetValue(pos, 5);
                    sourceValue.Data.Volume = getDecimal(ToStr(PPPPP, CultureInfo.InstalledUICulture), Logger);

                    pos += 14;

                    var tttt = 0.0f;
                    if (buf[pos] == 0x2d) {
                        var tempt = GetValue(pos + 1, 3);
                        tttt = tempt.HasValue ? tempt.Value * -0.1f : 0.0f;
                    } else {
                        var tempt = GetValue(pos, 4);
                        tttt = tempt.HasValue ? tempt.Value * 0.1f : 0.0f;
                    }
                    pos += 4;
                    sourceValue.Data.Temperature = getDecimal(tttt.ToString(CultureInfo.InvariantCulture), Logger);

                    pos += 14;

                    sourceValues.Add(sourceValue);
                }
            } catch (Exception e) {
                Logger.Error(e);
            }

            return sourceValues;
        }

        /// <summary>
        /// Product Weight - 0x57 W
        /// </summary>
        /// <returns></returns>
        private List<TankMeasurementDataPv4Dto> FillProductWeight() {
            var sourceValues = new List<TankMeasurementDataPv4Dto>();
            var pos = 1;

            try {
                var b = GetValue(pos, 1);
                pos += 2;

                for (var i = 0; i < b; i++) {
                    var sourceValue = new TankMeasurementDataPv4Dto {
                        Data = new TankMeasurementData {
                            MeasurementDate = DateTime.Now
                        }
                    };

                    var nn = GetValue(pos, 2);
                    pos += 2;
                    sourceValue.SourceName = ToStr(nn, CultureInfo.InvariantCulture);

                    var s = GetValue(pos, 1);
                    var st = s.HasValue ? (DensityDeviceStatus)s : DensityDeviceStatus.NotConfigurated;

                    pos++;

                    if (st != DensityDeviceStatus.TankOk) {
                        pos += 22;
                        continue;
                    }

                    pos++;

                    var aaaaaaaa = GetValue(pos, 8);
                    sourceValue.Data.Mass = getDecimal(ToStr(aaaaaaaa, CultureInfo.InvariantCulture), Logger);
                    pos += 8;

                    var bbbbbbbb = GetValue(pos, 8);// * 0.0001f;
                    sourceValue.Data.Density = bbbbbbbb.HasValue ? getDecimal((bbbbbbbb.Value * 0.0001f).ToString(CultureInfo.InvariantCulture), Logger) : 0m;

                    pos += 13;

                    sourceValues.Add(sourceValue);
                }
            } catch (Exception e) {
                Logger.Error(e);
            }

            return sourceValues;
        }

        /// <summary>
        /// Product Weight - 0x77 w
        /// </summary>
        /// <returns></returns>
        private List<TankMeasurementDataPv4Dto> FillProductWeightExt() {
            var sourceValues = new List<TankMeasurementDataPv4Dto>();
            var pos = 1;

            try {
                var b = GetValue(pos, 1);
                pos += 2;

                for (var i = 0; i < b; i++) {
                    var sourceValue = new TankMeasurementDataPv4Dto {
                        Data = new TankMeasurementData {
                            MeasurementDate = DateTime.Now
                        }
                    };

                    var nn = GetValue(pos, 2);
                    pos += 2;
                    sourceValue.SourceName = ToStr(nn, CultureInfo.InvariantCulture);

                    var s = GetValue(pos, 1);
                    var st = s.HasValue ? (DensityDeviceStatus)s : DensityDeviceStatus.NotConfigurated;
                    pos++;
                    if (st != DensityDeviceStatus.TankOk) {
                        pos += 24;
                        continue;
                    }

                    pos++;

                    var aaaaaaaaaa = GetValue(pos, 10);
                    sourceValue.Data.Mass = getDecimal(ToStr(aaaaaaaaaa, CultureInfo.InvariantCulture), Logger);
                    pos += 10;

                    var bbbbbbbb = GetValue(pos, 8);
                    sourceValue.Data.Density = bbbbbbbb.HasValue ? getDecimal((bbbbbbbb.Value * 0.0001f).ToString(CultureInfo.InvariantCulture), Logger) : 0m;

                    pos += 13;

                    sourceValues.Add(sourceValue);
                }
            } catch (Exception e) {
                Logger.Error(e);
            }

            return sourceValues;
        }

        /// <summary>
        /// DeliveryEnquiry2 - 0x44 D
        /// </summary>
        /// <returns></returns>
        private List<TankTransferData> FillDeliveryEnquiry2() {
            var tr = new TankTransferData();

            try {
                var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
                var stuff = (DeliveryEnquiry)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DeliveryEnquiry));
                handle.Free();

                var nn = GetValue(stuff.nn);

                if (stuff.s != DeliveryStatus.Stable) {
                    Logger.Error("Tank {0} Delivery TankStatus is {1}", nn, stuff.s);
                    return null;
                }

                var sDateTime = fillDT(stuff.StartDelivery);
                if (sDateTime == null) return null;
                tr.StartDate = (DateTime)sDateTime;

                var eDateTime = fillDT(stuff.EndDelivery);
                if (eDateTime == null) return null;
                tr.EndDate = (DateTime)eDateTime;

                tr.VolumeEnd = GetValue(stuff.ooooo).HasValue ? getDecimal(GetValue(stuff.ooooo).ToString(), Logger) : 0m;
                if (tr.VolumeEnd == 0) return null;

                var tttt = GetValue(stuff.tttt) * 0.1f;

                //tr.Mass = null;  tf?
                //tr.Level = null;  tf?

                Logger.Info("Delivery Tank {0} | Tst={1} | Sd={2} | Ed={3} | V={4} | T={5}", nn, stuff.s, sDateTime, eDateTime, tr.VolumeEnd, tttt);

                return new List<TankTransferData> { tr };
            } catch (Exception e) {
                Logger.Error(e);
            }

            return null;
        }

        /// <summary>
        /// DeliveryEnquiryExt2 - 0x64 d
        /// </summary>
        /// <returns></returns>
        private List<TankTransferData> FillDeliveryEnquiryExt2() {
            var tr = new TankTransferData();
            try {
                var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
                var stuff = (DeliveryEnquiryExt)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DeliveryEnquiryExt));
                handle.Free();

                var tanknum = GetValue(stuff.nn, true);

                if (stuff.s == DeliveryStatus.None)
                    return null;

                if (stuff.s != DeliveryStatus.Stable) {
                    Logger.Error("Tank {0} Delivery TankStatus is {1}", tanknum, stuff.s);
                    return null;
                }

                var sDateTime = fillDT(stuff.StartDelivery);
                if (sDateTime == null) return null;
                tr.StartDate = (DateTime)sDateTime;

                var eDateTime = fillDT(stuff.EndDelivery);
                if (eDateTime == null) return null;
                tr.EndDate = (DateTime)eDateTime;

                tr.VolumeEnd = GetValue(stuff.ooooo).HasValue ? getDecimal(GetValue(stuff.ooooo).ToString(), Logger) : 0m;
                if (tr.VolumeEnd == 0) {
                    var msg = "Transfer Volume is 0";
                    Logger.Info(msg);
                    throw new Exception(msg);
                }

                var zzzzzzz = GetValue(stuff.zzzzzzz).HasValue ? getDecimal(GetValue(stuff.zzzzzzz).ToString(), Logger) : 0m;
                if (zzzzzzz == 0) {
                    var msg = "Weight is X|0";
                    Logger.Info(msg);
                    throw new Exception(msg);
                }

                tr.MassEnd = zzzzzzz;
                //tr.Level = null; tf?

                Logger.Info("Delivery Tank {0} | Tst={1} | Sd={2} | Ed={3} | V={4} | M={5}", tanknum, stuff.s, tr.StartDate, tr.EndDate, tr.VolumeEnd, tr.MassEnd);

                if(tr.VolumeEnd == 0 || tr.MassEnd == 0) {
                    return null;
                }

                return new List<TankTransferData> { tr };
            } catch (Exception e) {
                Logger.Error(e);
            }
            return null;
        }

        #endregion

    }
}
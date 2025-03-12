using Service.Clients.Client;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Opc.Hda;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Text;
using Service.LocalDb;
using Service.Common;

namespace Service.Clients.OPC {
    public class OpcHdaMultiServerClient : AMultiServerClient {
        private Server _hdaServer = null;
        private Trend group = null;
        private Trend groupTransfers = null;

        private OpcCom.Factory _fact = new OpcCom.Factory();
        private Opc.URL _url;

        public OpcHdaMultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker)
        {
            ObjectSettings = objectSettings;
            Worker = worker;
            Init();
        }

        private new void Init()
        {
            Thread.Sleep(5000);
            Logger.Debug("Start Init");
            base.Init();
            try {
                _fact = new OpcCom.Factory();
                _url = new Opc.URL(ObjectSettings.IpConnectionConfig.IpAddress);
                _hdaServer = new Server(_fact, _url);

                var counter = 0;
                while (!Connect()) {
                    counter++;
                    Logger.Debug("Try connect to server {0}", counter);
                    Thread.Sleep(30000);
                }
            } catch (Exception e) {
                Logger.Error(e);
            }
        }

        protected override void Reconnect()
        {
            Logger.Error("Reconnecting...");
            Init();
            _timer.Start();
        }

        private new bool Connect()
        {
            try {
                _hdaServer.Connect(new Opc.ConnectData(new NetworkCredential(ObjectSettings.IpConnectionConfig.DComUid, ObjectSettings.IpConnectionConfig.DComPwd)));
            } catch (Exception e) {
                Logger.Error("Error on {0}  {1}", _url.ToString(), e);
                Logger.Info("uid {0}, pwd {1}", ObjectSettings.IpConnectionConfig.DComUid, ObjectSettings.IpConnectionConfig.DComPwd);
                return false;
            }
            Logger.Debug("Connected to {0}", _url);
            return true;
        }

        protected override bool StopCollection()
        {
            Logger.Debug("Stop collection from OPC server");

            if (_hdaServer == null || !_hdaServer.IsConnected) return true;

            try {
                _hdaServer.Disconnect();
                _hdaServer.Dispose();
                _hdaServer = null;
            } catch (Exception e) {
                Logger.Error(e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Состояние связи с сервером
        /// </summary>
        /// <returns> связь есть-нет</returns>
        protected override bool GetConnectionState()
        {
            try {
                return _hdaServer.IsConnected; // :)
            } catch (Exception e) {
                Logger.Error(e);
                return false;
            }
        }

        protected override void CollectData()
        {
            Logger.Debug("RefreshTags");

            try {
                Logger.Debug($"Org {ObjectSettings.Objects.First().ObjectId}");

                var tanksMeasurements = new List<TankMeasurements>();

                // для всех КПУ (расходомеров/уровнемеров)
                foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(s => s.TankMeasurementParams != null)) {
                    var lastSyncDate = LastSyncRecord.GetByExternalId(source.ExternalId.Value, Logger);
                    // create tags
                    group = new Trend(_hdaServer);

                    group.AddIfNotNull(source.TankMeasurementParams.Temperature);
                    group.AddIfNotNull(source.TankMeasurementParams.Density);
                    group.AddIfNotNull(source.TankMeasurementParams.Volume);
                    group.AddIfNotNull(source.TankMeasurementParams.Mass);
                    group.AddIfNotNull(source.TankMeasurementParams.Level);
                    group.AddIfNotNull(source.TankMeasurementParams.DateTimeStamp);

                    group.Name = string.Format("{0}-{1}", group.Server.Url.HostName, Guid.NewGuid().ToString());

                    // int sec = 60;

                    //raw read
                    var startTime = lastSyncDate.LastMeasurementsSyncDate;
                    var endTime = startTime.AddMonths(3);
                    group.StartTime = new Time(startTime);
                    group.EndTime = new Time(endTime);
                    TimeSpan span = endTime.Subtract(startTime);
                    int calcinterval = ((int)span.TotalSeconds);
                    group.ResampleInterval = (decimal)calcinterval;
                    group.AggregateID = AggregateID.DURATIONGOOD;
                    group.MaxValues = 1; //Только 1 значение можно считывать. Не менять!

                    Logger.Debug("stt={0}, ent={1}", startTime, endTime);

                    ItemValueCollection[] results = group.ReadRaw();

                    if (results.All(t => t.Count <= 0) || results.Count(t => t.ResultID == Opc.ResultID.S_OK) < 4)
                    //if (results.All(t => t.Count <= 0))
                    {
                        Logger.Debug("no result");
                        continue;
                    }

                    var tankMeasurements = new TankMeasurements() { TankId = source.ExternalId.Value, Measurements = new List<TankMeasurementData>() };
                    var measurement = new TankMeasurementData()
                    {
                        MeasurementDate = OpcHelpers.GetDateTime(results, source.TankMeasurementParams.DateTimeStamp),
                        Density = OpcHelpers.GetDecimal(results, source.TankMeasurementParams.Density),
                        Level = OpcHelpers.GetDecimal(results, source.TankMeasurementParams.Level),
                        Mass = OpcHelpers.GetDecimal(results, source.TankMeasurementParams.Mass),
                        Volume = OpcHelpers.GetDecimal(results, source.TankMeasurementParams.Volume),
                        Temperature = OpcHelpers.GetDecimal(results, source.TankMeasurementParams.Temperature),
                        LevelUnitType = source.LevelUnitType.Value,
                        MassUnitType = source.MassUnitType.Value,
                        VolumeUnitType = source.VolumeUnitType.Value,
                        OilProductType = source.OilProductType.Value
                    };

                    tankMeasurements.Measurements.Add(measurement);
                    tankMeasurements.Measurements = tankMeasurements.Measurements.ToArray().SetEnums(source);
                    tanksMeasurements.Add(tankMeasurements);
                    group = null;
                }
                QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private static string ToSafeString(object val)
        {
            return val == null ? string.Empty : val.ToString().Replace("\0", string.Empty);
        }
    }
}

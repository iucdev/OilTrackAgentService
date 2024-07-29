using Service.Clients.Client;
using Service.Clients.RSMDB;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Enums;
using PISDK;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Service.Common;

namespace Service.Clients.PI {
    public class PiMultiServerClient : AMultiServerClient
    {
        private Server _piServer;
        private PISDK.PISDK _piSdk;
        private RSMDBProxy _rsproxy;

        public PiMultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker)
        {
            ObjectSettings = objectSettings;
            Worker = worker;
            Init();
        }

        private new void Init()
        {
            Logger.Debug("Start Init");
            base.Init();
            try
            {
                if (_piSdk == null || _piServer == null)
                {
                    _piSdk = new PISDK.PISDK();
                    var serverName = string.IsNullOrEmpty(ObjectSettings.IpConnectionConfig.IpAddress) ? _piSdk.Servers.DefaultServer.Name : ObjectSettings.IpConnectionConfig.IpAddress;
                    _piServer = _piSdk.Servers[serverName];
                }

                _rsproxy = new RSMDBProxy();
                RSMDBProxy.RSMDBConnectionString = ObjectSettings.IpConnectionConfig.RSMDBConnectionString;

                if (IsPiOpen()) return;

                // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, Task.ObjectId);
                throw new Exception("PI is closed!");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected override void Reconnect()
        {
            Logger.Error("Reconnecting...");
            Init();
            _timer.Start();
        }

        protected new bool StopCollection()
        {
            return IsPiClose();
        }

        private bool IsPiOpen()
        {
            try
            {
                if (!_piServer.Connected)
                    _piServer.Open();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
            return true;
        }

        private bool IsPiClose()
        {
            try
            {
                if (_piServer.Connected)
                    _piServer.Close();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
            return true;
        }

        //protected override void RefreshTransfers()
        //{
            //try
            //{
            //    Logger.Debug("Call RefreshTransfers");

            //    var dtS = DateTime.Now.Date;
            //    var dtE = DateTime.Now;

            //    if (_rsproxy == null)
            //        return;

            //    var listTr = _rsproxy.GetTransfers(dtS, dtE);

            //    if (listTr == null)
            //    {
            //        Logger.Debug("Transfer List is empty");
            //        return;
            //    }

            //    var tankTransfers = new List<TankTransferData>();

            //    foreach (var transfer in listTr)
            //    {
            //        tankTransfers.Add(new TankTransferData()
            //        {
            //            StartDate = transfer.StartTime,
            //            EndDate = transfer.EndTime
            //            откуда брать остальные данные?
            //        }); 
            //    }

            //    _sunpApiClientUtil.SendTanksTransfers(tankTransfers, _task);
            //}
            //catch (Exception e)
            //{
            //    Logger.Error($"ERROR on RefreshTransfers - {e.Message + e.StackTrace}");
            //}
        //}

        private TankMeasurements GetTankMeasurements(ObjectSource source) {
            var tankMeasurements = new TankMeasurements() { TankId = source.ExternalId.Value };
            var parameter = source.TankMeasurementParams;
            var measurement = new TankMeasurementData() {
                MeasurementDate = PiMultiServerClientHelpers.GetDateTime(Snapshot(parameter.DateTimeStamp)),
                Density = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Density)),
                Level = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Level)),
                Mass = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Mass)),
                Volume = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Volume)),
                Temperature = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Temperature)),
                LevelUnitType = source.LevelUnitType.Value,
                MassUnitType = source.MassUnitType.Value,
                VolumeUnitType = source.VolumeUnitType.Value,
                OilProductType = source.OilProductType.Value
            };
            tankMeasurements.Measurements = new[] { measurement }.SetEnums(source);
            return tankMeasurements;
        }

        protected override void CollectData()
        {
            Logger.Debug("Call CollectData");

            try
            {
                var tanksMeasurements = new List<TankMeasurements>();
                foreach (var source in ObjectSettings.Objects.First().ObjectSources) {
                    tanksMeasurements.Add(GetTankMeasurements(source));
                }

                QueueTaskService.Instance.SaveAsTask(tanksMeasurements.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Error($"Error on collect data {ex.Message + ex.StackTrace}");
            }
        }

        protected override bool Fill(int bytesRec)
        {
            return true;
        }

        protected override bool SkipCmd()
        {
            return false;
        }

        protected override bool CheckRb(int bytesRec)
        {
            return true;
        }

        

        private PointData Snapshot(string tagName)
        {
            PointData data = null;
            try
            {
                var tag = GetPoint(tagName);
                if (tag != null)
                {
                    var value = tag.Data.Snapshot;
                    if (value != null)
                    {
                        data = new PointData(tag.Name, value.Value is DigitalState ? ((DigitalState)value.Value).Name : value.Value, value.TimeStamp.LocalDate);
                        Logger.Debug($"SnapshotValue {data.Name} = {data.Value} ({data.DateTimeStamp})");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return data;
        }

        private PIPoint GetPoint(string tagName)
        {
            PIPoint pt = null;
            try
            {
                if (_piServer != null)
                    pt = _piServer.PIPoints[tagName];
            }
            catch (Exception ex)
            {
                // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, Task.ObjectId);
                Logger.Error(ex);
            }

            return pt;
        }

        protected override bool GetConnectionState()
        {
            try
            {
                return _piServer != null ? _piServer.Connected : false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }
    }
}
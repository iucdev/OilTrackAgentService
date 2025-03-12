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

        private TankMeasurements GetTankMeasurements(ObjectSource source) {
            var tankMeasurements = new TankMeasurements() { TankId = source.ExternalId.Value };
            var parameter = source.TankMeasurementParams;
            var measurement = new TankMeasurementData();

            measurement.MeasurementDate = string.IsNullOrEmpty(parameter.DateTimeStamp) ? DateTime.Now : PiMultiServerClientHelpers.GetDateTime(Snapshot(parameter.DateTimeStamp));
            measurement.Density = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Density));
            measurement.Level = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Level));
            measurement.Mass = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Mass));
            measurement.Volume = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Volume));
            measurement.Temperature = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.Temperature));
            var oilProductTypeRaw = PiMultiServerClientHelpers.ToSafeStringWithEncoding(Snapshot(parameter.OilProductType).Value);
            var parsedOilProductType = CommonHelper.TryGetOilProductType(oilProductTypeRaw, Logger);

            tankMeasurements.Measurements = new[] { measurement }.SetEnums(source, oilProductType: parsedOilProductType);
            return tankMeasurements;
        }

        private FlowmeterMeasurements GetFlowmeterMeasurements(ObjectSource source) 
        {
            var flowmeterMeasurements = new FlowmeterMeasurements() { FlowmeterId = source.ExternalId.Value };
            var parameter = source.FlowmeterIndicatorParams;
            var measurement = new FlowmeterMeasurementData();

            measurement.MeasurementDate = string.IsNullOrEmpty(parameter.DateTimeStamp) ? DateTime.Now : PiMultiServerClientHelpers.GetDateTime(Snapshot(parameter.DateTimeStamp));
            measurement.TotalMass = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.TotalMass));
            measurement.FlowMass = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.FlowMass));
            measurement.TotalVolume = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.TotalVolume));
            measurement.CurrentDensity = PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.CurrentDensity));
            measurement.CurrentTemperature= PiMultiServerClientHelpers.GetDecimal(Snapshot(parameter.CurrentTemperature));
            measurement.SourceTankId = CommonHelper.TryGetSourceTankId(PiMultiServerClientHelpers.ToSafeString(Snapshot(parameter.SourceTankId)), source);
            measurement.RenterXin = PiMultiServerClientHelpers.ToSafeStringWithEncoding(Snapshot(parameter.RenterXin).Value);
            var oilProductTypeRaw = PiMultiServerClientHelpers.ToSafeStringWithEncoding(Snapshot(parameter.OilProductType).Value);
            var parsedOilProductType = CommonHelper.TryGetOilProductType(oilProductTypeRaw, Logger);
            var operationTypeRaw = PiMultiServerClientHelpers.ToSafeStringWithEncoding(Snapshot(parameter.OperationType).Value);
            var parsedOperationType = CommonHelper.TryGetFlowmeterOperationType(operationTypeRaw, Logger);


            flowmeterMeasurements.Measurements = new[] { measurement }.SetEnums(source, operationType: parsedOperationType, oilProductType: parsedOilProductType);
            return flowmeterMeasurements;
        }

        protected override void CollectData()
        {
            Logger.Debug("Call CollectData");

            try {
                var tanksMeasurements = new List<TankMeasurements>();
                var flowmeterMeasurements = new List<FlowmeterMeasurements>();

                foreach (var source in ObjectSettings.Objects.First().ObjectSources) {
                    if (source.TankMeasurementParams != null) {
                        tanksMeasurements.Add(GetTankMeasurements(source));
                    }
                    if (source.FlowmeterIndicatorParams != null) {
                        flowmeterMeasurements.Add(GetFlowmeterMeasurements(source));
                    }
                }

                QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
                QueueTaskService.Instance.SaveFlowmeterAsTask(flowmeterMeasurements.ToArray());
            } 
            catch (Exception ex)
            {
                Logger.Error($"Error on collect data {ex.Message + ex.StackTrace}");
            }
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
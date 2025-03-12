using NLog;
using NLog.Fluent;
using Opc;
using Opc.Da;
using Service.Clients.Client;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Common;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Service.Clients.OPC {
    public class OpcMultiServerClient : AMultiServerClient {
        private Opc.Da.Server _server;
        private OpcCom.Factory _fact = new OpcCom.Factory();
        private Opc.URL _url;
        private Opc.Da.SubscriptionState _tankMeasurementGroupReadState = null;
        private Opc.Da.Subscription _tankMeasurementGroupRead = null;
        private Opc.Da.SubscriptionState _transferGroupReportState = null;
        private Opc.Da.Subscription _transferGroupReport = null;
        private Opc.Da.SubscriptionState _flowmeterGroupReportState = null;
        private Opc.Da.Subscription _flowmeterGroupReport = null;

        public OpcMultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker)
        {
            ObjectSettings = objectSettings;
            Worker = worker;
            Init();
        }

        private new void Init()
        {
            Thread.Sleep(1000);
            Logger.Debug("Start Init");
            base.Init();
            try {
                _fact = new OpcCom.Factory();
                _url = new Opc.URL(ObjectSettings.IpConnectionConfig.IpAddress);
                _server = new Opc.Da.Server(_fact, null);

                var counter = 0;
                while (!Connect()) {
                    counter++;
                    Logger.Debug("Try connect to server {0}", counter);
                    Thread.Sleep(600);
                }

                if (!InitGroup()) {
                    Logger.Debug("InitGroup false");
                    Thread.Sleep(60000);
                    Reconnect();
                }
            } catch (Exception e) //зациклить 
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

        private new bool Connect()
        {
            try {
                Logger.Debug($"Try connect to {ObjectSettings.IpConnectionConfig.IpAddress}");
                _server.Connect(_url, new Opc.ConnectData(new NetworkCredential(ObjectSettings.IpConnectionConfig.DComUid, ObjectSettings.IpConnectionConfig.DComPwd)));
            } catch (Exception e) {
                Logger.Error("Error on {0}  {1}", _url.ToString(), e);
                Logger.Info("uid {0}, pwd {1}", ObjectSettings.IpConnectionConfig.DComUid, ObjectSettings.IpConnectionConfig.DComPwd);
                return false;
            }
            Logger.Debug("Connected to {0}", _url);
            return true;
        }

        private bool InitGroup()
        {
            try {
                var isVaild = GetConnectionState();
                if (!isVaild) return false;

                _tankMeasurementGroupReadState = new Opc.Da.SubscriptionState { Name = "StateGroup", Active = true };
                _tankMeasurementGroupRead = (Opc.Da.Subscription)_server.CreateSubscription(_tankMeasurementGroupReadState);

                _transferGroupReportState = new Opc.Da.SubscriptionState { Name = "TransferGroup", Active = true };
                _transferGroupReport = (Opc.Da.Subscription)_server.CreateSubscription(_transferGroupReportState);

                _flowmeterGroupReportState = new Opc.Da.SubscriptionState { Name = "FlowmeterGroup", Active = true };
                _flowmeterGroupReport = (Opc.Da.Subscription)_server.CreateSubscription(_flowmeterGroupReportState);

                // Create a measurement groups
                Logger.Debug("Creating Measurement Groups");
                foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(p => p.TankMeasurementParams != null).ToArray()) {
                    List<TankMeasurementParams> tagList = new List<TankMeasurementParams> {
                        source.TankMeasurementParams
                    };
                    var items = new List<Opc.Da.Item>();

                    var tableBuilder = new StringBuilder();
                    tableBuilder.AppendLine("Measurement Parameters:");
                    tableBuilder.AppendLine("------------------------------");
                    tableBuilder.AppendLine($"Temperature: {source.TankMeasurementParams.Temperature}");
                    tableBuilder.AppendLine($"Density: {source.TankMeasurementParams.Density}");
                    tableBuilder.AppendLine($"Volume: {source.TankMeasurementParams.Volume}");
                    tableBuilder.AppendLine($"Weight: {source.TankMeasurementParams.Mass}");
                    tableBuilder.AppendLine($"Level: {source.TankMeasurementParams.Level}");
                    tableBuilder.AppendLine($"DateTimeStamp: {source.TankMeasurementParams.DateTimeStamp}");
                    tableBuilder.AppendLine($"OilProductType: {source.TankMeasurementParams.OilProductType}");
                    tableBuilder.AppendLine("------------------------------");

                    Logger.Debug(tableBuilder.ToString());

                    items.AddIfNotNull(source.TankMeasurementParams.Temperature);
                    items.AddIfNotNull(source.TankMeasurementParams.Density);
                    items.AddIfNotNull(source.TankMeasurementParams.Volume);
                    items.AddIfNotNull(source.TankMeasurementParams.Mass);
                    items.AddIfNotNull(source.TankMeasurementParams.Level);
                    items.AddIfNotNull(source.TankMeasurementParams.DateTimeStamp);
                    items.AddIfNotNull(source.TankMeasurementParams.OilProductType);

                    var itemArray = items.ToArray();
                    itemArray = _tankMeasurementGroupRead.AddItems(itemArray);
                }

                // Create a transfer groups
                Logger.Debug("Creating Transfer Groups");
                foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(p => p.TankTransferParams != null).ToArray()) {
                    //var tagsSource = source.Tags.Where(t => !string.IsNullOrEmpty(t.Value));
                    //List<Tag> tagList = new List<Tag>();
                    var items = new List<Opc.Da.Item>();

                    var tableBuilder = new StringBuilder();
                    tableBuilder.AppendLine("Transfer Parameters:");
                    tableBuilder.AppendLine("------------------------------");
                    tableBuilder.AppendLine($"StartTime: {source.TankTransferParams.StartTime}");
                    tableBuilder.AppendLine($"EndTime: {source.TankTransferParams.EndTime}");
                    tableBuilder.AppendLine($"WeightStart: {source.TankTransferParams.MassStart}");
                    tableBuilder.AppendLine($"WeightFinish: {source.TankTransferParams.MassFinish}");
                    tableBuilder.AppendLine($"LevelStart: {source.TankTransferParams.LevelStart}");
                    tableBuilder.AppendLine($"LevelFinish: {source.TankTransferParams.LevelFinish}");
                    tableBuilder.AppendLine($"VolumeStart: {source.TankTransferParams.VolumeStart}");
                    tableBuilder.AppendLine($"VolumeStart: {source.TankTransferParams.VolumeStart}");
                    tableBuilder.AppendLine($"VolumeFinish: {source.TankTransferParams.VolumeFinish}");
                    tableBuilder.AppendLine($"OilProductName: {source.TankTransferParams.OilProductType}");
                    tableBuilder.AppendLine("------------------------------");

                    if (source.TankTransferParams.StartTime.Contains("/")) {
                        char[] separators = new char[] { '/' };
                        var startDateTags = source.TankTransferParams.StartTime.Split(separators);
                        items.AddIfNotNull(startDateTags.First());
                        tableBuilder.AppendLine($"StartDate: {startDateTags.First()}");
                        items.AddIfNotNull(startDateTags.Last());
                        tableBuilder.AppendLine($"StartTime: {startDateTags.Last()}");
                        var endDateTags = source.TankTransferParams.EndTime.Split(separators);
                        items.AddIfNotNull(endDateTags.First());
                        items.AddIfNotNull(endDateTags.Last());
                    } else {
                        items.AddIfNotNull(source.TankTransferParams.StartTime);
                        items.AddIfNotNull(source.TankTransferParams.EndTime);
                    }

                    items.AddIfNotNull(source.TankTransferParams.StartTime);
                    items.AddIfNotNull(source.TankTransferParams.EndTime);
                    items.AddIfNotNull(source.TankTransferParams.MassStart);
                    items.AddIfNotNull(source.TankTransferParams.MassFinish);
                    items.AddIfNotNull(source.TankTransferParams.LevelStart);
                    items.AddIfNotNull(source.TankTransferParams.LevelFinish);
                    items.AddIfNotNull(source.TankTransferParams.VolumeStart);
                    items.AddIfNotNull(source.TankTransferParams.VolumeFinish);
                    items.AddIfNotNull(source.TankTransferParams.OilProductType);

                    var itemArray = items.ToArray();
                    Logger.Debug(tableBuilder.ToString());
                    itemArray = _transferGroupReport.AddItems(itemArray);
                }

                // Create a flowmeter groups
                Logger.Debug("Creating Flowmeter Groups");
                foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(p => p.FlowmeterIndicatorParams != null).ToArray()) {
                    var items = new List<Opc.Da.Item>();

                    var tableBuilder = new StringBuilder();
                    tableBuilder.AppendLine("Flowmeter Parameters:");
                    tableBuilder.AppendLine("------------------------------");

                    tableBuilder.AppendLine($"TotalMass: {source.FlowmeterIndicatorParams.TotalMass}");
                    tableBuilder.AppendLine($"FlowMass: {source.FlowmeterIndicatorParams.FlowMass}");
                    tableBuilder.AppendLine($"TotalVolume: {source.FlowmeterIndicatorParams.TotalVolume}");
                    tableBuilder.AppendLine($"CurrentDensity: {source.FlowmeterIndicatorParams.CurrentDensity}");
                    tableBuilder.AppendLine($"CurrentTemperature: {source.FlowmeterIndicatorParams.CurrentTemperature}");

                    tableBuilder.AppendLine($"OilProductName: {source.FlowmeterIndicatorParams.OilProductType}");
                    tableBuilder.AppendLine($"OperationType: {source.FlowmeterIndicatorParams.OperationType}");
                    tableBuilder.AppendLine($"SourceTankId: {source.FlowmeterIndicatorParams.SourceTankId}");
                    tableBuilder.AppendLine($"RenterXin: {source.FlowmeterIndicatorParams.RenterXin}");
                    tableBuilder.AppendLine("------------------------------");

                    items.AddIfNotNull(source.FlowmeterIndicatorParams.TotalMass);
                    items.AddIfNotNull(source.FlowmeterIndicatorParams.FlowMass);
                    items.AddIfNotNull(source.FlowmeterIndicatorParams.TotalVolume);
                    items.AddIfNotNull(source.FlowmeterIndicatorParams.CurrentDensity);
                    items.AddIfNotNull(source.FlowmeterIndicatorParams.CurrentTemperature);
                    items.AddIfNotNull(source.FlowmeterIndicatorParams.OilProductType);
                    items.AddIfNotNull(source.FlowmeterIndicatorParams.OperationType);
                    items.AddIfNotNull(source.FlowmeterIndicatorParams.SourceTankId);
                    items.AddIfNotNull(source.FlowmeterIndicatorParams.RenterXin);

                    var itemArray = items.ToArray();
                    Logger.Debug(tableBuilder.ToString());
                    itemArray = _flowmeterGroupReport.AddItems(itemArray);
                }
            } catch (Exception ex) {
                Logger.Error("Error on Init group {0}", ex.Message);
                return false;
            }
            Logger.Debug($"_transferGroupReport {_transferGroupReport.Items.Count()}; _tankMeasurementGroupRead {_tankMeasurementGroupRead.Items.Count()}; _flowmeterGroupReport {_flowmeterGroupReport.Items.Count()}");

            //if (_transferGroupReport.Items.Count() == 0 && _tankMeasurementGroupRead.Items.Count() == 0)
            //{
            //    Logger.Error("Error there is not a single correct tag! Check tags!");
            //    return false;
            //}
            return true;
        }

        protected override bool StopCollection()
        {
            Logger.Debug("Stop collection from OPC server");

            if (_server == null) return true;

            try {
                _server.Disconnect();
                _server.Dispose();
                _server = null;
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
                _server.GetStatus();
                return true;
            } catch (Exception e) {
                Logger.Error(e);
                return false;
            }
        }

        private FlowmeterMeasurements[] collectFlowmeterIndicators(ItemValueResult[] flowmeterMeasurementResults)
        {
            var flowmeterMeasurements = new List<FlowmeterMeasurements>();

            foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(s => s.FlowmeterIndicatorParams != null)) {
                try {
                    var flowmeterMeasurement = new FlowmeterMeasurements { FlowmeterId = source.ExternalId.Value };
                    var measurement = new FlowmeterMeasurementData();

                    var flowmeterParams = source.FlowmeterIndicatorParams;

                    measurement.MeasurementDate = DateTime.Now;
                    measurement.TotalMass = OpcHelpers.TryGetDecimal(flowmeterMeasurementResults, flowmeterParams.TotalMass, Logger);
                    measurement.FlowMass = OpcHelpers.TryGetDecimal(flowmeterMeasurementResults, flowmeterParams.FlowMass, Logger);
                    measurement.TotalVolume = OpcHelpers.TryGetDecimal(flowmeterMeasurementResults, flowmeterParams.TotalVolume, Logger);
                    measurement.CurrentDensity = OpcHelpers.TryGetDecimal(flowmeterMeasurementResults, flowmeterParams.CurrentDensity, Logger);
                    measurement.CurrentTemperature = OpcHelpers.TryGetDecimal(flowmeterMeasurementResults, flowmeterParams.CurrentTemperature, Logger);

                    flowmeterMeasurement.Measurements = new[] { measurement }.SetEnums(
                        source, 
                        OpcHelpers.TryGetFlowmeterOperationType(flowmeterMeasurementResults, flowmeterParams.OperationType, Logger),
                        string.IsNullOrEmpty(flowmeterParams.OilProductType) ? source.OilProductType : OpcHelpers.TryGetOilProductType(flowmeterMeasurementResults, flowmeterParams.OilProductType, Logger)
                    );
                    measurement.RenterXin = string.IsNullOrEmpty(flowmeterParams.RenterXin) ? "" : OpcHelpers.TryGetString(flowmeterMeasurementResults, flowmeterParams.RenterXin, Logger);
                    Logger.Debug("FlowmeterMeasurements Model Filled with values");

                    var tableBuilder = new StringBuilder();
                    tableBuilder.AppendLine("FlowmeterIndicators Values:");
                    tableBuilder.AppendLine("------------------------------");
                    tableBuilder.AppendLine($"TotalMass: {measurement.TotalMass}");
                    tableBuilder.AppendLine($"FlowMass: {measurement.FlowMass}");
                    tableBuilder.AppendLine($"TotalVolume: {measurement.TotalVolume}");
                    tableBuilder.AppendLine($"CurrentDensity: {measurement.CurrentDensity}");
                    tableBuilder.AppendLine($"CurrentTemperature: {measurement.CurrentTemperature}");
                    tableBuilder.AppendLine($"OilProductType: {measurement.OilProductType}");
                    tableBuilder.AppendLine($"OperationType: {measurement.OperationType}");
                    tableBuilder.AppendLine($"SourceTankId: {measurement.SourceTankId}");
                    tableBuilder.AppendLine($"RenterXin: {measurement.RenterXin}");

                    var itemsStr = string.Format("Source '{0}'", source.ExternalId.Value);

                    Logger.Debug(itemsStr);
                    Logger.Debug(tableBuilder.ToString());
                    flowmeterMeasurements.Add(flowmeterMeasurement);
                    Logger.Debug("Keep Filling List");
                    Logger.Debug("------------------------------");
                } catch (Exception ex) {
                    Logger.Error($"Error while collecting flowmeter measurements {source.InternalId}. Exception: {ex.Message + ex.StackTrace}");
                    continue;
                }
            }
            Logger.Debug("FlowmeterMeasurements List Filled");

            return flowmeterMeasurements.ToArray();
        }

        private TankMeasurements[] collectTankMeasurements(ItemValueResult[] tankMeasurementResults)
        {
            var tanksMeasurements = new List<TankMeasurements>();

            foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(s => s.TankMeasurementParams != null)) {
                try {
                    var tankMeasurements = new TankMeasurements { TankId = source.ExternalId.Value };
                    var measurement = new TankMeasurementData();

                    var tankParams = source.TankMeasurementParams;
                    DateTime utcNow = DateTime.UtcNow;
                    // Define the time span for UTC+5
                    TimeSpan utcPlusFive = new TimeSpan(5, 0, 0);
                    // Add the time span to the UTC time
                    DateTime utcPlusFiveTime = utcNow.Add(utcPlusFive);
                    var now = new DateTime(utcPlusFiveTime.Year, utcPlusFiveTime.Month, utcPlusFiveTime.Day, utcPlusFiveTime.Hour, utcPlusFiveTime.Minute, utcPlusFiveTime.Second, utcPlusFiveTime.Millisecond);
                    measurement.Temperature = OpcHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Temperature, Logger);
                    measurement.Density = OpcHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Density, Logger);
                    measurement.Volume = OpcHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Volume, Logger);
                    measurement.Mass = OpcHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Mass, Logger);
                    measurement.Level = OpcHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Level, Logger);
                    measurement.MeasurementDate = string.IsNullOrEmpty(tankParams.DateTimeStamp) ? now : OpcHelpers.TryGetDateTime(tankMeasurementResults, tankParams.DateTimeStamp, Logger);
                    var oilProductType = string.IsNullOrEmpty(tankParams.OilProductType) ? source.OilProductType : OpcHelpers.TryGetOilProductType(tankMeasurementResults, tankParams.OilProductType, Logger);
                    tankMeasurements.Measurements = new[] { measurement }.SetEnums(source, oilProductType);
                    Logger.Debug("Tank Measurement Model Filled with values");

                    var tableBuilder = new StringBuilder();
                    tableBuilder.AppendLine("Measurement Values:");
                    tableBuilder.AppendLine("------------------------------");
                    tableBuilder.AppendLine($"Temperature: {measurement.Temperature}");
                    tableBuilder.AppendLine($"Density: {measurement.Density}");
                    tableBuilder.AppendLine($"Volume: {measurement.Volume}");
                    tableBuilder.AppendLine($"Mass: {measurement.Mass}");
                    tableBuilder.AppendLine($"Level: {measurement.Level}");
                    tableBuilder.AppendLine($"MeasurementDate: {measurement.MeasurementDate}");
                    tableBuilder.AppendLine($"OilProductType: {measurement.OilProductType}");

                    var itemsStr = string.Format("Source '{0}'", source.ExternalId.Value);

                    Logger.Debug(itemsStr);
                    Logger.Debug(tableBuilder.ToString());
                    tanksMeasurements.Add(tankMeasurements);
                    Logger.Debug("Keep Filling List");
                    Logger.Debug("------------------------------");
                } catch (Exception ex) {
                    Logger.Error($"Error while collecting tank measurements {source.InternalId}. Exception: {ex}");
                    continue;
                }
            }
            Logger.Debug("TankMeasurement List Filled");

            return tanksMeasurements.ToArray();
        }

        private TankTransfers[] collectTankTransfers(ItemValueResult[] tankTransferResult)
        {
            var tanksTransfers = new List<TankTransfers>();
            Logger.Info("Here comes new Transfer code");
            foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(s => s.TankTransferParams != null)) {
                try {
                    var tankTransfers = new TankTransfers { TankId = source.ExternalId.Value, Transfers = new List<TankTransferData>() };
                    var transfer = new TankTransferData();

                    var transferParams = source.TankTransferParams;

                    Logger.Debug("Start Filling Decimals into Model");
                    transfer.VolumeStart = OpcHelpers.TryGetDecimal(tankTransferResult, transferParams.VolumeStart, Logger);
                    transfer.VolumeEnd = OpcHelpers.TryGetDecimal(tankTransferResult, transferParams.VolumeFinish, Logger);
                    transfer.MassStart = OpcHelpers.TryGetDecimal(tankTransferResult, transferParams.MassStart, Logger);
                    transfer.MassEnd = OpcHelpers.TryGetDecimal(tankTransferResult, transferParams.MassFinish, Logger);
                    transfer.LevelStart = OpcHelpers.TryGetDecimal(tankTransferResult, transferParams.LevelStart, Logger);
                    transfer.LevelEnd = OpcHelpers.TryGetDecimal(tankTransferResult, transferParams.LevelFinish, Logger);
                    Logger.Debug("Finish Filling Decimals into Model");
                    Logger.Debug("Start Filling Dates into Model");
                    
                    if (source.TankTransferParams.StartTime.Contains("/")) {
                        char[] separators = new char[] { '/' };
                        var startDateTags = source.TankTransferParams.StartTime.Split(separators);
                        Logger.Debug($"StartDateTags: {startDateTags.First()} {startDateTags.Last()}");
                        transfer.StartDate = OpcHelpers.TryGetDateTime(tankTransferResult, startDateTags.First(), startDateTags.Last(), Logger);
                        var endDateTags = source.TankTransferParams.EndTime.Split(separators);
                        Logger.Debug($"EndDateTags: {endDateTags.First()} {endDateTags.Last()}");
                        transfer.EndDate = OpcHelpers.TryGetDateTime(tankTransferResult, endDateTags.First(), endDateTags.Last(), Logger);
                    } else {
                        transfer.StartDate = OpcHelpers.TryGetDateTime(tankTransferResult, transferParams.StartTime, Logger);
                        transfer.EndDate = OpcHelpers.TryGetDateTime(tankTransferResult, transferParams.EndTime, Logger);
                    }

                    if (transfer.StartDate <= DateTime.MinValue || transfer.EndDate <= DateTime.MinValue) {
                        Logger.Error($"StartDate {transfer.StartDate}, EndDate {transfer.EndDate}. Date error");
                        continue;
                    }
                    Logger.Debug("Finish Filling Dates into Model");

                    var oilProductType = string.IsNullOrEmpty(transferParams.OilProductType) ? source.OilProductType : OpcHelpers.TryGetOilProductType(tankTransferResult, transferParams.OilProductType, Logger);
                    tankTransfers.Transfers = new[] { transfer }.SetEnums(source, oilProductType);
                    Logger.Debug("Transfer Measurement Model Filled with values");

                    var tableBuilder = new StringBuilder();
                    tableBuilder.AppendLine("Transfer Measurement Values:");
                    tableBuilder.AppendLine("------------------------------");
                    tableBuilder.AppendLine($"VolumeStart: {transfer.VolumeStart}");
                    tableBuilder.AppendLine($"VolumeFinish: {transfer.VolumeEnd}");
                    tableBuilder.AppendLine($"MassStart: {transfer.MassStart}");
                    tableBuilder.AppendLine($"MassFinish: {transfer.MassEnd}");
                    tableBuilder.AppendLine($"LevelStart: {transfer.LevelStart}");
                    tableBuilder.AppendLine($"LevelFinish: {transfer.LevelEnd}");
                    tableBuilder.AppendLine($"StartDate: {transfer.StartDate}");
                    tableBuilder.AppendLine($"EndDate: {transfer.EndDate}");
                    tableBuilder.AppendLine($"OilProductType: {transfer.OilProductType}");

                    Logger.Debug(tableBuilder.ToString());
                    tanksTransfers.Add(tankTransfers);
                    Logger.Debug("Keep Filling List");
                    Logger.Debug("------------------------------");
                } catch (Exception ex) {
                    Logger.Error($"Error while collecting tank tranfsers {source.InternalId}. Exception: {ex}");
                    continue;
                }
            }
            return tanksTransfers.ToArray();
        }

        protected override void CollectData()
        {
            Logger.Debug("CollectData");
            try {
                var tankMeasurementResults = _tankMeasurementGroupRead.Read(_tankMeasurementGroupRead.Items);
                var flowmeterMeasurementResults = _flowmeterGroupReport.Read(_flowmeterGroupReport.Items);
                var tankTransferResults = _transferGroupReport.Read(_transferGroupReport.Items);

                var tanksMeasurements = new List<TankMeasurements>();
                var flowmeterMeasurements = new List<FlowmeterMeasurements>();
                var tanksTransfers = new List<TankTransfers>();

                Logger.Debug($"Object ID {ObjectSettings.Objects.First().ObjectId}");
                tanksMeasurements.AddRange(collectTankMeasurements(tankMeasurementResults));
                tanksTransfers.AddRange(collectTankTransfers(tankTransferResults));
                flowmeterMeasurements.AddRange(collectFlowmeterIndicators(flowmeterMeasurementResults));

                QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
                QueueTaskService.Instance.SaveTransfersAsTask(tanksTransfers.ToArray());
                QueueTaskService.Instance.SaveFlowmeterAsTask(flowmeterMeasurements.ToArray());
            } catch (Exception e) {
                Logger.Error(e.StackTrace);
            }
        }

        private static string ToSafeString(object val)
        {
            return val == null ? string.Empty : val.ToString().Replace("\0", string.Empty);
        }
    }
}

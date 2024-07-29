using NLog;
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
using System.Text;
using System.Threading;

namespace Service.Clients.OPC {
    public class OpcMultiServerClient : AMultiServerClient
    {
        private Opc.Da.Server _server;
        private OpcCom.Factory _fact = new OpcCom.Factory();
        private Opc.URL _url;
        private Opc.Da.SubscriptionState _groupReadState = null;
        private Opc.Da.Subscription _groupRead = null;
        private Opc.Da.SubscriptionState _groupReportState = null;
        private Opc.Da.Subscription _groupReport = null;

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
            try
            {
                _fact = new OpcCom.Factory();
                _url = new Opc.URL(ObjectSettings.IpConnectionConfig.IpAddress);
                _server = new Opc.Da.Server(_fact, null);

                var counter = 0;
                while (!Connect())
                {
                    counter++;
                    Logger.Debug("Try connect to server {0}", counter);
                    Thread.Sleep(600);
                }

                if (!InitGroup())
                {
                    Logger.Debug("InitGroup false");
                    Thread.Sleep(60000);
                    Reconnect();
                }
            }
            catch (Exception e) //зациклить 
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
            try
            {
                Logger.Debug($"Try connect to {ObjectSettings.IpConnectionConfig.IpAddress}");
                _server.Connect(_url, new Opc.ConnectData(new NetworkCredential(ObjectSettings.IpConnectionConfig.DComUid, ObjectSettings.IpConnectionConfig.DComPwd)));
            }
            catch (Exception e)
            {
                Logger.Error("Error on {0}  {1}", _url.ToString(), e);
                Logger.Info("uid {0}, pwd {1}", ObjectSettings.IpConnectionConfig.DComUid, ObjectSettings.IpConnectionConfig.DComPwd);
                return false;
            }
            Logger.Debug("Connected to {0}", _url);
            return true;
        }

        private bool InitGroup()
        {
            try
            {
                var isVaild = GetConnectionState();
                if (!isVaild) return false;

                _groupReadState = new Opc.Da.SubscriptionState { Name = "StateGroup", Active = true };
                _groupRead = (Opc.Da.Subscription)_server.CreateSubscription(_groupReadState);

                _groupReportState = new Opc.Da.SubscriptionState { Name = "TransferGroup", Active = true };
                _groupReport = (Opc.Da.Subscription)_server.CreateSubscription(_groupReportState);

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
                    tableBuilder.AppendLine($"ProductName: {source.OilProductType.Value}");
                    tableBuilder.AppendLine("------------------------------");

                    Logger.Debug(tableBuilder.ToString());

                    items.AddIfNotNull(source.TankMeasurementParams.Temperature);
                    items.AddIfNotNull(source.TankMeasurementParams.Density);
                    items.AddIfNotNull(source.TankMeasurementParams.Volume);
                    items.AddIfNotNull(source.TankMeasurementParams.Mass);
                    items.AddIfNotNull(source.TankMeasurementParams.Level);
                    items.AddIfNotNull(source.TankMeasurementParams.DateTimeStamp);
                    items.AddIfNotNull(source.OilProductType.Value.ToString());

                    var itemArray = items.ToArray();
                    itemArray = _groupRead.AddItems(itemArray);
                }

                // Create a transfer groups
                foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(p => p.TankTransferParams != null).ToArray()) {
                    //var tagsSource = source.Tags.Where(t => !string.IsNullOrEmpty(t.Value));
                    //List<Tag> tagList = new List<Tag>();
                    var items = new List<Opc.Da.Item>();

                    var tableBuilder = new StringBuilder();
                    tableBuilder.AppendLine("Measurement Parameters:");
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
                    tableBuilder.AppendLine($"OilProductName: {source.OilProductType.Value}");
                    tableBuilder.AppendLine("------------------------------");

                    items.AddIfNotNull(source.TankTransferParams.StartTime);
                    items.AddIfNotNull(source.TankTransferParams.EndTime);
                    items.AddIfNotNull(source.TankTransferParams.MassStart);
                    items.AddIfNotNull(source.TankTransferParams.MassFinish);
                    items.AddIfNotNull(source.TankTransferParams.LevelStart);
                    items.AddIfNotNull(source.TankTransferParams.LevelFinish);
                    items.AddIfNotNull(source.TankTransferParams.VolumeStart);
                    items.AddIfNotNull(source.TankTransferParams.VolumeFinish);
                    items.AddIfNotNull(source.OilProductType.Value.ToString());

                    var itemArray = items.ToArray();
                    itemArray = _groupReport.AddItems(itemArray);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error on Init group {0}", ex.Message);
                return false;
            }
            Logger.Debug("_groupReport {0}; _groupRead {1}", _groupReport.Items.Count(), _groupRead.Items.Count());

            if (_groupReport.Items.Count() == 0 && _groupRead.Items.Count() == 0)
            {
                Logger.Error("Error there is not a single correct tag! Check tags!");
                return false;
            }
            return true;
        }

        protected override bool StopCollection()
        {
            Logger.Debug("Stop collection from OPC server");

            if (_server == null) return true;

            try
            {
                _server.Disconnect();
                _server.Dispose();
                _server = null;
            }
            catch (Exception e)
            {
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
            try
            {
                _server.GetStatus();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        private TankMeasurements[] collectTankMeasurements(ItemValueResult[] tankMeasurementResults) {
            var tanksMeasurements = new List<TankMeasurements>();

            foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(s => s.TankMeasurementParams != null)) {
                var tankMeasurements = new TankMeasurements { TankId = source.ExternalId.Value };
                var measurement = new TankMeasurementData();

                var tankParams = source.TankMeasurementParams;
                DateTime utcNow = DateTime.UtcNow;
                // Define the time span for UTC+5
                TimeSpan utcPlusFive = new TimeSpan(5, 0, 0);
                // Add the time span to the UTC time
                DateTime utcPlusFiveTime = utcNow.Add(utcPlusFive);
                var now = new DateTime(utcPlusFiveTime.Year, utcPlusFiveTime.Month, utcPlusFiveTime.Day, utcPlusFiveTime.Hour, utcPlusFiveTime.Minute, utcPlusFiveTime.Second, utcPlusFiveTime.Millisecond);
                measurement.Temperature = CommonHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Temperature, Logger);
                measurement.Density = CommonHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Density, Logger);
                measurement.Volume = CommonHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Volume, Logger);
                measurement.Mass = CommonHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Mass, Logger);
                measurement.Level = CommonHelpers.TryGetDecimal(tankMeasurementResults, tankParams.Level, Logger);
                measurement.MeasurementDate = string.IsNullOrEmpty(tankParams.DateTimeStamp) ? now : CommonHelpers.TryGetDateTime(tankMeasurementResults, tankParams.DateTimeStamp, Logger);
                measurement.OilProductType = source.OilProductType;
                measurement.VolumeUnitType = source.VolumeUnitType;
                measurement.LevelUnitType = source.LevelUnitType;
                measurement.MassUnitType = source.MassUnitType;

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
                tankMeasurements.Measurements = new[] { measurement }.SetEnums(source);
                Logger.Debug("Tank Measurement Model Filled with values");
                tanksMeasurements.Add(tankMeasurements);
                Logger.Debug("Keep Filling List");
                Logger.Debug("------------------------------");

            }
            Logger.Debug("TankMeasurement List Filled");

            return tanksMeasurements.ToArray();
        }

        private TankTransfers[] collectTankTransfers(ItemValueResult[] tankTransferResult) {
            var tanksTransfers = new List<TankTransfers>();

            foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(s => s.TankTransferParams != null)) {
                var tankTransfers = new TankTransfers { TankId = source.ExternalId.Value, Transfers = new List<TankTransferData>() };
                var transfer = new TankTransferData();

                var transferParams = source.TankTransferParams;

                Logger.Debug("Start Filling Decimals into Model");
                transfer.VolumeStart = CommonHelpers.TryGetDecimal(tankTransferResult, transferParams.VolumeStart, Logger);
                transfer.VolumeEnd = CommonHelpers.TryGetDecimal(tankTransferResult, transferParams.VolumeFinish, Logger);
                transfer.MassStart = CommonHelpers.TryGetDecimal(tankTransferResult, transferParams.MassStart, Logger);
                transfer.MassEnd = CommonHelpers.TryGetDecimal(tankTransferResult, transferParams.MassFinish, Logger);
                transfer.LevelStart = CommonHelpers.TryGetDecimal(tankTransferResult, transferParams.LevelStart, Logger);
                transfer.LevelEnd = CommonHelpers.TryGetDecimal(tankTransferResult, transferParams.LevelFinish, Logger);
                Logger.Debug("Finish Filling Decimals into Model");
                Logger.Debug("Start Filling Dates into Model");
                transfer.StartDate = CommonHelpers.TryGetDateTime(tankTransferResult, transferParams.StartTime, Logger);
                transfer.EndDate = CommonHelpers.TryGetDateTime(tankTransferResult, transferParams.EndTime, Logger);
                Logger.Debug("Finish Filling Dates into Model");
                transfer.OperationType = transfer.LevelStart < transfer.LevelEnd ? TransferOperationType.Outcome : TransferOperationType.Income;
                if (transfer.StartDate.DateTime <= DateTime.MinValue || transfer.EndDate.DateTime <= DateTime.MinValue) {
                    throw new Exception($"StartDate {transfer.StartDate}, EndDate {transfer.EndDate}. Date error");
                }

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
                tankTransfers.Transfers = new[] { transfer }.SetEnums(source);
                Logger.Debug("Transfer Measurement Model Filled with values");
                tanksTransfers.Add(tankTransfers);
                Logger.Debug("Keep Filling List");
                Logger.Debug("------------------------------");

            }
            return tanksTransfers.ToArray();
        }

        protected override void CollectData()
        {
            Logger.Debug("CollectData");
            try {
                if (_groupRead == null || _groupReport == null) {
                    Logger.Debug("_groupRead or _groupReport is null");
                    return;
                }
                if (_groupRead.Items == null || _groupReport.Items == null) {
                    Logger.Debug("_groupRead.Items or _groupReport.Items is null");
                    return;
                }
                var tankMeasurementResults = _groupRead.Read(_groupRead.Items);
                var tankTransferResults = _groupReport.Read(_groupReport.Items);

                if (
                    tankMeasurementResults == null || 
                    tankMeasurementResults.Length <= 0 || 
                    tankTransferResults == null || 
                    tankTransferResults.Length <= 0
                ) {
                    Logger.Error("group.Read or group.Report return null !");
                    return;
                }

                var tanksMeasurements = collectTankMeasurements(tankMeasurementResults).ToList();
                var tanksTransfers = collectTankTransfers(tankTransferResults).ToList();

                QueueTaskService.Instance.SaveAsTask(tanksMeasurements.ToArray());
                QueueTaskService.Instance.SaveAsTask(tanksTransfers.ToArray());
            } catch (Exception e) {
                Logger.Error(e.StackTrace);
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

        private static string ToSafeString(object val)
        {
            return val == null ? string.Empty : val.ToString().Replace("\0", string.Empty);
        }
    }
}

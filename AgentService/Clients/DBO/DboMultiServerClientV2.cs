﻿using Service.Clients.Client;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Enums;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Clients.DBO {
    public class DboMultiServerClientV2 : AMultiServerClient {
        public DboMultiServerClientV2(ObjectSettings objectSettings, NamedBackgroundWorker worker)
        {
            ObjectSettings = objectSettings;
            Worker = worker;
            Init();
        }

        protected override void Reconnect()
        {
            Logger.Error("Reconnecting...");
            Init();
            _timer.Start();
        }

        protected override void CollectData()
        {
            Logger.Debug("Collect data called");
            try {
                var tanksMeasurements = new List<TankMeasurements>();
                var tanksTransfers = new List<TankTransfers>();
                var flowmeterMeasurements = new List<FlowmeterMeasurements>();

                foreach (var source in ObjectSettings.Objects.First().ObjectSources) {
                    if (source.TankMeasurementParams != null) {
                        var measurementResult = GetAllMeasurementFieldsAsync(source).Result;
                        Logger.Debug($"measurementResult return {measurementResult.Count()} items");
                        tanksMeasurements.Add(
                            new TankMeasurements() { TankId = source.ExternalId.Value, Measurements = measurementResult }
                        );
                    }
                    if (source.TankTransferParams != null) {
                        tanksTransfers.Add(
                            new TankTransfers() { TankId = source.ExternalId.Value, Transfers = GetAllTransferFieldsAsync(source).Result }
                        );
                    }
                    if (source.FlowmeterIndicatorParams != null) {
                        flowmeterMeasurements.Add(
                            new FlowmeterMeasurements() { DeviceId = source.ExternalId.Value, Measurements = GetAllFlowmeterFieldsAsync(source).Result }
                        );
                    }
                }

                QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
                QueueTaskService.Instance.SaveFlowmeterAsTask(flowmeterMeasurements.ToArray());
                QueueTaskService.Instance.SaveTransfersAsTask(tanksTransfers.ToArray());
            } catch (Exception ex) {
                Logger.Error($"Collect data error {ex}");
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

        /// <summary>
        /// Состояние связи с сервером
        /// </summary>
        /// <returns> связь есть-нет</returns>
        protected override bool GetConnectionState()
        {
            try {
                return DboConnectionHelpers.GetConnectionState(ObjectSettings.DatabaseConnectionConfig);
            } catch (Exception e) {
                Logger.Error(e);
                throw new Exception($"{e.Message} {e.StackTrace}");
            }
        }

        private async Task<TankMeasurementData[]> GetAllMeasurementFieldsAsync(ObjectSource objectSource)
        {
            int cmdTimeout = 15;
            var config = ObjectSettings.DatabaseConnectionConfig;

            switch (config.DboType.Value) {
                case DboType.Default: return await DboConnectionHelpers.GetMeasurementDataFromDefaultConnectionAsync(config, cmdTimeout, objectSource, Logger);
                case DboType.Oracle: return await DboConnectionHelpers.GetMeasurementDataFromOracleConnectionAsync(config, cmdTimeout, objectSource, Logger);
                case DboType.MySql: return await DboConnectionHelpers.GetMeasurementDataFromMySqlConnectionAsync(config, cmdTimeout, objectSource, Logger);
                case DboType.FireBird: return await DboConnectionHelpers.GetMeasurementDataFromFireBirdConnectionAsync(config, cmdTimeout, objectSource, Logger);
                default: throw new NotImplementedException(config.DboType.Value.ToString());
            }
        }

        private async Task<FlowmeterMeasurementData[]> GetAllFlowmeterFieldsAsync(ObjectSource objectSource)
        {
            int cmdTimeout = 15;
            var config = ObjectSettings.DatabaseConnectionConfig;
            switch (config.DboType.Value) {
                case DboType.Default: return await DboConnectionHelpers.GetFlowmeterMeasurementDataFromDefaultConnectionAsync(config, cmdTimeout, objectSource, Logger);
                case DboType.Oracle: return await DboConnectionHelpers.GetFlowmeterMeasurementDataFromOracleConnectionAsync(config, cmdTimeout, objectSource, Logger);
                case DboType.MySql: return await DboConnectionHelpers.GetFlowmeterMeasurementDataFromMySqlConnectionAsync(config, cmdTimeout, objectSource, Logger);
                case DboType.FireBird: return await DboConnectionHelpers.GetFlowmeterMeasurementDataFromFireBirdConnectionAsync(config, cmdTimeout, objectSource, Logger);
                default: throw new NotImplementedException(config.DboType.Value.ToString());
            }
        }

        private async Task<TankTransferData[]> GetAllTransferFieldsAsync(ObjectSource objectSource)
        {
            int cmdTimeout = 15;
            var config = ObjectSettings.DatabaseConnectionConfig;
            switch (config.DboType.Value) {
                case DboType.Default: return await DboConnectionHelpers.GetTransferDataFromDefaultConnectionAsync(cmdTimeout, objectSource, config, Logger);
                case DboType.Oracle: return await DboConnectionHelpers.GetTransferDataFromOracleConnectionAsync(cmdTimeout, objectSource, config, Logger);
                case DboType.MySql: return await DboConnectionHelpers.GetTransferDataFromMySqlConnectionAsync(cmdTimeout, objectSource, config, Logger);
                case DboType.FireBird: return await DboConnectionHelpers.GetTransferDataFromFireBirdConnectionAsync(cmdTimeout, objectSource, config, Logger);
                default: throw new NotImplementedException(config.DboType.Value.ToString());
            }
        }

    }
}
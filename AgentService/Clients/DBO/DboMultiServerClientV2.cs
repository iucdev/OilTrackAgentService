using Service.Clients.Client;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Enums;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Clients.DBO {
    public class DboMultiServerClientV2 : AMultiServerClient
    {
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

                foreach (var source in ObjectSettings.Objects.First().ObjectSources) {
                    tanksMeasurements.Add(
                        new TankMeasurements() { TankId = source.ExternalId.Value, Measurements = GetAllMeasurementFields(source) }
                    );
                    tanksTransfers.Add(
                        new TankTransfers() { TankId = source.ExternalId.Value, Transfers = GetAllTransferFields(source) }
                    );
                }

                QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
                QueueTaskService.Instance.SaveTransfersAsTask(tanksTransfers.ToArray());
            } catch (Exception ex) { 
                Logger.Error($"Collect data error {ex.Message + ex.StackTrace}");
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
            try
            {
                return DboConnectionHelpers.GetConnectionState(ObjectSettings.DatabaseConnectionConfig);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw new Exception($"{e.Message} {e.StackTrace}");
            }
        }

        

        private TankMeasurementData[] GetAllMeasurementFields(ObjectSource objectSource) {
            int cmdTimeout = 15;
            var config = ObjectSettings.DatabaseConnectionConfig;
            switch (config.DboType.Value) {
                case DboType.Default: return DboConnectionHelpers.GetMeasurementDataFromDefaultConnection(config, cmdTimeout, objectSource, Logger);
                case DboType.Oracle: return DboConnectionHelpers.GetMeasurementDataFromOracleConnection(config, cmdTimeout, objectSource, Logger);
                case DboType.MySql: return DboConnectionHelpers.GetMeasurementDataFromMySqlConnection(config, cmdTimeout, objectSource, Logger);
                case DboType.FireBird: return DboConnectionHelpers.GetMeasurementDataFromFireBirdConnection(config, cmdTimeout, objectSource, Logger);
                default: throw new NotImplementedException(config.DboType.Value.ToString());
            }
        }

        private TankTransferData[] GetAllTransferFields(ObjectSource objectSource) {
            int cmdTimeout = 15;
            var config = ObjectSettings.DatabaseConnectionConfig;
            switch (config.DboType.Value) {
                case DboType.Default: return DboConnectionHelpers.GetTransferDataFromDefaultConnection(cmdTimeout, objectSource, config, Logger);
                case DboType.Oracle: return DboConnectionHelpers.GetTransferDataFromOracleConnection(cmdTimeout, objectSource, config, Logger);
                case DboType.MySql: return DboConnectionHelpers.GetTransferDataFromMySqlConnection(cmdTimeout, objectSource, config, Logger);
                case DboType.FireBird: return DboConnectionHelpers.GetTransferDataFromFireBirdConnection(cmdTimeout, objectSource, config, Logger);
                default: throw new NotImplementedException(config.DboType.Value.ToString());
            }
        }

    }
}

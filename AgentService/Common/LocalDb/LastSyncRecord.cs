using Newtonsoft.Json;
using NLog;
using Service.Clients.Scheduler;
using Service.Dtos;
using Service.Enums;
using Sunp.Api.Client;
using System;
using System.Data.SQLite;
using System.Linq;

namespace Service.LocalDb {
    public class LastSyncRecord {
        public string InternalTankId { get; set; }
        public long ExternalTankId { get; set; }
        public DateTime LastMeasurementsSyncDate { get; set; }
        public DateTime LastTransfersSyncDate { get; set; }
        public DateTime LastFlowmeterSyncDate { get; set; }

        public static void Update(QueueTaskRecord task, Logger logger)
        {
            try {
                logger.Debug($"LastSyncRecord->Update call");
                switch (task.Type) {
                    case QueueTaskType.SendTankMeasurements:
                        var items = JsonConvert.DeserializeObject<TankMeasurements[]>(task.Items);
                        foreach (var tankMeasurements in items) {
                            if (tankMeasurements.Measurements.Any()) {
                                continue;
                            }
                            var row = new LastSyncRecord
                            {
                                ExternalTankId = tankMeasurements.TankId,
                                LastMeasurementsSyncDate = tankMeasurements.Measurements.Max(t => t.MeasurementDate).DateTime
                            };
                            row.UpdateTankMeasurementsInDb(logger);
                        }
                        break;
                    case QueueTaskType.SendTankTransfer:
                        var tanksTransfers = JsonConvert.DeserializeObject<TankTransfers[]>(task.Items);
                        foreach (var tankTransfers in tanksTransfers) {
                            if (tankTransfers.Transfers.Any()) {
                                continue;
                            }
                            var row = new LastSyncRecord
                            {
                                ExternalTankId = tankTransfers.TankId,
                                LastTransfersSyncDate = tankTransfers.Transfers.Max(t => t.EndDate).DateTime
                            };
                            row.UpdateTankTransfersInDb(logger);
                        }
                        break;
                    case QueueTaskType.SendFlowmeterMeasurements:
                        var flowmeterItems = JsonConvert.DeserializeObject<FlowmeterMeasurements[]>(task.Items);
                        foreach (var flowmeter in flowmeterItems) {
                            if (flowmeter.Measurements.Any()) {
                                continue;
                            }
                            var row = new LastSyncRecord
                            {
                                ExternalTankId = flowmeter.FlowmeterId,
                                LastFlowmeterSyncDate = flowmeter.Measurements.Max(t => t.MeasurementDate).DateTime
                            };
                            row.UpdateFlowmeterMeasutementsInDb(logger);
                        }
                        break;
                }
                logger.Debug($"LastSyncRecord->Update success");
            } catch (Exception ex) {
                logger.Error($"LastSyncRecord->Update exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public static LastSyncRecord GetByExternalId(long externalTankId, Logger logger)
        {
            try {
                logger.Debug($"LastSyncRecord->GetByExternalId call ({nameof(externalTankId)} {externalTankId})");
                using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                    connection.Open();
                    var query = $@"
                        SELECT * FROM {nameof(LastSyncRecord)}
                        WHERE {nameof(ExternalTankId)} = @{nameof(ExternalTankId)}
                    ";
                    using (var command = new SQLiteCommand(query, connection)) {
                        command.Parameters.AddWithValue("@ExternalTankId", externalTankId);
                        using (var reader = command.ExecuteReader()) {
                            if (reader.Read()) {
                                logger.Debug($"LastSyncRecord->GetByExternalId success");
                                return new LastSyncRecord
                                {
                                    InternalTankId = reader["InternalTankId"].ToString(),
                                    ExternalTankId = externalTankId,
                                    LastMeasurementsSyncDate = DateTime.Parse(reader["LastMeasurementsSyncDate"].ToString()),
                                    LastTransfersSyncDate = DateTime.Parse(reader["LastTransfersSyncDate"].ToString())
                                };
                            } else {
                                var error = $"LastSyncRecord->GetByExternalId error: not found in db";
                                logger.Error(error);
                                throw new Exception(error);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error($"LastSyncRecord->GetByExternalId exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public void AddToDbIfNotExists(Logger logger)
        {
            try {
                logger.Debug("LastSyncRecord->AddToDbIfNotExists call");
                var count = 0;
                using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                    connection.Open();
                    string countQuery = $@"
                        SELECT COUNT(*) FROM {nameof(LastSyncRecord)}
                        WHERE {nameof(InternalTankId)} = @{nameof(InternalTankId)}
                        AND {nameof(ExternalTankId)} = @{nameof(ExternalTankId)}";

                    using (var command = new SQLiteCommand(countQuery, connection)) {
                        command.Parameters.AddWithValue($"@{nameof(InternalTankId)}", InternalTankId);
                        command.Parameters.AddWithValue($"@{nameof(ExternalTankId)}", ExternalTankId);
                        count = Convert.ToInt32(command.ExecuteScalar());
                    }
                }

                if (count > 0) {
                    logger.Debug($"LastSyncRecord->AddToDbIfNotExists success ({ExternalTankId})");
                    return;
                }

                using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                    connection.Open();
                    string insertQuery = $@"
                        INSERT INTO {nameof(LastSyncRecord)} (
                            {nameof(InternalTankId)}, 
                            {nameof(ExternalTankId)}, 
                            {nameof(LastMeasurementsSyncDate)},
                            {nameof(LastFlowmeterSyncDate)},
                            {nameof(LastTransfersSyncDate)}
                        ) VALUES (
                            @{nameof(InternalTankId)}, 
                            @{nameof(ExternalTankId)}, 
                            @{nameof(LastMeasurementsSyncDate)},
                            @{nameof(LastFlowmeterSyncDate)},
                            @{nameof(LastTransfersSyncDate)}
                        )";

                    using (var command = new SQLiteCommand(insertQuery, connection)) {
                        command.Parameters.AddWithValue($"@{nameof(InternalTankId)}", InternalTankId);
                        command.Parameters.AddWithValue($"@{nameof(ExternalTankId)}", ExternalTankId);
                        command.Parameters.AddWithValue($"@{nameof(LastMeasurementsSyncDate)}", LastMeasurementsSyncDate);
                        command.Parameters.AddWithValue($"@{nameof(LastFlowmeterSyncDate)}", LastFlowmeterSyncDate);
                        command.Parameters.AddWithValue($"@{nameof(LastTransfersSyncDate)}", LastTransfersSyncDate);

                        command.ExecuteNonQuery();
                    }
                }
                logger.Debug($"LastSyncRecord->AddToDbIfNotExists success ({ExternalTankId})");
            } catch (Exception ex) {
                logger.Error($"LastSyncRecord->AddToDbIfNotExists exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public void UpdateTankMeasurementsInDb(Logger logger)
        {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string updateQuery = $@"
                UPDATE {nameof(LastSyncRecord)} SET 
                    {nameof(LastMeasurementsSyncDate)} = @{nameof(LastMeasurementsSyncDate)}
                WHERE {nameof(ExternalTankId)} = @{nameof(ExternalTankId)}";

                using (var command = new SQLiteCommand(updateQuery, connection)) {
                    command.Parameters.AddWithValue($"@{nameof(ExternalTankId)}", ExternalTankId);
                    command.Parameters.AddWithValue($"@{nameof(LastMeasurementsSyncDate)}", LastMeasurementsSyncDate.ToDbString());
                    command.ExecuteNonQuery();
                }
            }
        }
        public void UpdateTankTransfersInDb(Logger logger)
        {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string updateQuery = $@"
                UPDATE {nameof(LastSyncRecord)} SET 
                    {nameof(LastTransfersSyncDate)} = @{nameof(LastTransfersSyncDate)}
                WHERE {nameof(ExternalTankId)} = @{nameof(ExternalTankId)}";

                using (var command = new SQLiteCommand(updateQuery, connection)) {
                    command.Parameters.AddWithValue($"@{nameof(ExternalTankId)}", ExternalTankId);
                    command.Parameters.AddWithValue($"@{nameof(LastTransfersSyncDate)}", LastTransfersSyncDate.ToDbString());
                    command.ExecuteNonQuery();
                }
            }
        }
        public void UpdateFlowmeterMeasutementsInDb(Logger logger)
        {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string updateQuery = $@"
                UPDATE {nameof(LastSyncRecord)} SET 
                    {nameof(LastFlowmeterSyncDate)} = @{nameof(LastFlowmeterSyncDate)}
                WHERE {nameof(ExternalTankId)} = @{nameof(ExternalTankId)}";

                using (var command = new SQLiteCommand(updateQuery, connection)) {
                    command.Parameters.AddWithValue($"@{nameof(ExternalTankId)}", ExternalTankId);
                    command.Parameters.AddWithValue($"@{nameof(LastFlowmeterSyncDate)}", LastFlowmeterSyncDate.ToDbString());
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

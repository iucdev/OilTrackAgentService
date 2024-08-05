using Newtonsoft.Json;
using NLog;
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

        public static void Update(QueueTaskRecord task, Logger logger)
        {
            try { 
                logger.Debug($"LastSyncRecord->Update call");
                switch (task.Type) {
                    case QueueTaskType.SendTankMeasurements:
                        var items = JsonConvert.DeserializeObject<TankMeasurements[]>(task.Items);
                        foreach (var tankMeasurements in items) {
                            var row = new LastSyncRecord
                            {
                                ExternalTankId = tankMeasurements.TankId,
                                LastMeasurementsSyncDate = tankMeasurements.Measurements.Max(t => t.MeasurementDate).DateTime
                            };
                            row.UpdateInDb(logger);
                        }
                        break;
                    case QueueTaskType.SendTankTransfer:
                        var tanksTransfers = JsonConvert.DeserializeObject<TankTransfers[]>(task.Items);
                        foreach (var tankTransfers in tanksTransfers) {
                            var row = new LastSyncRecord
                            {
                                ExternalTankId = tankTransfers.TankId,
                                LastTransfersSyncDate = tankTransfers.Transfers.Max(t => t.EndDate).DateTime
                            };
                            row.UpdateInDb(logger);
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

        public static LastSyncRecord Get(string internalTankId, long externalTankId, Logger logger)
        {
            try {
                logger.Debug($"LastSyncRecord->Get call ({nameof(externalTankId)} {externalTankId}, {nameof(internalTankId)} {internalTankId})");
                using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                    connection.Open();
                    var query = $@"
                        SELECT * FROM {nameof(LastSyncRecord)}
                        WHERE {nameof(InternalTankId)} = @{nameof(InternalTankId)}
                        AND {nameof(ExternalTankId)} = @{nameof(ExternalTankId)}
                    ";
                    using (var command = new SQLiteCommand(query, connection)) {
                        command.Parameters.AddWithValue("@InternalTankId", internalTankId);
                        command.Parameters.AddWithValue("@ExternalTankId", externalTankId);
                        using (var reader = command.ExecuteReader()) {
                            if (reader.Read()) {
                                logger.Debug($"LastSyncRecord->Get success");
                                return new LastSyncRecord
                                {
                                    InternalTankId = internalTankId,
                                    ExternalTankId = externalTankId,
                                    LastMeasurementsSyncDate = DateTime.Parse(reader["LastMeasurementsSyncDate"].ToString()),
                                    LastTransfersSyncDate = DateTime.Parse(reader["LastTransfersSyncDate"].ToString())
                                };
                            } else {
                                var error = $"LastSyncRecord->Get error: not found in db";
                                logger.Error(error);
                                throw new Exception(error);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error($"LastSyncRecord->Get exception: {ex.Message + ex.StackTrace}");
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
                    WHERE {nameof(InternalTankId)} = @{nameof(InternalTankId)}";

                    using (var command = new SQLiteCommand(countQuery, connection)) {
                        command.Parameters.AddWithValue("@InternalTankId", InternalTankId);
                        count = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
                if (count > 0) {
                    logger.Debug("LastSyncRecord->AddToDbIfNotExists success");
                    return;
                }
                using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                    connection.Open();
                    string insertQuery = $@"
                    INSERT INTO LastSyncRecord (
                        InternalTankId, 
                        ExternalTankId, 
                        LastMeasurementsSyncDate,
                        LastTransfersSyncDate
                    ) VALUES (
                        @InternalTankId, 
                        @ExternalTankId, 
                        @LastMeasurementsSyncDate,
                        @LastTransfersSyncDate
                    )";

                    using (var command = new SQLiteCommand(insertQuery, connection)) {
                        command.Parameters.AddWithValue("@InternalTankId", InternalTankId);
                        command.Parameters.AddWithValue("@ExternalTankId", ExternalTankId);
                        command.Parameters.AddWithValue("@LastMeasurementsSyncDate", LastMeasurementsSyncDate);
                        command.Parameters.AddWithValue("@LastTransfersSyncDate", LastTransfersSyncDate);

                        command.ExecuteNonQuery();
                    }
                }
                logger.Debug("LastSyncRecord->AddToDbIfNotExists success");
            } catch (Exception ex) {
                logger.Error($"LastSyncRecord->AddToDbIfNotExists exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public void UpdateInDb(Logger logger)
        {
            try {
                logger.Debug("LastSyncRecord->UpdateInDb call");
                using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                    connection.Open();
                    string updateQuery = $@"
                        UPDATE LastSyncRecord SET 
                            {nameof(InternalTankId)} = @{nameof(InternalTankId)}, 
                            {nameof(ExternalTankId)} = @{nameof(ExternalTankId)}, 
                            {nameof(LastMeasurementsSyncDate)} = @{nameof(LastMeasurementsSyncDate)},
                            {nameof(LastTransfersSyncDate)} = @{nameof(LastTransfersSyncDate)}
                        WHERE {nameof(InternalTankId)} = @{nameof(InternalTankId)}";

                    using (var command = new SQLiteCommand(updateQuery, connection)) {
                        command.Parameters.AddWithValue($"@{nameof(InternalTankId)}", InternalTankId);
                        command.Parameters.AddWithValue($"@{nameof(ExternalTankId)}", ExternalTankId);
                        command.Parameters.AddWithValue($"@{nameof(LastMeasurementsSyncDate)}", LastMeasurementsSyncDate.ToDbString());
                        command.Parameters.AddWithValue($"@{nameof(LastTransfersSyncDate)}", LastTransfersSyncDate.ToDbString());
                        command.ExecuteNonQuery();
                    }
                }
                logger.Debug("LastSyncRecord->UpdateInDb success");
            } catch (Exception ex) { 
                logger.Error($"LastSyncRecord->UpdateInDb exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }
    }
}

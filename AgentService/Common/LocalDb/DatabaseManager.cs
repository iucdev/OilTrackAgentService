using AgentService.Models;
using AgentService.References;
using Newtonsoft.Json;
using NLog;
using Service.Clients.Utils;
using Service.Dtos;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Service.LocalDb {
    public class DatabaseManager {
        private static readonly string _currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string ConnectionString = $"Data Source={_currentDirectory}database.db;Version=3;";
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static void InitializeDatabase() {
            try {
                using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                    connection.Open();
                    string createTankTransferRecordTableQuery = $@"
                    CREATE TABLE IF NOT EXISTS {nameof(TankTransferRecord)}(
                        {nameof(TankTransferRecord.InternalTankId)} TEXT,
                        {nameof(TankTransferRecord.ExternalTankId)} INTEGER,

                        {nameof(TankTransferRecord.StartDate)} TEXT,
                        {nameof(TankTransferRecord.EndDate)} TEXT,
                        
                        {nameof(TankTransferRecord.LevelStart)} NUMERIC,
                        {nameof(TankTransferRecord.LevelEnd)} NUMERIC,
                        {nameof(TankTransferRecord.LevelUnitType)} TEXT,
                        
                        {nameof(TankTransferRecord.MassStart)} NUMERIC,
                        {nameof(TankTransferRecord.MassEnd)} NUMERIC,
                        {nameof(TankTransferRecord.MassUnitType)} TEXT,
                        
                        {nameof(TankTransferRecord.VolumeStart)} NUMERIC,
                        {nameof(TankTransferRecord.VolumeEnd)} NUMERIC,
                        {nameof(TankTransferRecord.VolumeUnitType)} TEXT,
                        
                        {nameof(TankTransferRecord.OperationType)} TEXT,
                        {nameof(TankTransferRecord.OilProductType)} TEXT
                    )";

                    using (var command = new SQLiteCommand(createTankTransferRecordTableQuery, connection)) {
                        command.ExecuteNonQuery();
                    }

                    string createQueueTaskRecordTableQuery = $@"
                    CREATE TABLE IF NOT EXISTS {nameof(QueueTaskRecord)}(
                        {nameof(QueueTaskRecord.Id)} INTEGER PRIMARY KEY, 
                        {nameof(QueueTaskRecord.CreateDate)} TEXT, 
                        {nameof(QueueTaskRecord.PackageId)} TEXT, 
                        {nameof(QueueTaskRecord.Type)} TEXT, 
                        {nameof(QueueTaskRecord.Status)} TEXT, 
                        {nameof(QueueTaskRecord.Items)} TEXT, 
                        {nameof(QueueTaskRecord.Error)} TEXT
                    )";

                    using (var command = new SQLiteCommand(createQueueTaskRecordTableQuery, connection)) {
                        command.ExecuteNonQuery();
                    }

                    string createLastSyncRecordTableQuery = $@"
                    CREATE TABLE IF NOT EXISTS {nameof(LastSyncRecord)}(
                        {nameof(LastSyncRecord.InternalTankId)} TEXT, 
                        {nameof(LastSyncRecord.ExternalTankId)} INTEGER, 
                        {nameof(LastSyncRecord.LastFlowmeterSyncDate)} TEXT, 
                        {nameof(LastSyncRecord.LastMeasurementsSyncDate)} TEXT, 
                        {nameof(LastSyncRecord.LastTransfersSyncDate)} TEXT
                    )";

                    using (var command = new SQLiteCommand(createLastSyncRecordTableQuery, connection)) {
                        command.ExecuteNonQuery();
                    }
                }
            } catch (Exception ex) {
                throw ex;
            }

            var objectSettings = ObjectSettingsSingleton.Instance.ObjectSettings;
            var initLastSyncDate = objectSettings.StartFrom.HasValue ? objectSettings.StartFrom.Value : DateTime.Now.AddYears(-1);
            foreach (var @object in objectSettings.Objects) {
                @object.ObjectSources.ForEach(e => new LastSyncRecord
                {
                    InternalTankId = e.InternalId,
                    ExternalTankId = e.ExternalId.Value,
                    LastFlowmeterSyncDate = initLastSyncDate,
                    LastMeasurementsSyncDate = initLastSyncDate,
                    LastTransfersSyncDate = initLastSyncDate
                }.AddToDbIfNotExists(_logger));
            }
        }

        public static async Task<List<TankIndicatorRecord>> LoadTankIndicatorsDataAsync() {
            List<TankIndicatorRecord> TankIndicators = new List<TankIndicatorRecord>();
            var data = await LoadQueueTaskRecordDataAsync();
            var queueRecords = data.Where(t => t.Type == QueueTaskType.SendTankMeasurements).ToArray();
            var objectSettings = ObjectSettingsSingleton.Instance.ObjectSettings.Objects.First();
            foreach (var record in queueRecords.OrderByDescending(t => t.CreateTime)) {
                var items = JsonConvert.DeserializeObject<TankIndicator[]>(record.Items);
                foreach (var item in items) {
                    var internalTankId = objectSettings.ObjectSources.FirstOrDefault(t => t.ExternalId == item.TankId).InternalId;
                    foreach (var indicator in item.Measurements) {
                        TankIndicators.Add(new TankIndicatorRecord {
                            InternalTankId = internalTankId,
                            TankIndicators = indicator
                        });
                    }
                }
            }

            return TankIndicators;
        }

        public static async Task<List<TankTransferRecord>> LoadTankTransfersDataAsync() {
            List<TankTransferRecord> tankTransfers = new List<TankTransferRecord>();

            // Загружаем записи из очереди
            var queueRecords = (await LoadTankTransfersDataFromDbAsync()).ToArray();

            // Обрабатываем каждую запись
            var objectSettings = ObjectSettingsSingleton.Instance.ObjectSettings.Objects.First();
            foreach (var record in queueRecords.OrderByDescending(t => t.StartDate)) {
                var internalTankId = objectSettings.ObjectSources.FirstOrDefault(t => t.ExternalId == record.ExternalTankId).InternalId;
                tankTransfers.Add(new TankTransferRecord {
                    InternalTankId = internalTankId,
                    ExternalTankId = record.ExternalTankId,
                    StartDate = record.StartDate,
                    EndDate = record.EndDate,
                    LevelEnd = record.LevelEnd,
                    LevelStart = record.LevelStart,
                    LevelUnitType = record.LevelUnitType,
                    MassEnd = record.MassEnd,
                    MassStart = record.MassStart,
                    OilProductType = record.OilProductType,
                    MassUnitType = record.MassUnitType,
                    OperationType = record.OperationType,
                    VolumeEnd = record.VolumeEnd,
                    VolumeStart = record.VolumeStart,
                    VolumeUnitType = record.VolumeUnitType
                });
            }

            // Извлекаем уникальные записи
            var uniqueRecords = tankTransfers
                .GroupBy(record => new { record.ExternalTankId, record.StartDate })
                .Select(group => group.First())
                .ToList();

            // Возвращаем уникальные записи как ObservableCollection
            return new List<TankTransferRecord>(uniqueRecords);
        }

        public static async Task<List<QueueTaskRecord>> LoadQueueTaskRecordDataAsync() {
            List<QueueTaskRecord> QueueTask = new List<QueueTaskRecord>();
            using (var connection = new SQLiteConnection(ConnectionString)) {
                await connection.OpenAsync();

                var commandQuery = $@"
SELECT 
{nameof(QueueTaskRecord.Id)}, 
{nameof(QueueTaskRecord.PackageId)}, 
{nameof(QueueTaskRecord.CreateDate)}, 
{nameof(QueueTaskRecord.Type)}, 
{nameof(QueueTaskRecord.Status)}, 
{nameof(QueueTaskRecord.Items)}, 
{nameof(QueueTaskRecord.Error)}
FROM {nameof(QueueTaskRecord)}";
                using (var command = new SQLiteCommand(commandQuery, connection)) {
                    using (var reader = await command.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            try {
                                var Id = reader.GetInt32(reader.GetOrdinal($"{nameof(QueueTaskRecord.Id)}"));
                                var PackageId = reader.GetString(reader.GetOrdinal($"{nameof(QueueTaskRecord.PackageId)}"));
                                var CreateDate = reader.GetDateTime(reader.GetOrdinal($"{nameof(QueueTaskRecord.CreateDate)}"));
                                var Error = reader[$"{nameof(QueueTaskRecord.Error)}"].ToString();
                                var Items = reader.GetString(reader.GetOrdinal($"{nameof(QueueTaskRecord.Items)}"));
                                var Status = Enum.TryParse<QueueTaskStatus>(reader.GetString(reader.GetOrdinal($"{nameof(QueueTaskRecord.Status)}")), out var status) ? status : throw new NotImplementedException(reader.GetString(reader.GetOrdinal("Status")));
                                var Type = Enum.TryParse<QueueTaskType>(reader.GetString(reader.GetOrdinal($"{nameof(QueueTaskRecord.Type)}")), out var type) ? type : throw new NotImplementedException(reader.GetString(reader.GetOrdinal("Type")));
                                QueueTask.Add(new QueueTaskRecord {
                                    Id = Id,
                                    PackageId = PackageId,
                                    CreateDate = CreateDate,
                                    Error = Error,
                                    Items = Items,
                                    Status = Status,
                                    Type = Type,
                                });
                            }catch(Exception ex) {
                                Debug.WriteLine(ex);
                            }
                        }
                    }
                }
            }

            return QueueTask;
        }

        public static async Task<ObservableCollection<TankTransferRecord>> LoadTankTransfersDataFromDbAsync() {
            ObservableCollection<TankTransferRecord> tankTransfers = new ObservableCollection<TankTransferRecord>();
            using (var connection = new SQLiteConnection(ConnectionString)) {
                await connection.OpenAsync();

                var commandQuery = $@"SELECT * FROM {nameof(TankTransferRecord)}";
                using (var command = new SQLiteCommand(commandQuery, connection)) {
                    using (var reader = await command.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            try {
                                var InternalTankId = reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.InternalTankId)}"));
                                var ExternalTankId = reader.GetInt64(reader.GetOrdinal($"{nameof(TankTransferRecord.ExternalTankId)}"));
                                var StartDate = reader.GetDateTime(reader.GetOrdinal($"{nameof(TankTransferRecord.StartDate)}"));
                                var EndDate = reader.GetDateTime(reader.GetOrdinal($"{nameof(TankTransferRecord.EndDate)}"));
                                var LevelStart = reader.GetDecimal(reader.GetOrdinal($"{nameof(TankTransferRecord.LevelStart)}"));
                                var LevelEnd = reader.GetDecimal(reader.GetOrdinal($"{nameof(TankTransferRecord.LevelEnd)}"));
                                var LevelUnitType = Enum.TryParse<LevelUnitType>(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.LevelUnitType)}")), out var leveltype) ? leveltype : throw new NotImplementedException(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.LevelUnitType)}")));
                                
                                var MassStart = reader.GetDecimal(reader.GetOrdinal($"{nameof(TankTransferRecord.MassStart)}"));
                                var MassEnd = reader.GetDecimal(reader.GetOrdinal($"{nameof(TankTransferRecord.MassEnd)}"));
                                var MassUnitType = Enum.TryParse<MassUnitType>(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.MassUnitType)}")), out var masstype) ? masstype : throw new NotImplementedException(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.MassUnitType)}")));

                                var VolumeStart = reader.GetDecimal(reader.GetOrdinal($"{nameof(TankTransferRecord.VolumeStart)}"));
                                var VolumeEnd = reader.GetDecimal(reader.GetOrdinal($"{nameof(TankTransferRecord.VolumeEnd)}"));
                                var VolumeUnitType = Enum.TryParse<VolumeUnitType>(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.VolumeUnitType)}")), out var volumetype) ? volumetype : throw new NotImplementedException(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.VolumeUnitType)}")));
                                
                                
                                var OilProductType = Enum.TryParse<OilProductType>(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.OilProductType)}")), out var oiltype) ? oiltype : throw new NotImplementedException(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.OilProductType)}")));
                                var OperationType = Enum.TryParse<TransferOperationType>(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.OperationType)}")), out var operationtype) ? operationtype : throw new NotImplementedException(reader.GetString(reader.GetOrdinal($"{nameof(TankTransferRecord.OperationType)}")));

                                tankTransfers.Add(new TankTransferRecord {
                                    InternalTankId = InternalTankId,
                                    ExternalTankId = ExternalTankId,
                                    StartDate = StartDate,
                                    EndDate = EndDate,
                                    
                                    LevelStart = LevelStart,
                                    LevelEnd = LevelEnd,
                                    LevelUnitType = LevelUnitType,
                                    
                                    MassStart = MassStart,
                                    MassEnd = MassEnd,
                                    MassUnitType = MassUnitType,

                                    VolumeStart = VolumeStart,
                                    VolumeEnd = VolumeEnd,
                                    VolumeUnitType = VolumeUnitType,

                                    OilProductType = OilProductType,
                                    OperationType = OperationType
                                });
                            } catch (Exception ex) {
                                Console.WriteLine(ex);
                            }
                        }
                    }
                }
            }

            return tankTransfers;
        }
    }
}

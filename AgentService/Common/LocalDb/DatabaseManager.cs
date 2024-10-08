﻿using NLog;
using Service.Clients.Utils;
using System;
using System.Data.SQLite;

namespace Service.LocalDb {
    public class DatabaseManager {
        private static readonly string _currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string ConnectionString = $"Data Source={_currentDirectory}database.db;Version=3;";
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static void InitializeDatabase() {
            try {
                using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                    connection.Open();
                    string createTankTransferRecordTableQuery = @"
                    CREATE TABLE IF NOT EXISTS TankTransferRecord(
                        InternalTankId TEXT,
                        StartDate TEXT,
                        EndDate TEXT,
                        LevelStart NUMERIC,
                        LevelEnd NUMERIC,
                        LevelUnitType TEXT,
                        MassStart NUMERIC,
                        MassEnd NUMERIC,
                        MassUnitType TEXT,
                        VolumeStart NUMERIC,
                        VolumeEnd NUMERIC,
                        VolumeUnitType TEXT,
                        OperationType TEXT,
                        OilProductType TEXT
                    )";

                    using (var command = new SQLiteCommand(createTankTransferRecordTableQuery, connection)) {
                        command.ExecuteNonQuery();
                    }

                    string createQueueTaskRecordTableQuery = @"
                    CREATE TABLE IF NOT EXISTS QueueTaskRecord(
                        Id INTEGER PRIMARY KEY, 
                        CreateDate TEXT, 
                        UpdateDate TEXT, 
                        Type TEXT, 
                        Status TEXT, 
                        Items TEXT, 
                        Error TEXT
                    )";

                    using (var command = new SQLiteCommand(createQueueTaskRecordTableQuery, connection)) {
                        command.ExecuteNonQuery();
                    }

                    string createLastSyncRecordTableQuery = @"
                    CREATE TABLE IF NOT EXISTS LastSyncRecord(
                        InternalTankId TEXT, 
                        ExternalTankId INTEGER, 
                        LastFlowmeterSyncDate TEXT, 
                        LastMeasurementsSyncDate TEXT, 
                        LastTransfersSyncDate TEXT
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
    }
}

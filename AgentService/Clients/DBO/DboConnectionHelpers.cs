using FirebirdSql.Data.FirebirdClient;
using MySql.Data.MySqlClient;
using NLog;
using Oracle.DataAccess.Client;
using Service.Clients.Utils;
using Service.Enums;
using Service.LocalDb;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Service.Clients.DBO {
    public static class DboConnectionHelpers {
        private static TankTransferData ReadTransfersFromDb(this DbDataReader reader, ObjectSource objectSource) {
            var data = new TankTransferData {
                StartDate = reader.GetDateTime(reader.GetOrdinal($"{objectSource.TankTransferParams.StartTime}")),
                EndDate = reader.GetDateTime(reader.GetOrdinal($"{objectSource.TankTransferParams.EndTime}")),
                MassStart = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankTransferParams.MassStart}")),
                MassEnd = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankTransferParams.MassFinish}")),
                LevelStart = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelStart}")),
                LevelEnd = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelFinish}")),
                VolumeStart = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeStart}")),
                VolumeEnd = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeFinish}")),
                LevelUnitType = objectSource.LevelUnitType.Value,
                MassUnitType = objectSource.MassUnitType.Value,
                OilProductType = objectSource.OilProductType.Value,
                VolumeUnitType = objectSource.VolumeUnitType.Value
            };
            return data;
        }

        private static TankMeasurementData ReadMeasurementFromDb(this DbDataReader reader, ObjectSource objectSource) {
            var data = new TankMeasurementData {
                Temperature = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Temperature}")),
                Level = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Level}")),
                Volume = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Volume}")),
                Mass = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Mass}")),
                MeasurementDate = reader.GetDateTime(reader.GetOrdinal($"{objectSource.TankMeasurementParams.DateTimeStamp}")),
                Density = reader.GetDecimal(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Density}")),
                LevelUnitType = objectSource.LevelUnitType.Value,
                MassUnitType = objectSource.MassUnitType.Value,
                OilProductType = objectSource.OilProductType.Value,
                VolumeUnitType = objectSource.VolumeUnitType.Value
            };
            return data;
        }

        #region TransferData

        public static TankTransferData[] GetTransferDataFromDefaultConnection(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new SqlConnection(config.ConnectionString)) {
                    conn.Open();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, DboType.Default, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                list.Add(reader.ReadTransfersFromDb(objectSource));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (System.Data.SqlClient.SqlException ex) {
                throw defaultSqlExceptionProcessor(ex);
            }
        }

        public static TankTransferData[] GetTransferDataFromMySqlConnection(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new MySqlConnection(config.ConnectionString)) {
                    conn.Open();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, DboType.MySql, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                list.Add(reader.ReadTransfersFromDb(objectSource));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (MySql.Data.MySqlClient.MySqlException ex) {
                throw mySqlExceptionProcessor(ex);
            }
        }

        public static TankTransferData[] GetTransferDataFromOracleConnection(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new OracleConnection(config.ConnectionString)) {
                    conn.Open();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, DboType.Oracle, logger);
                        command.CommandTimeout = cmdTimeout;
                        var parameter = new OracleParameter(":lastDateTime", OracleDbType.TimeStamp) {
                            Value = LastSyncRecord.Get(objectSource.InternalId, objectSource.ExternalId.Value).LastTransfersSyncDate
                        };
                        command.Parameters.Add(parameter);
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                list.Add(reader.ReadTransfersFromDb(objectSource));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (OracleException ex) {
                throw oracleExceptionProcessor(ex);
            }
        }

        public static TankTransferData[] GetTransferDataFromFireBirdConnection(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new FbConnection(config.ConnectionString)) {
                    conn.Open();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, DboType.FireBird, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                list.Add(reader.ReadTransfersFromDb(objectSource));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (FbException ex) {
                throw fireBirdSqlExceptionProcessor(ex);
            }
        }

        #endregion

        #region MeasurementData

        public static TankMeasurementData[] GetMeasurementDataFromDefaultConnection(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new SqlConnection(config.ConnectionString)) {
                    conn.Open();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, DboType.Default, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                list.Add(reader.ReadMeasurementFromDb(objectSource));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (SqlException ex) {
                throw defaultSqlExceptionProcessor(ex);
            }
        }

        public static TankMeasurementData[] GetMeasurementDataFromMySqlConnection(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new MySqlConnection(config.ConnectionString)) {
                    conn.Open();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, DboType.MySql, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                list.Add(reader.ReadMeasurementFromDb(objectSource));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (MySqlException ex) {
                throw mySqlExceptionProcessor(ex);
            }
        }

        public static TankMeasurementData[] GetMeasurementDataFromOracleConnection(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new OracleConnection(config.ConnectionString)) {
                    conn.Open();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, DboType.Oracle, logger);
                        command.CommandTimeout = cmdTimeout;
                        var parameter = new OracleParameter(":lastDateTime", OracleDbType.TimeStamp) {
                            Value = LastSyncRecord.Get(objectSource.InternalId, objectSource.ExternalId.Value).LastTransfersSyncDate
                        };
                        command.Parameters.Add(parameter);
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                list.Add(reader.ReadMeasurementFromDb(objectSource));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (OracleException ex) {
                throw oracleExceptionProcessor(ex);
            }
        }

        public static TankMeasurementData[] GetMeasurementDataFromFireBirdConnection(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new FbConnection(config.ConnectionString)) {
                    conn.Open();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, DboType.FireBird, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                list.Add(reader.ReadMeasurementFromDb(objectSource));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (FbException ex) {
                throw fireBirdSqlExceptionProcessor(ex);
            }
        }

        #endregion

        #region Exceptioins

        private static Exception oracleExceptionProcessor(OracleException ex) {
            switch (ex.Number) {
                case 900:
                case 903:
                case 904:
                case 936:
                case 942:
                case 1756: {
                        // Todo: send to api IncidentUtil.PutIncident(IncidentKind.SqlQueryError, objectId, ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message);
                        break;
                    }
                case 604:
                case 1089:
                case 12541:
                case 12514: {
                        // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, objectId);
                        break;
                    }
            }
            return new Exception($"{ex.Message} {ex.StackTrace}");
        }

        private static Exception mySqlExceptionProcessor(MySqlException ex) {
            switch (ex.Number) {
                case 1054:
                case 1064: {
                        // Todo: send to api IncidentUtil.PutIncident(IncidentKind.SqlQueryError, objectId, ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message);
                        break;
                    }
                case 1042: {
                        // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, objectId);
                        break;
                    }
            }

            return new Exception($"{ex.Message} {ex.StackTrace}");
        }

        private static Exception defaultSqlExceptionProcessor(SqlException ex) {
            switch (ex.Number) {
                case 102:
                case 105:
                case 156:
                case 207:
                case 208: {
                        // Todo: send to api IncidentUtil.PutIncident(IncidentKind.SqlQueryError, objectId, ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message);
                        break;
                    }
                case 2: {
                        // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, objectId);
                        break;
                    }
            }
            throw new Exception($"{ex.Message} {ex.StackTrace}");
        }

        private static Exception fireBirdSqlExceptionProcessor(FbException ex) {
            switch (ex.SQLSTATE) {
                case "42000": {
                    // Todo: send to api IncidentUtil.PutIncident(IncidentKind.SqlQueryError, objectId);
                    break;
                }
                case "08006": {
                    // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, objectId);
                    break;
                }
            }
            throw new Exception($"{ex.Message} {ex.StackTrace}");
        }

        #endregion

        #region Queries

        private static string GetTransferQuery(ObjectSource source, DboType dboType, Logger logger) {
            var queryParams = $@"
                    {source.TankTransferParams.StartTime}, 
                    {source.TankTransferParams.EndTime}, 
                    {source.TankTransferParams.MassStart},
                    {source.TankTransferParams.MassFinish},
                    {source.TankTransferParams.LevelStart},
                    {source.TankTransferParams.LevelFinish},
                    {source.TankTransferParams.VolumeStart},
                    {source.TankTransferParams.VolumeFinish}";
            var lastSyncDate = LastSyncRecord.Get(source.InternalId, source.ExternalId.Value).LastTransfersSyncDate;
            const int ps = 100;

            switch (dboType) {
                case DboType.Default: return getDefaultSqlQuery(queryParams, source.TankTransferParams.StartTime, source, ps, lastSyncDate, logger);
                case DboType.MySql: return getMySqlQuery(queryParams, source.TankTransferParams.StartTime, source, ps, lastSyncDate, logger);
                case DboType.Oracle: return getOracleQuery(queryParams, source.TankTransferParams.StartTime, source, ps, lastSyncDate, logger);
                case DboType.FireBird: return getFireBirdQuery(queryParams, source.TankTransferParams.StartTime, source, ps, lastSyncDate, logger);
                default: throw new NotImplementedException(dboType.ToString());
            }
        }

        private static string GetMeasurementQuery(ObjectSource source, DboType dboType, Logger logger) {
            var queryParams = $@"
                    {source.TankMeasurementParams.Temperature}, 
                    {source.TankMeasurementParams.Level}, 
                    {source.TankMeasurementParams.Volume}, 
                    {source.TankMeasurementParams.Mass}, 
                    {source.TankMeasurementParams.DateTimeStamp}, 
                    {source.TankMeasurementParams.Density}";
            var lastSyncDate = LastSyncRecord.Get(source.InternalId, source.ExternalId.Value).LastMeasurementsSyncDate;
            const int ps = 100;

            switch (dboType) {
                case DboType.Default: return getDefaultSqlQuery(queryParams, source.TankMeasurementParams.DateTimeStamp, source, ps, lastSyncDate, logger);
                case DboType.MySql: return getMySqlQuery(queryParams, source.TankMeasurementParams.DateTimeStamp, source, ps, lastSyncDate, logger);
                case DboType.Oracle: return getOracleQuery(queryParams, source.TankMeasurementParams.DateTimeStamp, source, ps, lastSyncDate, logger);
                case DboType.FireBird: return getFireBirdQuery(queryParams, source.TankMeasurementParams.DateTimeStamp, source, ps, lastSyncDate, logger);
                default: throw new NotImplementedException(dboType.ToString());
            }
        }

        private static string getDefaultSqlQuery(string queryParams, string timeStampParam, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.Condition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT TOP({ps}) {queryParams} FROM {source.Table} {lastSyncDateQuery}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getMySqlQuery(string queryParams, string timeStampParam, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.Condition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT {queryParams} FROM {source.Table} {lastSyncDateQuery} Limit {ps}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getOracleQuery(string queryParams, string timeStampParam, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.Condition} and {timeStampParam} > :lastDateTime order by {timeStampParam} asc) WHERE ROWNUM <= {ps}";
            var query = $"SELECT * FROM (SELECT {queryParams} FROM {source.Table} {source.Join} {lastSyncDateQuery}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getFireBirdQuery(string queryParams, string timeStampParam, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.Condition} and {timeStampParam} > '{lastSyncDate.ToString("MM-dd-yyyy HH:mm:ss.fff")}' order by {timeStampParam} asc";
            var query = $"SELECT FIRST {ps} {queryParams} FROM {source.Table} {lastSyncDateQuery}";
            logger.Debug($"Query - {query}");
            return query;
        }

        #endregion

        #region ConnectionState

        public static bool GetConnectionState(DatabaseConnectionConfig config) {
            switch (config.DboType) {
                case DboType.Default: return getDefaultSqlConnectionState(config.ConnectionString);
                case DboType.MySql: return getMySqlConnectionState(config.ConnectionString);
                case DboType.Oracle: return getOracleSqlConnectionState(config.ConnectionString);
                case DboType.FireBird: return getFireBirdSqlConnectionState(config.ConnectionString);
                default: throw new NotImplementedException(config.DboType.Value.ToString());
            }
        }

        private static bool getDefaultSqlConnectionState(string connectionString) {
            using (var conn = new SqlConnection(connectionString)) {
                conn.Open();
                var res = conn.State == ConnectionState.Open;
                conn.Close();
                return res;
            }
        }

        private static bool getMySqlConnectionState(string connectionString) {
            using (var conn = new MySqlConnection(connectionString)) {
                conn.Open();
                var res = conn.State == ConnectionState.Open;
                conn.Close();
                return res;
            }
        }

        private static bool getOracleSqlConnectionState(string connectionString) {
            using (var conn = new OracleConnection(connectionString)) {
                conn.Open();
                var res = conn.State == ConnectionState.Open;
                conn.Close();
                return res;
            }
        }

        private static bool getFireBirdSqlConnectionState(string connectionString) {
            using (var conn = new FbConnection(connectionString)) {
                conn.Open();
                var res = conn.State == ConnectionState.Open;
                conn.Close();
                return res;
            }
        }

        #endregion
    }
}

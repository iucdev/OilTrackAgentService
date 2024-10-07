using FirebirdSql.Data.FirebirdClient;
using MySql.Data.MySqlClient;
using NLog;
using Oracle.DataAccess.Client;
using Service.Clients.Utils;
using Service.Common;
using Service.Enums;
using Service.LocalDb;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection.Emit;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace Service.Clients.DBO {
    public static class DboConnectionHelpers {
        private static TankTransferData ReadTransfersFromDb(this DbDataReader reader, ObjectSource objectSource, Logger logger)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"StartDate {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankTransferParams.StartTime}"))}");
            stringBuilder.AppendLine($"EndDate {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankTransferParams.EndTime}"))}");
            stringBuilder.AppendLine($"MassStart {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankTransferParams.MassStart}"))}");
            stringBuilder.AppendLine($"MassEnd {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankTransferParams.MassFinish}"))}");
            stringBuilder.AppendLine($"LevelStart {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelStart}"))}");
            stringBuilder.AppendLine($"LevelEnd {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelFinish}"))}");
            stringBuilder.AppendLine($"VolumeStart {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeStart}"))}");
            stringBuilder.AppendLine($"VolumeEnd {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeFinish}"))}");

            //logger.Debug(stringBuilder.ToString());

            var StartDate = reader.GetDateTime(reader.GetOrdinal($"{objectSource.TankTransferParams.StartTime}"));
            var EndDate = reader.GetDateTime(reader.GetOrdinal($"{objectSource.TankTransferParams.EndTime}"));
            var MassStart = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.MassStart}")), 2));
            var MassEnd = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.MassFinish}")), 2));
            var LevelStart = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelStart}")), 2));
            var LevelEnd = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelFinish}")), 2));
            var VolumeStart = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeStart}")), 2));
            var VolumeEnd = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeFinish}")), 2));


            var data = new TankTransferData
            {
                StartDate = reader.GetDateTime(reader.GetOrdinal($"{objectSource.TankTransferParams.StartTime}")),
                EndDate = reader.GetDateTime(reader.GetOrdinal($"{objectSource.TankTransferParams.EndTime}")),
                MassStart = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.MassStart}")), 2)),
                MassEnd = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.MassFinish}")), 2)),
                LevelStart = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelStart}")), 2)),
                LevelEnd = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelFinish}")), 2)),
                VolumeStart = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeStart}")), 2)),
                VolumeEnd = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeFinish}")), 2)),
                OilProductType = CommonHelper.TryGetOilProductType(reader.GetString(reader.GetOrdinal($"{objectSource.TankTransferParams.OilProductType}")), logger),
                LevelUnitType = objectSource.LevelUnitType.Value,
                MassUnitType = objectSource.MassUnitType.Value,
                VolumeUnitType = objectSource.VolumeUnitType.Value
            };
            data.OperationType = data.VolumeEnd > data.VolumeStart
                ? TransferOperationType.Income
                : TransferOperationType.Outcome;
            return data;
        }

        private static TankMeasurementData ReadMeasurementFromDb(this DbDataReader reader, ObjectSource objectSource, Logger logger)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"tempType {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Temperature}"))}");
            stringBuilder.AppendLine($"levelType {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Level}"))}");
            stringBuilder.AppendLine($"volumeType {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Volume}"))}");
            stringBuilder.AppendLine($"massType {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Mass}"))}");
            stringBuilder.AppendLine($"measurementDateType {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankMeasurementParams.DateTimeStamp}"))}");
            stringBuilder.AppendLine($"densityType {reader.GetFieldType(reader.GetOrdinal($"{objectSource.TankMeasurementParams.Density}"))}");

            //logger.Debug(stringBuilder.ToString());

            var temperature = tryGetDecimalFromTbField(reader, objectSource.TankMeasurementParams.Temperature);
            var level = tryGetDecimalFromTbField(reader, objectSource.TankMeasurementParams.Level);
            var volume = tryGetDecimalFromTbField(reader, objectSource.TankMeasurementParams.Volume);
            var mass = tryGetDecimalFromTbField(reader, objectSource.TankMeasurementParams.Mass);
            var measurementDate = reader.GetDateTime(reader.GetOrdinal($"{objectSource.TankMeasurementParams.DateTimeStamp}"));
            var density = tryGetDecimalFromTbField(reader, objectSource.TankMeasurementParams.Density);
            var data = new TankMeasurementData {
                Temperature = temperature,
                Level = level,
                Volume = volume,
                Mass = mass,
                MeasurementDate = measurementDate,
                Density = density,
                LevelUnitType = objectSource.LevelUnitType.Value,
                MassUnitType = objectSource.MassUnitType.Value,
                OilProductType = CommonHelper.TryGetOilProductType(reader.GetString(reader.GetOrdinal($"{objectSource.TankMeasurementParams.OilProductType}")), logger),
                VolumeUnitType = objectSource.VolumeUnitType.Value
            };
            return data;
        }

        private static decimal tryGetDecimalFromTbField(DbDataReader reader, string fieldName) {
            int ordinal = reader.GetOrdinal(fieldName);

            if (!reader.IsDBNull(ordinal)) {
                var value = reader.GetValue(ordinal);

                if (value is double doubleValue) {
                    return Convert.ToDecimal(Math.Round(doubleValue, 2));
                } else if (value is float floatValue) {
                    return Convert.ToDecimal(Math.Round(floatValue, 2));
                } else if (value is decimal decimalValue) {
                    return Math.Round(decimalValue, 2);
                }
            }

            return 0;
        }

        private static FlowmeterMeasurementData ReadFlowmeterMeasurementFromDb(this DbDataReader reader, ObjectSource objectSource, Logger logger)
        {
            var data = new FlowmeterMeasurementData
            {
                TotalMass = reader.GetDecimal(reader.GetOrdinal($"{objectSource.FlowmeterIndicatorParams.TotalMass}")),
                FlowMass = reader.GetDecimal(reader.GetOrdinal($"{objectSource.FlowmeterIndicatorParams.FlowMass}")),
                TotalVolume = reader.GetDecimal(reader.GetOrdinal($"{objectSource.FlowmeterIndicatorParams.TotalVolume}")),
                CurrentDensity = reader.GetDecimal(reader.GetOrdinal($"{objectSource.FlowmeterIndicatorParams.CurrentDensity}")),
                CurrentTemperature = reader.GetDecimal(reader.GetOrdinal($"{objectSource.FlowmeterIndicatorParams.CurrentTemperature}")),
                MeasurementDate = reader.GetDateTime(reader.GetOrdinal($"{objectSource.FlowmeterIndicatorParams.DateTimeStamp}")),
                MassUnitType = objectSource.MassUnitType.Value,
                OilProductType = CommonHelper.TryGetOilProductType(reader.GetString(reader.GetOrdinal($"{objectSource.FlowmeterIndicatorParams.OilProductType}")), logger),
                OperationType = Enum.TryParse<FlowmeterOperationType>(reader.GetString(reader.GetOrdinal($"{objectSource.FlowmeterIndicatorParams.OilProductType}")), out var parsedOpType) 
                    ? parsedOpType
                    : FlowmeterOperationType.Undefined,
                SourceTankId = CommonHelper.TryGetSourceTankId(reader.GetString(reader.GetOrdinal($"{objectSource.FlowmeterIndicatorParams.SourceTankId}")), objectSource),
                VolumeUnitType = objectSource.VolumeUnitType.Value
            };
            return data;
        }

        #region TransferData

        public static async Task<TankTransferData[]> GetTransferDataFromDefaultConnectionAsync(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger)
        {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new SqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, config.TransferTable, DboType.Default, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadTransfersFromDb(objectSource, logger));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (System.Data.SqlClient.SqlException ex) {
                throw defaultSqlExceptionProcessor(ex);
            }
        }

        public static async Task<TankTransferData[]> GetTransferDataFromMySqlConnectionAsync(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger)
        {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new MySqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, config.TransferTable, DboType.MySql, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadTransfersFromDb(objectSource, logger));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (MySql.Data.MySqlClient.MySqlException ex) {
                throw mySqlExceptionProcessor(ex);
            }
        }

        public static async Task<TankTransferData[]> GetTransferDataFromOracleConnectionAsync(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger)
        {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new OracleConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, config.TransferTable, DboType.Oracle, logger);
                        command.CommandTimeout = cmdTimeout;
                        var parameter = new OracleParameter(":lastDateTime", OracleDbType.TimeStamp)
                        {
                            Value = LastSyncRecord.GetByExternalId(objectSource.ExternalId.Value, logger).LastTransfersSyncDate
                        };
                        command.Parameters.Add(parameter);
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadTransfersFromDb(objectSource, logger));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (OracleException ex) {
                throw oracleExceptionProcessor(ex);
            }
        }

        public static async Task<TankTransferData[]> GetTransferDataFromFireBirdConnectionAsync(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger)
        {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new FbConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, config.TransferTable, DboType.FireBird, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadTransfersFromDb(objectSource, logger));
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

        public static async Task<TankMeasurementData[]> GetMeasurementDataFromDefaultConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger)
        {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new SqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, config.MeasurementTable, DboType.Default, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadMeasurementFromDb(objectSource, logger));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (SqlException ex) {
                throw defaultSqlExceptionProcessor(ex);
            }
        }

        public static async Task<TankMeasurementData[]> GetMeasurementDataFromMySqlConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger)
        {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new MySqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, config.MeasurementTable, DboType.MySql, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadMeasurementFromDb(objectSource, logger));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (MySqlException ex) {
                throw mySqlExceptionProcessor(ex);
            }
        }

        public static async Task<TankMeasurementData[]> GetMeasurementDataFromOracleConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger)
        {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new OracleConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, config.MeasurementTable, DboType.Oracle, logger);
                        command.CommandTimeout = cmdTimeout;
                        var parameter = new OracleParameter(":lastDateTime", OracleDbType.TimeStamp)
                        {
                            Value = LastSyncRecord.GetByExternalId(objectSource.ExternalId.Value, logger).LastTransfersSyncDate
                        };
                        command.Parameters.Add(parameter);
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadMeasurementFromDb(objectSource, logger));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (OracleException ex) {
                throw oracleExceptionProcessor(ex);
            }
        }

        public static async Task<TankMeasurementData[]> GetMeasurementDataFromFireBirdConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger)
        {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new FbConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, config.MeasurementTable, DboType.FireBird, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadMeasurementFromDb(objectSource, logger));
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

        #region FlowmeterMeasurementData

        public static async Task<FlowmeterMeasurementData[]> GetFlowmeterMeasurementDataFromDefaultConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger)
        {
            try {
                var list = new List<FlowmeterMeasurementData>();
                using (var conn = new SqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetFlowmeterQuery(objectSource, config.FlowmeterTable, DboType.Default, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadFlowmeterMeasurementFromDb(objectSource, logger));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (SqlException ex) {
                throw defaultSqlExceptionProcessor(ex);
            }
        }

        public static async Task<FlowmeterMeasurementData[]> GetFlowmeterMeasurementDataFromMySqlConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger)
        {
            try {
                var list = new List<FlowmeterMeasurementData>();
                using (var conn = new MySqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetFlowmeterQuery(objectSource, config.FlowmeterTable, DboType.MySql, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadFlowmeterMeasurementFromDb(objectSource, logger));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (MySqlException ex) {
                throw mySqlExceptionProcessor(ex);
            }
        }

        public static async Task<FlowmeterMeasurementData[]> GetFlowmeterMeasurementDataFromOracleConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger)
        {
            try {
                var list = new List<FlowmeterMeasurementData>();
                using (var conn = new OracleConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetFlowmeterQuery(objectSource, config.FlowmeterTable, DboType.Oracle, logger);
                        command.CommandTimeout = cmdTimeout;
                        var parameter = new OracleParameter(":lastDateTime", OracleDbType.TimeStamp)
                        {
                            Value = LastSyncRecord.GetByExternalId(objectSource.ExternalId.Value, logger).LastTransfersSyncDate
                        };
                        command.Parameters.Add(parameter);
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadFlowmeterMeasurementFromDb(objectSource, logger));
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (OracleException ex) {
                throw oracleExceptionProcessor(ex);
            }
        }

        public static async Task<FlowmeterMeasurementData[]> GetFlowmeterMeasurementDataFromFireBirdConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger)
        {
            try {
                var list = new List<FlowmeterMeasurementData>();
                using (var conn = new FbConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetFlowmeterQuery(objectSource, config.FlowmeterTable, DboType.FireBird, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                list.Add(reader.ReadFlowmeterMeasurementFromDb(objectSource, logger));
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

        private static Exception oracleExceptionProcessor(OracleException ex)
        {
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

        private static Exception mySqlExceptionProcessor(MySqlException ex)
        {
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

        private static Exception defaultSqlExceptionProcessor(SqlException ex)
        {
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

        private static Exception fireBirdSqlExceptionProcessor(FbException ex)
        {
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

        private static string GetTransferQuery(ObjectSource source, string tableName, DboType dboType, Logger logger)
        {
            var queryParams = $@"
                    {source.TankTransferParams.StartTime}, 
                    {source.TankTransferParams.EndTime}, 
                    {source.TankTransferParams.MassStart},
                    {source.TankTransferParams.MassFinish},
                    {source.TankTransferParams.LevelStart},
                    {source.TankTransferParams.LevelFinish},
                    {source.TankTransferParams.VolumeStart},
                    {source.TankTransferParams.OilProductType},
                    {source.TankTransferParams.VolumeFinish}";

            var isNotNullParams = $@"
                    {source.TankTransferParams.StartTime} is not null and 
                    {source.TankTransferParams.EndTime} is not null and
                    {source.TankTransferParams.MassStart} is not null and
                    {source.TankTransferParams.MassFinish} is not null and
                    {source.TankTransferParams.LevelStart} is not null and
                    {source.TankTransferParams.LevelFinish} is not null and
                    {source.TankTransferParams.VolumeStart} is not null and
                    {source.TankTransferParams.VolumeFinish} is not null";
            var lastSyncDate = LastSyncRecord.GetByExternalId(source.ExternalId.Value, logger).LastTransfersSyncDate;
            const int ps = 100;

            switch (dboType) {
                case DboType.Default: return getDefaultTransferSqlQuery(queryParams, source.TankTransferParams.StartTime, tableName, source, ps, lastSyncDate, logger, isNotNullParams);
                case DboType.MySql: return getMySqlTransferQuery(queryParams, source.TankTransferParams.StartTime, tableName, source, ps, lastSyncDate, logger);
                case DboType.Oracle: return getOracleTransferQuery(queryParams, source.TankTransferParams.StartTime, tableName, source, ps, logger);
                case DboType.FireBird: return getFireBirdTransferQuery(queryParams, source.TankTransferParams.StartTime, tableName, source, ps, lastSyncDate, logger);
                default: throw new NotImplementedException(dboType.ToString());
            }
        }

        private static string GetMeasurementQuery(ObjectSource source, string tableName, DboType dboType, Logger logger)
        {
            var queryParams = $@"
                    {source.TankMeasurementParams.Temperature}, 
                    {source.TankMeasurementParams.Level}, 
                    {source.TankMeasurementParams.Volume}, 
                    {source.TankMeasurementParams.Mass}, 
                    {source.TankMeasurementParams.DateTimeStamp}, 
                    {source.TankMeasurementParams.OilProductType}, 
                    {source.TankMeasurementParams.Density}";
            var lastSyncDate = LastSyncRecord.GetByExternalId(source.ExternalId.Value, logger).LastMeasurementsSyncDate;
            const int ps = 100;

            switch (dboType) {
                case DboType.Default: return getDefaultMeasurementSqlQuery(queryParams, source.TankMeasurementParams.DateTimeStamp, tableName, source, ps, lastSyncDate, logger);
                case DboType.MySql: return getMySqlMeasurementQuery(queryParams, source.TankMeasurementParams.DateTimeStamp, tableName, source, ps, lastSyncDate, logger);
                case DboType.Oracle: return getOracleMeasurementQuery(queryParams, source.TankMeasurementParams.DateTimeStamp, tableName, source, ps, logger);
                case DboType.FireBird: return getFireBirdMeasurementQuery(queryParams, source.TankMeasurementParams.DateTimeStamp, tableName, source, ps, lastSyncDate, logger);
                default: throw new NotImplementedException(dboType.ToString());
            }
        }

        private static string GetFlowmeterQuery(ObjectSource source, string tableName, DboType dboType, Logger logger)
        {
            var queryParams = $@"
                    {source.FlowmeterIndicatorParams.TotalMass}, 
                    {source.FlowmeterIndicatorParams.FlowMass}, 
                    {source.FlowmeterIndicatorParams.TotalVolume}, 
                    {source.FlowmeterIndicatorParams.CurrentDensity}, 
                    {source.FlowmeterIndicatorParams.CurrentTemperature},
                    {source.FlowmeterIndicatorParams.OilProductType},
                    {source.FlowmeterIndicatorParams.OperationType},
                    {source.FlowmeterIndicatorParams.DateTimeStamp},
                    {source.FlowmeterIndicatorParams.SourceTankId}";
            var lastSyncDate = LastSyncRecord.GetByExternalId(source.ExternalId.Value, logger).LastFlowmeterSyncDate;
            const int ps = 100;

            switch (dboType) {
                case DboType.Default: return getDefaultFlowmeterSqlQuery(queryParams, source.FlowmeterIndicatorParams.DateTimeStamp, tableName, source, ps, lastSyncDate, logger);
                case DboType.MySql: return getMySqlFlowmeterQuery(queryParams, source.FlowmeterIndicatorParams.DateTimeStamp, tableName, source, ps, lastSyncDate, logger);
                case DboType.Oracle: return getOracleFlowmeterQuery(queryParams, source.FlowmeterIndicatorParams.DateTimeStamp, tableName, source, ps, logger);
                case DboType.FireBird: return getFireBirdFlowmeterQuery(queryParams, source.FlowmeterIndicatorParams.DateTimeStamp, tableName, source, ps, lastSyncDate, logger);
                default: throw new NotImplementedException(dboType.ToString());
            }
        }

        private static string getDefaultMeasurementSqlQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger)
        {
            var lastSyncDateQuery = $"{source.MeasurementCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT TOP({ps}) {queryParams} FROM {tableName} {lastSyncDateQuery}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getDefaultFlowmeterSqlQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger)
        {
            var lastSyncDateQuery = $"{source.FlowmeterCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT TOP({ps}) {queryParams} FROM {tableName} {lastSyncDateQuery}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getDefaultTransferSqlQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger, string isNotNullParam)
        {
            var lastSyncDateQuery = $"{source.TransferCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT TOP({ps}) {queryParams} FROM {tableName} {lastSyncDateQuery} and {isNotNullParam}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getMySqlMeasurementQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger)
        {
            var lastSyncDateQuery = $"{source.MeasurementCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT {queryParams} FROM {tableName} {lastSyncDateQuery} Limit {ps}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getMySqlFlowmeterQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger)
        {
            var lastSyncDateQuery = $"{source.FlowmeterCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT {queryParams} FROM {tableName} {lastSyncDateQuery} Limit {ps}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getMySqlTransferQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger)
        {
            var lastSyncDateQuery = $"{source.TransferCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT {queryParams} FROM {tableName} {lastSyncDateQuery} Limit {ps}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getOracleMeasurementQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, Logger logger)
        {
            var lastSyncDateQuery = $"{source.MeasurementCondition} and {timeStampParam} > :lastDateTime order by {timeStampParam} asc) WHERE ROWNUM <= {ps}";
            var query = $"SELECT * FROM (SELECT {queryParams} FROM {tableName} {lastSyncDateQuery}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getOracleFlowmeterQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, Logger logger)
        {
            var lastSyncDateQuery = $"{source.FlowmeterCondition} and {timeStampParam} > :lastDateTime order by {timeStampParam} asc) WHERE ROWNUM <= {ps}";
            var query = $"SELECT * FROM (SELECT {queryParams} FROM {tableName} {lastSyncDateQuery}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getOracleTransferQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, Logger logger)
        {
            var lastSyncDateQuery = $"{source.TransferCondition} and {timeStampParam} > :lastDateTime order by {timeStampParam} asc) WHERE ROWNUM <= {ps}";
            var query = $"SELECT * FROM (SELECT {queryParams} FROM {tableName} {lastSyncDateQuery}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getFireBirdMeasurementQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger)
        {
            var lastSyncDateQuery = $"{source.MeasurementCondition} and {timeStampParam} > '{lastSyncDate.ToString("MM-dd-yyyy HH:mm:ss.fff")}' order by {timeStampParam} asc";
            var query = $"SELECT FIRST {ps} {queryParams} FROM {tableName} {lastSyncDateQuery}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getFireBirdFlowmeterQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger)
        {
            var lastSyncDateQuery = $"{source.FlowmeterCondition} and {timeStampParam} > '{lastSyncDate.ToString("MM-dd-yyyy HH:mm:ss.fff")}' order by {timeStampParam} asc";
            var query = $"SELECT FIRST {ps} {queryParams} FROM {tableName} {lastSyncDateQuery}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getFireBirdTransferQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger)
        {
            var lastSyncDateQuery = $"{source.TransferCondition} and {timeStampParam} > '{lastSyncDate.ToString("MM-dd-yyyy HH:mm:ss.fff")}' order by {timeStampParam} asc";
            var query = $"SELECT FIRST {ps} {queryParams} FROM {tableName} {lastSyncDateQuery}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        #endregion

        #region ConnectionState

        public static bool GetConnectionState(DatabaseConnectionConfig config)
        {
            switch (config.DboType) {
                case DboType.Default: return getDefaultSqlConnectionState(config.ConnectionString);
                case DboType.MySql: return getMySqlConnectionState(config.ConnectionString);
                case DboType.Oracle: return getOracleSqlConnectionState(config.ConnectionString);
                case DboType.FireBird: return getFireBirdSqlConnectionState(config.ConnectionString);
                default: throw new NotImplementedException(config.DboType.Value.ToString());
            }
        }

        private static bool getDefaultSqlConnectionState(string connectionString)
        {
            using (var conn = new SqlConnection(connectionString)) {
                conn.Open();
                var res = conn.State == ConnectionState.Open;
                conn.Close();
                return res;
            }
        }

        private static bool getMySqlConnectionState(string connectionString)
        {
            using (var conn = new MySqlConnection(connectionString)) {
                conn.Open();
                var res = conn.State == ConnectionState.Open;
                conn.Close();
                return res;
            }
        }

        private static bool getOracleSqlConnectionState(string connectionString)
        {
            using (var conn = new OracleConnection(connectionString)) {
                conn.Open();
                var res = conn.State == ConnectionState.Open;
                conn.Close();
                return res;
            }
        }

        private static bool getFireBirdSqlConnectionState(string connectionString)
        {
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

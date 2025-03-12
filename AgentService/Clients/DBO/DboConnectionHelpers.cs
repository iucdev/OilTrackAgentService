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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Service.Clients.DBO {
    public static class DboConnectionHelpers {
        private static TankTransferData ReadTransfersFromDb(this DbDataReader reader, ObjectSource objectSource, Logger logger) {
            
            var StartDateString = reader.GetString(reader.GetOrdinal($"{objectSource.TankTransferParams.StartTime.Replace("D.", "")}"));
            var EndDateString = reader.GetString(reader.GetOrdinal($"{objectSource.TankTransferParams.EndTime.Replace("D.", "")}"));
            var MassStart = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.MassStart.Replace("D.", "")}")), 2));
            var MassEnd = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.MassFinish.Replace("D.", "")}")), 2));
            var LevelStart = string.IsNullOrEmpty(objectSource.TankTransferParams.LevelStart) ? 0 : Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelStart.Replace("D.", "")}")), 2));
            var LevelEnd = string.IsNullOrEmpty(objectSource.TankTransferParams.LevelFinish) ? 0 : Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.LevelFinish.Replace("D.", "")}")), 2));
            var VolumeStart = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeStart.Replace("D.", "")}")), 2));
            var VolumeEnd = Convert.ToDecimal(Math.Round(reader.GetDouble(reader.GetOrdinal($"{objectSource.TankTransferParams.VolumeFinish.Replace("D.", "")}")), 2));
            var OilProduct = CommonHelper.TryGetOilProductType(reader.GetString(reader.GetOrdinal($"{objectSource.TankTransferParams.OilProductType.Replace("T.", "").Replace("\"", "")}")), logger);
            var StartDate = tryGetDateTime(StartDateString);
            var EndDate = tryGetDateTime(EndDateString);
            var data = new TankTransferData {
                StartDate = StartDate,
                EndDate = StartDate,
                MassStart = MassStart,
                MassEnd = MassEnd,
                LevelStart = LevelStart,
                LevelEnd = LevelEnd,
                VolumeStart = VolumeStart,
                VolumeEnd = VolumeEnd,
                OilProductType = OilProduct,
                LevelUnitType = objectSource.LevelUnitType.Value,
                MassUnitType = objectSource.MassUnitType.Value,
                VolumeUnitType = objectSource.VolumeUnitType.Value
            };
            data.OperationType = data.MassEnd > data.MassStart
                ? TransferOperationType.Income
                : TransferOperationType.Outcome;

            if (data.OperationType == TransferOperationType.Income) {
                var isValid = (data.MassEnd > data.MassStart) && (data.VolumeEnd > data.VolumeStart);
                if (isValid) {
                    data.LevelEnd = 1;
                } else {
                    return null;
                }
            } else {
                var isValid = (data.MassStart > data.MassEnd) && (data.VolumeStart > data.VolumeEnd);
                if (isValid) {
                    data.LevelStart = 1;
                } else {
                    return null;
                }
            }
            return data;
        }

        private static TankMeasurementData ReadMeasurementFromDb(this DbDataReader reader, ObjectSource objectSource, Logger logger) {
            
            try {
                var temperatureText = reader.GetString(reader.GetOrdinal(objectSource.TankMeasurementParams.Temperature.Replace("D.", "")));
                var levelText = reader.GetString(reader.GetOrdinal(objectSource.TankMeasurementParams.Level.Replace("D.", "")));
                var volumeText = reader.GetString(reader.GetOrdinal(objectSource.TankMeasurementParams.Volume.Replace("D.", "")));
                var massText = reader.GetString(reader.GetOrdinal(objectSource.TankMeasurementParams.Mass.Replace("D.", "")));
                var measurementDateText = reader.GetString(reader.GetOrdinal($"{objectSource.TankMeasurementParams.DateTimeStamp.Replace("D.", "")}"));
                var densityText = reader.GetString(reader.GetOrdinal(objectSource.TankMeasurementParams.Density.Replace("D.", "")));

                var temperatureTyped = Convert.ToDecimal(temperatureText);
                var levelTyped = Convert.ToDecimal(levelText);
                var volumeTyped = Convert.ToDecimal(volumeText);
                var massTyped = Convert.ToDecimal(massText);
                var measurementDateTyped = tryGetDateTime(measurementDateText);
                var densityTyped = Convert.ToDecimal(densityText);

                var oilProductType = CommonHelper.TryGetOilProductType(reader.GetString(reader.GetOrdinal($"{objectSource.TankMeasurementParams.OilProductType.Replace("T.", "").Replace("\"", "")}")), logger);

                var data = new TankMeasurementData {
                    Temperature = temperatureTyped,
                    Level = levelTyped,
                    Volume = volumeTyped,
                    Mass = massTyped,
                    MeasurementDate = measurementDateTyped,
                    Density = densityTyped,
                    LevelUnitType = objectSource.LevelUnitType.Value,
                    MassUnitType = objectSource.MassUnitType.Value,
                    OilProductType = oilProductType,
                    VolumeUnitType = objectSource.VolumeUnitType.Value
                };
                return data;
            }catch(Exception ex) {
                logger.Debug(ex);
                throw ex;
            }
        }

        private static DateTime tryGetDateTime(string dateString) {
            try {
                return Convert.ToDateTime(dateString);
            } catch (Exception ex) {
                if (!DateTime.TryParseExact(dateString, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startTime)) {
                    DateTime.TryParseExact(dateString, "dd.MM.yyyy H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startTimeExtra);
                    return startTimeExtra;
                }
                return startTime;
            }
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
                } else if (value is string stringValue) {
                    return Convert.ToDecimal(stringValue);
                }
            }

            return 0;
        }

        private static FlowmeterMeasurementData ReadFlowmeterMeasurementFromDb(this DbDataReader reader, ObjectSource objectSource, Logger logger) {
            var data = new FlowmeterMeasurementData {
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

        public static async Task<TankTransferData[]> GetTransferDataFromDefaultConnectionAsync(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new SqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, config.TransferTable, DboType.Default, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadTransfersFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (System.Data.SqlClient.SqlException ex) {
                throw defaultSqlExceptionProcessor(ex);
            }
        }

        public static async Task<TankTransferData[]> GetTransferDataFromMySqlConnectionAsync(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new MySqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, config.TransferTable, DboType.MySql, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    var model = reader.ReadTransfersFromDb(objectSource, logger);
                                    if (model != null) {
                                        list.Add(model);
                                    }
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                    await conn.CloseAsync();
                }
                return list.ToArray();
            } catch (MySql.Data.MySqlClient.MySqlException ex) {
                throw mySqlExceptionProcessor(ex, logger);
            }
        }

        public static async Task<TankTransferData[]> GetTransferDataFromOracleConnectionAsync(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new OracleConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, config.TransferTable, DboType.Oracle, logger);
                        command.CommandTimeout = cmdTimeout;
                        var parameter = new OracleParameter(":lastDateTime", OracleDbType.TimeStamp) {
                            Value = LastSyncRecord.GetByExternalId(objectSource.ExternalId.Value, logger).LastTransfersSyncDate
                        };
                        command.Parameters.Add(parameter);
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadTransfersFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (OracleException ex) {
                throw oracleExceptionProcessor(ex);
            }
        }

        public static async Task<TankTransferData[]> GetTransferDataFromFireBirdConnectionAsync(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var list = new List<TankTransferData>();
                using (var conn = new FbConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetTransferQuery(objectSource, config.TransferTable, DboType.FireBird, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadTransfersFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (FbException ex) {
                throw fireBirdSqlExceptionProcessor(ex);
            }
        }

        public static TankTransferData[] GetTransferDataFromDbfConnection(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var transferDataList = new List<TankTransferData>();
                var lastSyncDate = LastSyncRecord.GetByExternalId(objectSource.ExternalId.Value, logger).LastTransfersSyncDate;
                
                
                Encoding encoding = Encoding.GetEncoding("windows-1251");

                for (int year = lastSyncDate.Year; year <= DateTime.UtcNow.Year; year++) {
                    for (int month = new DateTime(year, 1, 1).Month; month <= 12; month++) {
                        var filePath = $"{config.ConnectionString}\\{(string.Format(config.TransferTable, year, objectSource.InternalId, (month < 10 ? $"0{month}" : $"{month}")))}";
                        if (File.Exists(filePath)) {
                            using (var reader = new SimpleDBFReader(filePath, encoding)) {
                                var records = reader.ReadRecords();

                                foreach (var row in records) {
                                    try {
                                        char[] separators = new char[] { '/' };
                                        var startDateTimeParam = objectSource.TankTransferParams.StartTime.Split(separators);
                                        var endDateTimeParam = objectSource.TankTransferParams.EndTime.Split(separators);
                                        var transferData = new TankTransferData {
                                            StartDate = CommonHelper.GetDateTime(row[startDateTimeParam.First()], row[startDateTimeParam.Last()]),//$"{row["N2DATA"]}", $"{row["N2START_T"]}"),
                                            EndDate = CommonHelper.GetDateTime(row[endDateTimeParam.First()], row[endDateTimeParam.Last()]),//$"{row["N2DATA"]}", $"{row["N2STOP_T"]}"),
                                            LevelStart = CommonHelper.ParseDecimal(row[objectSource.TankTransferParams.LevelStart]),//row["N2LEVEL_S"]),
                                            LevelEnd = CommonHelper.ParseDecimal(row[objectSource.TankTransferParams.LevelFinish]),//row["N2LEVEL_F"]),
                                            MassStart = CommonHelper.ParseDecimal(row[objectSource.TankTransferParams.MassStart]),//row["N2MASSA_S"]),
                                            MassEnd = CommonHelper.ParseDecimal(row[objectSource.TankTransferParams.MassFinish]),//row["N2MASSA_F"]),
                                            VolumeStart = CommonHelper.ParseDecimal(row[objectSource.TankTransferParams.VolumeStart]),//row["N2VOLUME_S"]),
                                            VolumeEnd = CommonHelper.ParseDecimal(row[objectSource.TankTransferParams.VolumeFinish]),//row["N2VOLUME_F"]),
                                            OperationType = CommonHelper.ParseDecimal(row[objectSource.TankTransferParams.VolumeFinish]) > CommonHelper.ParseDecimal(row[objectSource.TankTransferParams.VolumeStart]) //row["N2TYPE_NP"]
                                                ? TransferOperationType.Income
                                                : TransferOperationType.Outcome,
                                            OilProductType = CommonHelper.TryGetOilProductType(row[objectSource.TankTransferParams.OilProductType], logger),//row["N2OILNAME"].ToString() == "АИ-92"
                                            LevelUnitType = objectSource.LevelUnitType.Value,
                                            MassUnitType = objectSource.MassUnitType.Value,
                                            VolumeUnitType = objectSource.VolumeUnitType.Value
                                        };

                                        transferDataList.Add(transferData);
                                    }catch(Exception ex) {
                                        logger.Error(ex);
                                        continue;
                                    }
                                }

                                // Вывод результата
                                //foreach (var data in transferDataList) {
                                //    logger.Info($"Start: {data.StartDate}, End: {data.EndDate}, Oil: {data.OilProductType}, Mass: {data.MassStart} -> {data.MassEnd}, Volume: {data.VolumeStart} -> {data.VolumeEnd}");
                                //}
                                logger.Info($"{filePath} succesfully finished. Rows count: {transferDataList.Count()}");
                            }
                        } else {
                            logger.Info($"{filePath} does not exist");
                        }
                    }
                }

                return transferDataList.Where(t => t.EndDate > lastSyncDate).ToArray();
            } catch (Exception ex) {
                logger.Error(ex);
                throw ex;
            }
        }

        #endregion

        #region MeasurementData

        public static async Task<TankMeasurementData[]> GetMeasurementDataFromDefaultConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new SqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, config.MeasurementTable, DboType.Default, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadMeasurementFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (SqlException ex) {
                throw defaultSqlExceptionProcessor(ex);
            }
        }

        public static async Task<TankMeasurementData[]> GetMeasurementDataFromMySqlConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new MySqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, config.MeasurementTable, DboType.MySql, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadMeasurementFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                    await conn.CloseAsync();
                }
                return list.ToArray();
            } catch (MySqlException ex) {
                throw mySqlExceptionProcessor(ex, logger);
            }
        }

        public static async Task<TankMeasurementData[]> GetMeasurementDataFromOracleConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new OracleConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, config.MeasurementTable, DboType.Oracle, logger);
                        command.CommandTimeout = cmdTimeout;
                        var parameter = new OracleParameter(":lastDateTime", OracleDbType.TimeStamp) {
                            Value = LastSyncRecord.GetByExternalId(objectSource.ExternalId.Value, logger).LastTransfersSyncDate
                        };
                        command.Parameters.Add(parameter);
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadMeasurementFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (OracleException ex) {
                throw oracleExceptionProcessor(ex);
            }
        }

        public static async Task<TankMeasurementData[]> GetMeasurementDataFromFireBirdConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<TankMeasurementData>();
                using (var conn = new FbConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetMeasurementQuery(objectSource, config.MeasurementTable, DboType.FireBird, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadMeasurementFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (FbException ex) {
                throw fireBirdSqlExceptionProcessor(ex);
            }
        }

        public static TankMeasurementData[] GetMeasurementDataFromDbfConnection(int cmdTimeout, ObjectSource objectSource, DatabaseConnectionConfig config, Logger logger) {
            try {
                var tankMeasurementsDataList = new List<TankMeasurementData>();
                var lastSyncDate = LastSyncRecord.GetByExternalId(objectSource.ExternalId.Value, logger).LastMeasurementsSyncDate;


                Encoding encoding = Encoding.GetEncoding("windows-1251");

                for (int year = lastSyncDate.Year; year <= DateTime.UtcNow.Year; year++) {
                    for (int month = new DateTime(year, 1, 1).Month; month <= 12; month++) {
                        var filePath = $"{config.ConnectionString}\\{(string.Format(config.MeasurementTable, year, objectSource.InternalId, (month < 10 ? $"0{month}" : $"{month}")))}";

                        if (File.Exists(filePath)) {

                            string tempPath = Path.GetTempFileName();
                            //File.Copy(filePath, filePath, true);
                            // Читаем tempPath

                            using (var reader = new SimpleDBFReader(filePath, encoding)) {
                                var records = reader.ReadRecords();

                                foreach (var row in records) {
                                    try {
                                        char[] separators = new char[] { '/' };
                                        var dateTimeParam = objectSource.TankMeasurementParams.DateTimeStamp.Split(separators);
                                        var MeasurementDate = CommonHelper.GetDateTime(row[dateTimeParam.First()], row[dateTimeParam.Last()]);
                                        var Temperature = CommonHelper.ParseDecimal(row[objectSource.TankMeasurementParams.Temperature]);
                                        var Density = CommonHelper.ParseDecimal(row[objectSource.TankMeasurementParams.Density]);
                                        var Level = CommonHelper.ParseDecimal(row[objectSource.TankMeasurementParams.Level]);
                                        var Mass = CommonHelper.ParseDecimal(row[objectSource.TankMeasurementParams.Mass]);
                                        var Volume = CommonHelper.ParseDecimal(row[objectSource.TankMeasurementParams.Volume]);
                                        var OilProductType = CommonHelper.TryGetOilProductType(row[objectSource.TankMeasurementParams.OilProductType], logger);
                                        var LevelUnitType = objectSource.LevelUnitType.Value;
                                        var MassUnitType = objectSource.MassUnitType.Value;
                                        var VolumeUnitType = objectSource.VolumeUnitType.Value;
                                        var measurementData = new TankMeasurementData {
                                            MeasurementDate = MeasurementDate,
                                            Temperature = Temperature,
                                            Density = Density,
                                            Level = Level,
                                            Mass = Mass,
                                            Volume = Volume,
                                            OilProductType = OilProductType,
                                            LevelUnitType = LevelUnitType,
                                            MassUnitType = MassUnitType,
                                            VolumeUnitType = VolumeUnitType
                                        };

                                        tankMeasurementsDataList.Add(measurementData);
                                    }catch(Exception ex) {
                                        logger.Error(ex);
                                        continue;
                                    }
                                }

                                // Вывод результата
                                //foreach (var data in tankMeasurementsDataList) {
                                //    logger.Info($"MeasurementDate: {data.MeasurementDate}, Oil: {data.OilProductType}, Mass: {data.Mass}, Volume: {data.Volume}");
                                //}
                                logger.Info($"{filePath} succesfully finished. Row count: {tankMeasurementsDataList.Count()}");
                                File.Delete(tempPath);
                            }
                        } else {
                            logger.Info($"{filePath} does not exist");
                        }
                    }
                }

                return tankMeasurementsDataList.Where(t => t.MeasurementDate > lastSyncDate).ToArray();
            } catch (Exception ex) {
                logger.Error(ex);
                throw ex;
            }
        }

        #endregion

        #region FlowmeterMeasurementData

        public static async Task<FlowmeterMeasurementData[]> GetFlowmeterMeasurementDataFromDefaultConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<FlowmeterMeasurementData>();
                using (var conn = new SqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetFlowmeterQuery(objectSource, config.FlowmeterTable, DboType.Default, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadFlowmeterMeasurementFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (SqlException ex) {
                throw defaultSqlExceptionProcessor(ex);
            }
        }

        public static async Task<FlowmeterMeasurementData[]> GetFlowmeterMeasurementDataFromMySqlConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<FlowmeterMeasurementData>();
                using (var conn = new MySqlConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetFlowmeterQuery(objectSource, config.FlowmeterTable, DboType.MySql, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadFlowmeterMeasurementFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (MySqlException ex) {
                throw mySqlExceptionProcessor(ex, logger);
            }
        }

        public static async Task<FlowmeterMeasurementData[]> GetFlowmeterMeasurementDataFromOracleConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<FlowmeterMeasurementData>();
                using (var conn = new OracleConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetFlowmeterQuery(objectSource, config.FlowmeterTable, DboType.Oracle, logger);
                        command.CommandTimeout = cmdTimeout;
                        var parameter = new OracleParameter(":lastDateTime", OracleDbType.TimeStamp) {
                            Value = LastSyncRecord.GetByExternalId(objectSource.ExternalId.Value, logger).LastFlowmeterSyncDate
                        };
                        command.Parameters.Add(parameter);
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadFlowmeterMeasurementFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
                            }
                        }
                    }
                }
                return list.ToArray();
            } catch (OracleException ex) {
                throw oracleExceptionProcessor(ex);
            }
        }

        public static async Task<FlowmeterMeasurementData[]> GetFlowmeterMeasurementDataFromFireBirdConnectionAsync(DatabaseConnectionConfig config, int cmdTimeout, ObjectSource objectSource, Logger logger) {
            try {
                var list = new List<FlowmeterMeasurementData>();
                using (var conn = new FbConnection(config.ConnectionString)) {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = GetFlowmeterQuery(objectSource, config.FlowmeterTable, DboType.FireBird, logger);
                        command.CommandTimeout = cmdTimeout;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                try {
                                    list.Add(reader.ReadFlowmeterMeasurementFromDb(objectSource, logger));
                                } catch (Exception ex) {
                                    logger.Error(ex);
                                    continue;
                                }
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

        private static Exception mySqlExceptionProcessor(MySqlException ex, Logger logger) {
            switch (ex.Number) {
                case 1054:
                case 1064: {
                        // Todo: send to api IncidentUtil.PutIncident(IncidentKind.SqlQueryError, objectId, ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message);
                        logger.Error("Номер " + ex.Number + " " + ex);
                        break;
                    }
                case 1042: {
                        // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, objectId);
                        logger.Error("Номер " + ex.Number + " " + ex);
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

        private static string GetTransferQuery(ObjectSource source, string tableName, DboType dboType, Logger logger) {
            //
            //
            var queryParams = new[] {
                    source.TankTransferParams.StartTime,
                    source.TankTransferParams.EndTime,
                    source.TankTransferParams.MassStart,
                    source.TankTransferParams.MassFinish,
                    source.TankTransferParams.LevelStart,
                    source.TankTransferParams.LevelFinish,
                    source.TankTransferParams.VolumeStart,
                    source.TankTransferParams.OilProductType,
                    source.TankTransferParams.VolumeFinish
            };
            var queryParamsString = string.Join(", ", queryParams.Where(t => !string.IsNullOrEmpty(t)));
            //{source.TankTransferParams.LevelFinish} is not null and
            //{source.TankTransferParams.LevelStart} is not null and
            var isNotNullParams = $@"
                    {source.TankTransferParams.StartTime} is not null and 
                    {source.TankTransferParams.EndTime} is not null and
                    {source.TankTransferParams.MassStart} is not null and
                    {source.TankTransferParams.MassFinish} is not null and
                    {source.TankTransferParams.VolumeStart} is not null and
                    {source.TankTransferParams.VolumeFinish} is not null";
            var lastSyncDate = LastSyncRecord.GetByExternalId(source.ExternalId.Value, logger).LastTransfersSyncDate;
            const int ps = 100;

            switch (dboType) {
                case DboType.Default: return getDefaultTransferSqlQuery(queryParamsString, source.TankTransferParams.StartTime, tableName, source, ps, lastSyncDate, logger, isNotNullParams);
                case DboType.MySql: return getMySqlTransferQuery(queryParamsString, source.TankTransferParams.StartTime, tableName, source, ps, lastSyncDate, logger);
                case DboType.Oracle: return getOracleTransferQuery(queryParamsString, source.TankTransferParams.StartTime, tableName, source, ps, logger);
                case DboType.FireBird: return getFireBirdTransferQuery(queryParamsString, source.TankTransferParams.StartTime, tableName, source, ps, lastSyncDate, logger);
                default: throw new NotImplementedException(dboType.ToString());
            }
        }

        private static string GetMeasurementQuery(ObjectSource source, string tableName, DboType dboType, Logger logger) {
            var queryParams = new[] {
                    source.TankMeasurementParams.Temperature,
                    source.TankMeasurementParams.Level,
                    source.TankMeasurementParams.Volume,
                    source.TankMeasurementParams.Mass,
                    source.TankMeasurementParams.DateTimeStamp,
                    source.TankMeasurementParams.OilProductType,
                    source.TankMeasurementParams.Density
            };
            var isNotNullParams = $@"
                    {source.TankMeasurementParams.Temperature} is not null and
                    {source.TankMeasurementParams.Level} is not null and
                    {source.TankMeasurementParams.Volume} is not null and
                    {source.TankMeasurementParams.Mass} is not null and
                    {source.TankMeasurementParams.DateTimeStamp} is not null and
                    {source.TankMeasurementParams.OilProductType} is not null and
                    {source.TankMeasurementParams.Density} is not null";
            var queryParamsString = string.Join(", ", queryParams.Where(t => !string.IsNullOrEmpty(t)));
            var lastSyncDate = LastSyncRecord.GetByExternalId(source.ExternalId.Value, logger).LastMeasurementsSyncDate;
            const int ps = 100;

            switch (dboType) {
                case DboType.Default: return getDefaultMeasurementSqlQuery(queryParamsString, source.TankMeasurementParams.DateTimeStamp, tableName, source, ps, lastSyncDate, logger);
                case DboType.MySql: return getMySqlMeasurementQuery(queryParamsString, source.TankMeasurementParams.DateTimeStamp, tableName, source, lastSyncDate, ps, logger);
                case DboType.Oracle: return getOracleMeasurementQuery(queryParamsString, source.TankMeasurementParams.DateTimeStamp, tableName, source, ps, logger);
                case DboType.FireBird: return getFireBirdMeasurementQuery(queryParamsString, source.TankMeasurementParams.DateTimeStamp, tableName, source, ps, lastSyncDate, logger);
                default: throw new NotImplementedException(dboType.ToString());
            }
        }

        private static string GetFlowmeterQuery(ObjectSource source, string tableName, DboType dboType, Logger logger) {
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

        private static string getDefaultMeasurementSqlQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.MeasurementCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT TOP({ps}) {queryParams} FROM {tableName} {lastSyncDateQuery}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getDefaultFlowmeterSqlQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.FlowmeterCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT TOP({ps}) {queryParams} FROM {tableName} {lastSyncDateQuery}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getDefaultTransferSqlQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger, string isNotNullParam) {
            var lastSyncDateQuery = $"{source.TransferCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT TOP({ps}) {queryParams} FROM {tableName} {lastSyncDateQuery} and {isNotNullParam}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getMySqlMeasurementQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, DateTime lastSyncDate, int ps, Logger logger) {
            var lastSyncDateQuery = $"{source.MeasurementCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-dd HH:mm:ss.fff")}'";
            var query = $"SELECT {queryParams} FROM {tableName} {lastSyncDateQuery} Limit {ps}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getMySqlFlowmeterQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.FlowmeterCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
            var query = $"SELECT {queryParams} FROM {tableName} {lastSyncDateQuery} Limit {ps}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getMySqlTransferQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.TransferCondition} and {timeStampParam} > '{lastSyncDate.ToString("yyyy-MM-dd HH:mm:ss.fff")}'";
            var query = $"SELECT {queryParams} FROM {tableName} {lastSyncDateQuery} Limit {ps}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getOracleMeasurementQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, Logger logger) {
            var lastSyncDateQuery = $"{source.MeasurementCondition} and {timeStampParam} > :lastDateTime order by {timeStampParam} asc) WHERE ROWNUM <= {ps}";
            var query = $"SELECT * FROM (SELECT {queryParams} FROM {tableName} {lastSyncDateQuery}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getOracleFlowmeterQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, Logger logger) {
            var lastSyncDateQuery = $"{source.FlowmeterCondition} and {timeStampParam} > :lastDateTime order by {timeStampParam} asc) WHERE ROWNUM <= {ps}";
            var query = $"SELECT * FROM (SELECT {queryParams} FROM {tableName} {lastSyncDateQuery}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getOracleTransferQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, Logger logger) {
            var lastSyncDateQuery = $"{source.TransferCondition} and {timeStampParam} > :lastDateTime order by {timeStampParam} asc) WHERE ROWNUM <= {ps}";
            var query = $"SELECT * FROM (SELECT {queryParams} FROM {tableName} {lastSyncDateQuery}";
            //logger.Debug($"Query - {query}");
            return query;
        }

        private static string getFireBirdMeasurementQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.MeasurementCondition} and {timeStampParam} > '{lastSyncDate.ToString("MM-dd-yyyy HH:mm:ss.fff")}' order by {timeStampParam} asc";
            var query = $"SELECT FIRST {ps} {queryParams} FROM {tableName} {lastSyncDateQuery}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getFireBirdFlowmeterQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.FlowmeterCondition} and {timeStampParam} > '{lastSyncDate.ToString("MM-dd-yyyy HH:mm:ss.fff")}' order by {timeStampParam} asc";
            var query = $"SELECT FIRST {ps} {queryParams} FROM {tableName} {lastSyncDateQuery}";
            logger.Debug($"Query - {query}");
            return query;
        }

        private static string getFireBirdTransferQuery(string queryParams, string timeStampParam, string tableName, ObjectSource source, int ps, DateTime lastSyncDate, Logger logger) {
            var lastSyncDateQuery = $"{source.TransferCondition} and {timeStampParam} > '{lastSyncDate.ToString("MM-dd-yyyy HH:mm:ss.fff")}' order by {timeStampParam} asc";
            var query = $"SELECT FIRST {ps} {queryParams} FROM {tableName} {lastSyncDateQuery}";
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
                case DboType.Dbf: return getDbfConnectionState(config.ConnectionString);
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

        private static bool getDbfConnectionState(string connectionString) {
            return Directory.Exists(connectionString);
        }

        #endregion
    }
}

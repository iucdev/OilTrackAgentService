using AgentService.References;
using Newtonsoft.Json;
using NLog;
using Service.Dtos;
using Service.LocalDb;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace AgentService.Models {
    public class TankTransferRecord {
        public string InternalTankId { get; set; }
        public long ExternalTankId { get; set; }

        public DateTime StartDate { get; set; }
        public string StartDateText => $"{StartDate.ToShortDateString()} {StartDate.ToShortTimeString()}";

        public DateTime EndDate { get; set; }
        public string EndDateText => EndDate.ToShortDateString();
        public string EndTimeText => EndDate.ToShortTimeString();

        public decimal LevelStart { get; set; }
        public decimal LevelEnd { get; set; }
        public LevelUnitType LevelUnitType { get; set; }
        public string LevelUnitTypeText => LevelUnitType.ToDisplayText();
        public string LevelStartWithUnit => $"{Math.Round(LevelStart, 2)} {LevelUnitType.ToDisplayText()}";
        public string LevelEndWithUnit => $"{Math.Round(LevelEnd, 2)} {LevelUnitType.ToDisplayText()}";
        public string LevelEndText => $"{Math.Round(LevelEnd, 2)}";

        public decimal MassStart { get; set; }
        public decimal MassEnd { get; set; }
        public MassUnitType MassUnitType { get; set; }
        public string MassUnitTypeText => MassUnitType.ToDisplayText();
        public string MassStartWithUnit => $"{Math.Round(MassStart, 2)} {MassUnitType.ToDisplayText()}";
        public string MassEndWithUnit => $"{Math.Round(MassEnd, 2)} {MassUnitType.ToDisplayText()}";
        public string MassEndText => $"{Math.Round(MassEnd, 2)}";

        public decimal VolumeStart { get; set; }
        public decimal VolumeEnd { get; set; }
        public VolumeUnitType VolumeUnitType { get; set; }
        public string VolumeUnitTypeText => VolumeUnitType.ToDisplayText();
        public string VolumeStartWithUnit => $"{Math.Round(VolumeStart, 2)} {VolumeUnitType.ToDisplayText()}";
        public string VolumeEndWithUnit => $"{Math.Round(VolumeEnd, 2)} {VolumeUnitType.ToDisplayText()}";
        public string VolumeEndText => $"{Math.Round(VolumeEnd, 2)}";

        public TransferOperationType? OperationType { get; set; }
        public string OperationTypeText => OperationType.HasValue ? OperationType.Value.ToDisplayText() : string.Empty;
        public string OperationTypeColor => OperationType == TransferOperationType.Income ? "#9900C54E" : "#994E5BF2";
        
        public OilProductType OilProductType { get; set; }
        public string OilProductTypeText => OilProductType.ToDisplayText();

        public TankTransfers AddToDb(Logger logger) {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();

                // Запрос для проверки существования записи
                string checkExistQuery = $@"
            SELECT COUNT(1)
            FROM {nameof(TankTransferRecord)}
            WHERE 
                {nameof(StartDate)} = @{nameof(StartDate)} AND
                {nameof(EndDate)} = @{nameof(EndDate)} AND
                {nameof(OperationType)} = @{nameof(OperationType)} AND
                {nameof(OilProductType)} = @{nameof(OilProductType)} AND
                {nameof(MassStart)} = @{nameof(MassStart)} AND
                {nameof(MassEnd)} = @{nameof(MassEnd)} AND
                {nameof(VolumeStart)} = @{nameof(VolumeStart)} AND
                {nameof(VolumeEnd)} = @{nameof(VolumeEnd)} AND
                {nameof(OilProductType)} = @{nameof(OilProductType)}
        ";

                // Запрос для вставки новой записи
                string insertQuery = $@"
            INSERT INTO {nameof(TankTransferRecord)} (
                {nameof(InternalTankId)},
                {nameof(ExternalTankId)},
                {nameof(StartDate)},
                {nameof(EndDate)},
                {nameof(LevelStart)},
                {nameof(LevelEnd)},
                {nameof(LevelUnitType)},
                {nameof(MassStart)},
                {nameof(MassEnd)},
                {nameof(MassUnitType)},
                {nameof(VolumeStart)},
                {nameof(VolumeEnd)},
                {nameof(VolumeUnitType)},
                {nameof(OperationType)},
                {nameof(OilProductType)}
            ) VALUES (
                @{nameof(InternalTankId)},
                @{nameof(ExternalTankId)},
                @{nameof(StartDate)},
                @{nameof(EndDate)},
                @{nameof(LevelStart)},
                @{nameof(LevelEnd)},
                @{nameof(LevelUnitType)},
                @{nameof(MassStart)},
                @{nameof(MassEnd)},
                @{nameof(MassUnitType)},
                @{nameof(VolumeStart)},
                @{nameof(VolumeEnd)},
                @{nameof(VolumeUnitType)},
                @{nameof(OperationType)},
                @{nameof(OilProductType)}
            )
        ";

                using (var checkCommand = new SQLiteCommand(checkExistQuery, connection)) {
                    // Добавляем параметры для проверки
                    checkCommand.Parameters.AddWithValue($"@{nameof(StartDate)}", StartDate);
                    checkCommand.Parameters.AddWithValue($"@{nameof(EndDate)}", EndDate);
                    checkCommand.Parameters.AddWithValue($"@{nameof(OperationType)}", OperationType);
                    checkCommand.Parameters.AddWithValue($"@{nameof(OilProductType)}", OilProductType);
                    checkCommand.Parameters.AddWithValue($"@{nameof(MassStart)}", MassStart);
                    checkCommand.Parameters.AddWithValue($"@{nameof(MassEnd)}", MassEnd);
                    checkCommand.Parameters.AddWithValue($"@{nameof(VolumeStart)}", VolumeStart);
                    checkCommand.Parameters.AddWithValue($"@{nameof(VolumeEnd)}", VolumeEnd);
                    checkCommand.Parameters.AddWithValue($"@{nameof(OilProductType)}", OilProductType);

                    // Выполняем проверку существования записи
                    var exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;

                    if (!exists) {
                        using (var insertCommand = new SQLiteCommand(insertQuery, connection)) {
                            // Добавляем параметры для вставки
                            insertCommand.Parameters.AddWithValue($"@{nameof(InternalTankId)}", InternalTankId);
                            insertCommand.Parameters.AddWithValue($"@{nameof(ExternalTankId)}", ExternalTankId);
                            insertCommand.Parameters.AddWithValue($"@{nameof(StartDate)}", StartDate);
                            insertCommand.Parameters.AddWithValue($"@{nameof(EndDate)}", EndDate);
                            insertCommand.Parameters.AddWithValue($"@{nameof(LevelStart)}", LevelStart);
                            insertCommand.Parameters.AddWithValue($"@{nameof(LevelEnd)}", LevelEnd);
                            insertCommand.Parameters.AddWithValue($"@{nameof(LevelUnitType)}", LevelUnitType);
                            insertCommand.Parameters.AddWithValue($"@{nameof(MassStart)}", MassStart);
                            insertCommand.Parameters.AddWithValue($"@{nameof(MassEnd)}", MassEnd);
                            insertCommand.Parameters.AddWithValue($"@{nameof(MassUnitType)}", MassUnitType);
                            insertCommand.Parameters.AddWithValue($"@{nameof(VolumeStart)}", VolumeStart);
                            insertCommand.Parameters.AddWithValue($"@{nameof(VolumeEnd)}", VolumeEnd);
                            insertCommand.Parameters.AddWithValue($"@{nameof(VolumeUnitType)}", VolumeUnitType);
                            insertCommand.Parameters.AddWithValue($"@{nameof(OperationType)}", OperationType);
                            insertCommand.Parameters.AddWithValue($"@{nameof(OilProductType)}", OilProductType);

                            // Выполняем вставку
                            insertCommand.ExecuteNonQuery();

                            // Добавляем запись в список уникальных
                            return new TankTransfers() {
                                TankId = ExternalTankId,
                                Transfers = new TankTransferData[] {
                                    new TankTransferData() {
                                        StartDate = StartDate,
                                        EndDate = EndDate,
                                     
                                        LevelEnd = LevelEnd,
                                        LevelStart = LevelStart,
                                        LevelUnitType = LevelUnitType,
                                        
                                        VolumeStart = VolumeStart,
                                        VolumeEnd = VolumeEnd,
                                        VolumeUnitType = VolumeUnitType,
                                        
                                        MassEnd = MassEnd,
                                        MassStart = MassStart,
                                        MassUnitType = MassUnitType,
                                        
                                        OilProductType = OilProductType,
                                        OperationType = OperationType
                                    }
                                }
                            };
                        }
                    } else {
                        logger.Error($"Запись с InternalTankId: {InternalTankId}, StartDate: {StartDate}, EndDate: {EndDate} уже существует.");
                    }
                }
            }
            return null;
        }


    }
}

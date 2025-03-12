using NLog;
using Opc.Hda;
using Service.Clients.Utils;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Service.Common {
    public static class CommonHelper {
        public static OilProductType TryGetOilProductType(string rawVal, Logger logger) {
            var oilProductMapping = ObjectSettingsSingleton.Instance.ObjectSettings.OilProductTypeMapping;
            if (oilProductMapping.ContainsKey(rawVal)) {
                return oilProductMapping[rawVal];
            } else {
                logger.Error($"Не удалось найти вид нефтепродукта в маппинге для значения {rawVal}. Пожалуйста, добавьте это значение в маппинг в objectSettings.json");
                return OilProductType.UNKNOWN;
            }
        }
        
        public static FlowmeterOperationType TryGetFlowmeterOperationType(string rawVal, Logger logger) {
            var opType = FlowmeterOperationType.Undefined;
            if (!Enum.TryParse<FlowmeterOperationType>(rawVal, out opType)) {
                logger.Error($"Exception found. Unexpected operation type: {rawVal}.");
            }
            return opType;
        }

        public static long TryGetSourceTankId(string rawVal, ObjectSource objectSource) {
            var objectSettings = ObjectSettingsSingleton.Instance.ObjectSettings.Objects.FirstOrDefault(e => e.ObjectSources.Any(s => s.InternalId == objectSource.InternalId && s.ExternalId == objectSource.ExternalId));
            
            if (objectSettings is null) {
                return 0;
            }

            var tankId = objectSettings.ObjectSources.FirstOrDefault(t => t.InternalId == rawVal)?.ExternalId;
            if (tankId is null) {
                return 0;
            }

            return tankId.Value;
        }

        public static TankMeasurementData[] SetEnums(this TankMeasurementData[] items, ObjectSource source, OilProductType? oilProductType = null) {
            foreach (var i in items) {
                i.OilProductType = oilProductType.HasValue ? oilProductType.Value : source.OilProductType;
                i.LevelUnitType = source.LevelUnitType;
                i.MassUnitType = source.MassUnitType;
                i.VolumeUnitType = source.VolumeUnitType;
            }
            return items;
        }

        public static TankTransferData[] SetEnums(this TankTransferData[] items, ObjectSource source, OilProductType? oilProductType = null) {
            foreach (var i in items) {
                i.OilProductType = oilProductType.HasValue ? oilProductType.Value : source.OilProductType;
                i.OperationType = i.VolumeEnd > i.VolumeStart
                    ? TransferOperationType.Income
                    : TransferOperationType.Outcome;
                i.LevelUnitType = source.LevelUnitType;
                i.MassUnitType = source.MassUnitType;
                i.VolumeUnitType = source.VolumeUnitType;
            }
            return items;
        }

        public static FlowmeterMeasurementData[] SetEnums(this FlowmeterMeasurementData[] items, ObjectSource source, FlowmeterOperationType operationType, OilProductType? oilProductType = null)
        {
            foreach (var i in items) {
                i.OperationType = operationType;
                i.OilProductType = oilProductType.HasValue ? oilProductType.Value : source.OilProductType;
                i.MassUnitType = source.MassUnitType;
                i.VolumeUnitType = source.VolumeUnitType;
            }
            return items;
        }

        public static List<TankMeasurementData> SetEnums(this List<TankMeasurementData> items, ObjectSource source) {
            items.ForEach(i => {
                i.OilProductType = source.OilProductType;
                i.LevelUnitType = source.LevelUnitType;
                i.MassUnitType = source.MassUnitType;
                i.VolumeUnitType = source.VolumeUnitType;
            });
            return items;
        }

        public static List<TankTransferData> SetEnums(this List<TankTransferData> items, ObjectSource source) {
            items.ForEach(i => {
                i.OilProductType = source.OilProductType;
                i.OperationType = i.VolumeEnd > i.VolumeStart
                    ? TransferOperationType.Income
                    : TransferOperationType.Outcome;
                i.LevelUnitType = source.LevelUnitType;
                i.MassUnitType = source.MassUnitType;
                i.VolumeUnitType = source.VolumeUnitType;
            });
            return items;
        }

        public static DateTime GetDateTime(string date, string time) {
            var dateTimeString = $"{date} {time}";

            string[] dateFormats = { "yyyyMMdd HH:mm:ss", "yyyyMMdd H:mm:ss" };
            if (DateTime.TryParseExact(dateTimeString, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)) {
                return result;
            }

            throw new NotImplementedException($"Неверный формат даты {dateTimeString}");
        }

        public static decimal ParseDecimal(object value) {
            if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)) {
                return result;
            }
            throw new FormatException($"Невозможно преобразовать '{value}' в decimal.");
        }
    }
}

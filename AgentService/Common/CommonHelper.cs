﻿using Service.Clients.Utils;
using Sunp.Api.Client;
using System.Collections.Generic;

namespace Service.Common {
    public static class CommonHelper {
        public static OilProductType TryGetOilProductType(string rawVal) {
            var oilProductMapping = ObjectSettingsSingleton.Instance.ObjectSettings.OilProductTypeMapping;
            if (oilProductMapping.ContainsKey(rawVal)) {
                return oilProductMapping[rawVal];
            } else {
                throw new System.Exception($"Не удалось найти вид нефтепродукта в маппинге для значения {rawVal}. Пожалуйста, добавьте это значение в маппинг в objectSettings.json");
            }
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

        public static FlowmeterMeasurementData[] SetEnums(this FlowmeterMeasurementData[] items, ObjectSource source, OilProductType? oilProductType = null)
        {
            foreach (var i in items) {
                i.OilProductType = oilProductType.HasValue ? oilProductType.Value : source.OilProductType;
                i.MassUnitType = source.MassUnitType;
                i.VolumeUnitType = source.VolumeUnitType;
            }
            return items;
        }

        public static List<TankMeasurementData> SetEnums(this List<TankMeasurementData> items, ObjectSource source, OilProductType? oilProductType = null) {
            items.ForEach(i => {
                i.OilProductType = oilProductType.HasValue ? oilProductType.Value : source.OilProductType;
                i.LevelUnitType = source.LevelUnitType;
                i.MassUnitType = source.MassUnitType;
                i.VolumeUnitType = source.VolumeUnitType;
            });
            return items;
        }

        public static List<TankTransferData> SetEnums(this List<TankTransferData> items, ObjectSource source, OilProductType? oilProductType = null) {
            items.ForEach(i => {
                i.OilProductType = oilProductType.HasValue ? oilProductType.Value : source.OilProductType;
                i.OperationType = i.VolumeEnd > i.VolumeStart
                    ? TransferOperationType.Income
                    : TransferOperationType.Outcome;
                i.LevelUnitType = source.LevelUnitType;
                i.MassUnitType = source.MassUnitType;
                i.VolumeUnitType = source.VolumeUnitType;
            });
            return items;
        }
    }
}
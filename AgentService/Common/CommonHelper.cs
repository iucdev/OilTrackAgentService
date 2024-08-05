using Service.Clients.Utils;
using Sunp.Api.Client;
using System.Collections.Generic;

namespace Service.Common {
    public static class CommonHelper {
        public static TankMeasurementData[] SetEnums(this TankMeasurementData[] items, ObjectSource source) {
            foreach (var i in items) {
                i.OilProductType = source.OilProductType;
                i.LevelUnitType = source.LevelUnitType;
                i.MassUnitType = source.MassUnitType;
                i.VolumeUnitType = source.VolumeUnitType;
            }
            return items;
        }

        public static TankTransferData[] SetEnums(this TankTransferData[] items, ObjectSource source) {
            foreach (var i in items) {
                i.OilProductType = source.OilProductType;
                i.OperationType = i.VolumeEnd > i.VolumeStart
                    ? TransferOperationType.Income
                    : TransferOperationType.Outcome;
                i.LevelUnitType = source.LevelUnitType;
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
    }
}

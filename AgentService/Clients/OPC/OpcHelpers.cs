using NLog;
using Opc.Da;
using Opc.Hda;
using Service.Common;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Clients.OPC {
    public static class OpcHelpers {//ItemIdentifier
        public static List<Opc.Da.Item> AddIfNotNull(this List<Opc.Da.Item> list, string item) {
            if (string.IsNullOrEmpty(item)) {
                return list;
            } else {
                list.Add(new Opc.Da.Item { ItemName = item, EnableBuffering = true });
                return list;
            }
        }

        public static Trend AddIfNotNull(this Trend group, string item) {
            if (string.IsNullOrEmpty(item)) {
                return group;
            } else {
                _ = group.Server.ValidateItems(new [] { new Opc.ItemIdentifier { ItemName = item } });
                group.AddItem(new Opc.ItemIdentifier { ItemName = item });
                return group;
            }
        }

        private static DateTime? ParseDate(object val) {

            DateTime dateStamp;
            if (val is DateTime)
                return (DateTime)val;
            else
                if (DateTime.TryParse(ToSafeString(val), out dateStamp))
                return dateStamp;
            return null;
        }

        public static DateTime GetDateTime(ItemValueCollection[] itemValueCollections, string item) {
            var result = itemValueCollections.FirstOrDefault(r => r.ItemName == item);
            var enumerator = result.GetEnumerator();
            enumerator.MoveNext();
            var pData = (Opc.Hda.ItemValue)enumerator.Current;
            return ParseDate(pData).Value;
        }

        public static decimal GetDecimal(ItemValueCollection[] itemValueCollections, string item) {
            var result = itemValueCollections.FirstOrDefault(r => r.ItemName == item);
            var enumerator = result.GetEnumerator();
            enumerator.MoveNext();
            var pData = (Opc.Hda.ItemValue)enumerator.Current;
            return decimal.Parse(ToSafeString(pData.Value).Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        public static ItemValueResult GetValueByName(ItemValueResult[] itemValueResults, string searchedItemName, Logger logger) {
            var result = itemValueResults.FirstOrDefault(t => t.ItemName == searchedItemName);
            if (result == null) {
                logger.Error("Param {0} no result" + Environment.NewLine, searchedItemName);
                throw new NotImplementedException(searchedItemName);
            }
            if (result.Value is byte[]) {
                var buffer = (byte[])result.Value;
                result.Value = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            }
            return result;
        }

        public static ItemValueCollection GetValueByName(ItemValueCollection[] itemValueResults, string searchedItemName, Logger logger) {
            var result = itemValueResults.FirstOrDefault(t => t.ItemName == searchedItemName);
            if (result == null) {
                logger.Error("Param {0} no result" + Environment.NewLine, searchedItemName);
                throw new NotImplementedException(searchedItemName);
            }
            return result;
        }

        public static decimal TryGetDecimal(ItemValueResult[] itemValueResults, string searchedItemName, Logger logger) {
            var result = ToSafeString(OpcHelpers.GetValueByName(itemValueResults, searchedItemName, logger).Value).Replace(',', '.');
            if (string.IsNullOrEmpty(result)) {
                logger.Error("Param {0} Result is null or Empty", searchedItemName, result);
                throw new InvalidOperationException();
            }
            return decimal.Parse(result, CultureInfo.InvariantCulture);
        }

        public static DateTime TryGetDateTime(ItemValueResult[] itemValueResults, string searchedItemName, Logger logger) {
            var result = OpcHelpers.GetValueByName(itemValueResults, searchedItemName, logger);
            DateTime dateStamp;
            if (result.Value is DateTime) {
                logger.Debug($"Value is DT {result.Value}");
                return (DateTime)result.Value;
            } else if (DateTime.TryParse(ToSafeString(result.Value), out dateStamp)) {
                logger.Debug($"Parse DT {dateStamp}");
                return dateStamp;
            }
            return result.Timestamp;
        }

        private static string ToSafeString(object val) {
            return val == null ? string.Empty : val.ToString().Replace("\0", string.Empty);
        }

        public static string TryGetString(ItemValueResult[] itemValueResults, string searchedItemName, Logger logger)
        {
            var result = OpcHelpers.GetValueByName(itemValueResults, searchedItemName, logger).Value;
            if (string.IsNullOrEmpty(ToSafeString(result))) {
                logger.Error("Param {0} Result is null or Empty", searchedItemName, result);
                throw new InvalidOperationException();
            }
            var rawVal = ToSafeString(result);
            return rawVal;
        }

        public static OilProductType TryGetOilProductType(ItemValueResult[] itemValueResults, string searchedItemName, Logger logger)
        {
            var result = OpcHelpers.GetValueByName(itemValueResults, searchedItemName, logger).Value;
            if (string.IsNullOrEmpty(ToSafeString(result))) {
                logger.Error("Param {0} Result is null or Empty", searchedItemName, result);
                throw new InvalidOperationException();
            }
            var rawVal = ToSafeString(result);
            var oilProductType = CommonHelper.TryGetOilProductType(rawVal, logger);
            return oilProductType;
        }

        public static FlowmeterOperationType TryGetFlowmeterOperationType(ItemValueResult[] itemValueResults, string searchedItemName, Logger logger)
        {
            var result = OpcHelpers.GetValueByName(itemValueResults, searchedItemName, logger).Value;
            if (string.IsNullOrEmpty(ToSafeString(result))) {
                logger.Error("Param {0} Result is null or Empty", searchedItemName, result);
                throw new InvalidOperationException();
            }
            var rawVal = ToSafeString(result);
            if (!Enum.TryParse<FlowmeterOperationType>(rawVal, out var opType)) {
                logger.Error($"Exception found. Unexpected operation type: {rawVal}.");
            }
            return opType;
        }
    }
}

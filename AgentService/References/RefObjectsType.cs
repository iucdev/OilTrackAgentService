using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentService.References {
    public class RefObjectsType {
        public static IReadOnlyDictionary<ObjectsType, string> StatusVsText => _statusVsText;
        private static readonly Dictionary<ObjectsType, string> _statusVsText = new Dictionary<ObjectsType, string>() {
            [ObjectsType.Refinery] = "Нефтеперерабатывающий завод",
            [ObjectsType.RefineryLow] = "Нефтеперерабатывающий завод малой мощности",
            [ObjectsType.PetrolStation] = "Автозаправочная станция",
            [ObjectsType.OilBase] = "Нефтебаза",
        };
    }

    public static class RefObjectsTypeExtensions {
        public static string ToDisplayText(this ObjectsType status) => RefObjectsType.StatusVsText[status];
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ObjectsType {
        PetrolStation,
        Refinery,
        RefineryLow,
        OilBase
    }
}

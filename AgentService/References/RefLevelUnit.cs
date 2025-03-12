using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Sunp.Api.Client;

namespace AgentService.References {
    public class RefLevelUnit {
        public static IReadOnlyDictionary<LevelUnitType, string> UnitTypeVsText => _statusVsText;
        private static readonly Dictionary<LevelUnitType, string> _statusVsText = new Dictionary<LevelUnitType, string>() {
            [LevelUnitType.Centimeter] = "см",
            [LevelUnitType.Meter] = "м",
            [LevelUnitType.Millimeter] = "мм"
        };
    }

    public static class RefLevelUnitExtension {
        public static string ToDisplayText(this LevelUnitType unitType) => RefLevelUnit.UnitTypeVsText[unitType];
    }
}

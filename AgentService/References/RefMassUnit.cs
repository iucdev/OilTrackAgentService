using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Sunp.Api.Client;

namespace AgentService.References {
    public class RefMassUnit {
        public static IReadOnlyDictionary<MassUnitType, string> UnitTypeVsText => _statusVsText;
        private static readonly Dictionary<MassUnitType, string> _statusVsText = new Dictionary<MassUnitType, string>() {
            [MassUnitType.Kilogram] = "кг",
            [MassUnitType.Quintal] = "ц",
            [MassUnitType.Ton] = "т",
        };
    }

    public static class RefMassUnitExtensions {
        public static string ToDisplayText(this MassUnitType unitType) => RefMassUnit.UnitTypeVsText[unitType];
    }
}

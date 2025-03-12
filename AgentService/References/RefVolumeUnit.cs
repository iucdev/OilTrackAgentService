using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Sunp.Api.Client;

namespace AgentService.References {
    public class RefVolumeUnit {
        public static IReadOnlyDictionary<VolumeUnitType, string> UnitTypeVsText => _statusVsText;
        private static readonly Dictionary<VolumeUnitType, string> _statusVsText = new Dictionary<VolumeUnitType, string>() {
            [VolumeUnitType.CubicMeter] = "м³ (1000 л)",
            [VolumeUnitType.CubicCentimeter] = "см³ (0.001 л)",
            [VolumeUnitType.CubicDecimeter] = "дм³ (л)",
        };
    }

    public static class RefVolumeUnitExtension {
        public static string ToDisplayText(this VolumeUnitType unitType) => RefVolumeUnit.UnitTypeVsText[unitType];
    }
}

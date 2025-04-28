using AgentService.References;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentService.Models {
    public class TankIndicatorRecord {
        public TankIndicatorRecord(string internalTankId, long externalTankId, Measurement tankIndicators) {
            InternalTankId = internalTankId;
            ExternalTankId = externalTankId;
            TankIndicators = tankIndicators;
        }

        public string InternalTankId { get; set; }
        public long ExternalTankId { get; set; }
        public Measurement TankIndicators { get; set; }
    }

    public class TankStatusCardViewModel {
        public string InternalTankId { get; set; }
        public string OilProductTypeText { get; set; }
        public int LevelPercent { get; set; }
        public string StatusText { get; set; } = "Активный";
        public string StatusColor { get; set; } = "#4CAF50";
        public double CurrentVolume { get; set; }
        public double MaxVolume { get; set; }
        public string UnitText { get; set; } = "дм³";
    }


    public class TankIndicator {
        public long TankId { get; set; }
        public Measurement[] Measurements { get; set; }
    }

    public class Measurement {
        public DateTime MeasurementDate { get; set; }
        public string MeasurementDateOnly => MeasurementDate.ToShortDateString();
        public string MeasurementTime => MeasurementDate.ToShortTimeString();
        
        public double Mass { get; set; }
        public double MassRounded => Math.Round(Mass, 2);
        public MassUnitType MassUnitType { get; set; }
        public string MassWithUnit => $"{Mass} {MassUnitType.ToDisplayText()}";
        public string MassUnitText => MassUnitType.ToDisplayText();
        
        public double Volume { get; set; }
        public double VolumeRounded => Math.Round(Volume, 2);
        public VolumeUnitType VolumeUnitType { get; set; }
        public string VolumeWithUnit => $"{Volume} {VolumeUnitType.ToDisplayText()}";
        public string VolumeUnitText => VolumeUnitType.ToDisplayText();
        
        public double Level { get; set; }
        public double LevelRounded => Math.Round(Level, 2);
        public LevelUnitType LevelUnitType { get; set; }
        public string LevelWithUnit => $"{Level} {LevelUnitType.ToDisplayText()}";
        public string LevelUnitText => LevelUnitType.ToDisplayText();
        
        public double Density { get; set; }
        public double DensityRounded => Math.Round(Density, 2);

        public double Temperature { get; set; }
        
        public OilProductType OilProductType { get; set; }
        public string OilProductTypeText => OilProductType.ToDisplayText();
    }
}

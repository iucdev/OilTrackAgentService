namespace Service.Clients.IGLA
{
    public class IglaCmd
    {
        public const int STX = 0x40;
        public const int ETX = 0x2A;
        public const int ETX2 = 0x0D;
        public const int Version = 0x01;
        public const int Copyright = 0x02;
        public const int ParamN = 0x03;
        public const int EstablishParamN = 0x83;
        public const int LevelFuel = 0x04;
        public const int LevelWater = 0x05;
        public const int AvrTemp = 0x06;
        public const int NPointTemp = 0x07;
        public const int DensityFuel = 0x08;
        public const int ResultedDensityFuel = 0x09;
        public const int NDensityFuel = 0x0A;
        public const int ControllerStatus = 0x0C;
        public const int GaugeConfig = 0x0D;
        public const int StratificationLevel = 0x0E;
        public const int DensimeterNTemp = 0x0F;
        public const int VolumeFuel = 0x10;
        public const int WeightFuel = 0x11;
        public const int FullInfoCMD = 0x1C;
        public const int StartMeasureCMD = 0x8A;
    }
}

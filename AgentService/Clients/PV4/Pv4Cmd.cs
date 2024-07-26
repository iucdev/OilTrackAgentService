using System.Runtime.InteropServices;

namespace Service.Clients.PV4 {
    public class Pv4Cmd {
        public const int STX = 0x02;
        public const int ETX = 0x03;
        public const int TankInventory = 0x48;
        public const int SsVersion = 0x49;
        public const int TankInventoryExt = 0x68;
        public const int TankStatus = 0x53;
        public const int TankLevels = 0x4C;
        public const int TankLevelsExt = 0x6C;
        public const int SensorStatus = 0x4F;
        public const int ProductWeight = 0x57;
        public const int ProductWeightExt = 0x77;
        public const int ProductDensity = 0x47;
        public const int DeliveryEnquiry = 0x44;    //D
        public const int DeliveryEnquiryExt = 0x64; //d
    }

    public enum Unit : byte {
        Metric = 0x4C,//'L',
        Usa = 0x47,//'G',
        Imperial = 0x49//'I'
    }

    public enum TankInventoryTdStatus {
        TankOk = 0,// tank O.K.
        ProbeError = 1,// probe/SM error
        TankNotFound = 2,// tank was not found
        BadTemp = 3,// bad temperature
        ProductVolOutOfRange = 4,// product volume out of the range
        WaterVolOutOfRange = 5,// water volume out of the range
        ProductHightOutOfRange = 6// water hight out of the range
    }

    public enum DensityDeviceStatus {
        TankOk = 0,//OK 
        Reserved = 1, //reserved
        Reserved2 = 2, //reserved
        NotConfigurated = 3, //density not configured for tank
        TankNotFound = 4, //tank was not found
        VolDataNotAviable = 5 //product volume data not available
    }

    public enum DeliveryStatus : byte {
        None = 0x4E,//'N',
        Stable = 0x53,//'S',
        Unstable = 0x55//'U'
    }

    /// <summary>
    /// Command - d (DeliveryEnquiry Extended format) 131 byte
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 131)]
    public class DeliveryEnquiryExt {
        [MarshalAs(UnmanagedType.U1)] //0
        public byte STX;//(0x02H)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] //1
        public byte[] nn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] //3
        public byte[] mm;
        [MarshalAs(UnmanagedType.U1)] //5
        public DeliveryStatus s;
        [MarshalAs(UnmanagedType.U1)]  //6
        public Unit u;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] //7
        public byte[] StartDelivery;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //17
        public byte[] ppppp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //22
        public byte[] ttttb;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //26
        public byte[] uuuuu;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //31
        public byte[] wwww;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] //35
        public byte[] EndDelivery;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //45
        public byte[] rrrrr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //50
        public byte[] tttta;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //54
        public byte[] uuuuua;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //59
        public byte[] vvvv;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //63
        public byte[] ooooo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //68
        public byte[] tttt;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] //72
        public byte[] StartDeliveryCalc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //82
        public byte[] sssss;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] //87
        public byte[] ff;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //89
        public byte[] nnnnn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //94
        public byte[] bbbbb;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] //99
        public byte[] dddddd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //105
        public byte[] ttttem;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //109
        public byte[] qqqq;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //113
        public byte[] xxxx;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //117
        public byte[] yyyyy;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] //122
        public byte[] zzzzzzz;
        [MarshalAs(UnmanagedType.U1)] //129
        public byte ETX;//(0x03H)
        [MarshalAs(UnmanagedType.U1)] //130
        public byte BCC;
    }

    /// <summary>
    /// Command - D (DeliveryEnquiry old format) 56 byte
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 56)]
    public class DeliveryEnquiry {
        [MarshalAs(UnmanagedType.U1)] //0
        public byte STX;//(0x02H)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] //1
        public byte[] nn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] //3
        public byte[] mm;
        [MarshalAs(UnmanagedType.U1)] //5
        public DeliveryStatus s;
        [MarshalAs(UnmanagedType.U1)]  //6
        public Unit u;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] //7
        public byte[] StartDelivery;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //17
        public byte[] ppppp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //22
        public byte[] wwww;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] //26
        public byte[] EndDelivery;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //36
        public byte[] rrrrr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //41
        public byte[] vvvv;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] //45
        public byte[] ooooo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //50
        public byte[] tttt;
        [MarshalAs(UnmanagedType.U1)] //54
        public byte ETX;//(0x03H)
        [MarshalAs(UnmanagedType.U1)] //55
        public byte BCC;
    }
}

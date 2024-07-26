namespace Service.Clients.ModBus
{
    public class StrunaCmd
    {
        public const int Sl = 0x50;
        public const int SlWrite = 0x06;
        public const int SlRead = 0x04;
        public const int SetChannel = 0x01;
        public const int GetMask = 0x02;
        public const int GetAll = 0x03;
    }
}

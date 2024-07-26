namespace Service.Clients.ModBus
{
    public class StrunaHelper
    {
        /// <summary>
        ///  Алгоритм генерации CRC:  Стандартный crc16 modbus, описание в протоколе 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static ushort ModbusCrc16(byte[] buf, int len)
        {
            ushort registrCrc = 0xFFFF;

            //unsafe{ // nice...
            //    byte* bufp=&buf; 
            //    while (len-- > 0)
            //    {
            //        registrCrc ^= (*bufp++);
            //        for (var i = 0; i < 8; i++)
            //            registrCrc = (registrCrc & 0x01) == 1 ? (ushort)((registrCrc >> 1) ^ 0xA001) : (ushort)(registrCrc >> 1);
            //    }
            //}

            for (var i = 0; i < len; i++)
            {
                registrCrc ^= buf[i];
                for (var j = 0; j < 8; j++)
                    registrCrc = (registrCrc & 0x01) == 1 ? (ushort)((registrCrc >> 1) ^ 0xA001) : (ushort)(registrCrc >> 1);
            }
            return registrCrc;
        }

    }

    public enum mbErrors
    {
        /// <summary>
        /// Функция в принятом сообщении не поддерживается на данном SL. Если тип запроса – POLL PROGRAM COMPLETE, этот код указывает, 
        /// что предварительный запрос не был командой программирования.
        /// </summary>
        ILLEGAL_FUNCTION = 0x01,

        /// <summary>
        /// Адрес, указанный в поле данных, является недопустимым для данного SL.
        /// </summary>
        ILLEGAL_DATA_ADDRESS = 0x02,

        /// <summary>
        /// Значения в поле данных недопустимы для данного SL.
        /// </summary>
        ILLEGAL_DATA_VALUE = 0x03,

        /// <summary>
        /// SL не может ответить на запрос или произошла авария.
        /// </summary>
        FAILURE_IN_ASSOCIATED_DEVICE = 0x04,

        /// <summary>
        /// SL принял запрос и начал выполнять долговременную операцию программирования. Для определения момента завершения операции используйте 
        /// запрос типа POLL PROGRAM COMPLETE. Если этот запрос был послан до завершения операции программирования, то SL ответит сообщением REJECTED MESSAGE.
        /// </summary>
        ACKNOWLEDGE = 0x05,

        /// <summary>
        /// Сообщение было принято без ошибок, но SL в данный момент выполняет долговременную операцию программирования. Запрос необходимо ретранслировать позднее.
        /// </summary>
        BUSY_REJECTED_MESSAGE = 0x06,

        /// <summary>
        /// Функция программирования не может быть выполнена. Используйте опрос для получения детальной аппаратно-зависимой информации об ошибке.
        /// </summary>
        NAK_NEGATIVE_ACKNOWLEDGMENT = 0x07,

        /// <summary>
        /// Ошибка связи с БР при выполнении доступа к информации канала
        /// </summary>
        _84h = 0x84,

        /// <summary>
        /// Датчик не инициализирован
        /// </summary>
        _91h = 0x91,

        /// <summary>
        /// Ошибка связи с датчиком
        /// </summary>
        _92h = 0x92,

        /// <summary>
        /// Ошибка связи с устройством
        /// </summary>
        _93h = 0x93,

        /// <summary>
        /// Ошибка связи с БР на этапе определения ТОД
        /// </summary>
        _96h = 0x96,

        /// <summary>
        /// Ошибка записи конфигурации
        /// </summary>
        _9Ah = 0x9A,

        /// <summary>
        /// Ошибка чтения конфигурации
        /// </summary>
        _9Bh = 0x9B,

        /// <summary>
        /// Канал выключен
        /// </summary>
        _9Сh = 0x9C
    }

    public static class MbErrorsHelper
    {
        public static string GetMessage(mbErrors errors)
        {
            switch (errors)
            {
                case mbErrors.ILLEGAL_FUNCTION: return "Функция в принятом сообщении не поддерживается на данном SL. Если тип запроса – POLL PROGRAM COMPLETE, этот код указывает, что предварительный запрос не был командой программирования. ";
                case mbErrors.ILLEGAL_DATA_ADDRESS: return "Адрес, указанный в поле данных, является недопустимым для данного SL.";
                case mbErrors.ILLEGAL_DATA_VALUE: return "Значения в поле данных недопустимы для данного SL.";
                case mbErrors.FAILURE_IN_ASSOCIATED_DEVICE: return "SL не может ответить на запрос или произошла авария.";
                case mbErrors.ACKNOWLEDGE: return "SL принял запрос и начал выполнять долговременную операцию программирования. Для определения момента завершения операции используйте запрос типа POLL PROGRAM COMPLETE. Если этот запрос был послан до завершения операции программирования, то SL ответит сообщением REJECTED MESSAGE.";
                case mbErrors.BUSY_REJECTED_MESSAGE: return "Сообщение было принято без ошибок, но SL в данный момент выполняет долговременную операцию программирования. Запрос необходимо ретранслировать позднее.";
                case mbErrors.NAK_NEGATIVE_ACKNOWLEDGMENT: return "Функция программирования не может быть выполнена. Используйте опрос для получения детальной аппаратно-зависимой информации об ошибке.";
                case mbErrors._84h: return "Ошибка связи с БР при выполнении доступа к информации канала";
                case mbErrors._91h: return "Датчик не инициализирован";
                case mbErrors._92h: return "Ошибка связи с датчиком";
                case mbErrors._93h: return "Ошибка связи с устройством";
                case mbErrors._96h: return "Ошибка связи с БР на этапе определения ТОД";
                case mbErrors._9Ah: return "Ошибка записи конфигурации";
                case mbErrors._9Bh: return "Ошибка чтения конфигурации";
                case mbErrors._9Сh: return "Канал выключен";
            }
            return "Unknown Exception";
        }
    }

}

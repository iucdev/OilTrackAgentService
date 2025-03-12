using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Clients.DBO {
    /// <summary>
    /// Упрощённый класс для чтения DBF-файлов с использованием FileStream для безопасного доступа.
    /// </summary>
    public class SimpleDBFReader : IDisposable {
        private FileStream fileStream;
        private BinaryReader reader;
        private Encoding encoding;
        private int numberOfRecords; // число записей в файле
        private int headerLength;    // длина заголовка
        private int recordLength;    // длина одной записи
        private List<DBFField> fields = new List<DBFField>();

        /// <summary>
        /// Создаёт объект для чтения DBF-файла с безопасным доступом через FileStream.
        /// </summary>
        /// <param name="filePath">Путь к DBF-файлу.</param>
        /// <param name="encoding">Кодировка файла (например, Encoding.ASCII или Encoding.Default).</param>
        public SimpleDBFReader(string filePath, Encoding encoding) {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден", filePath);

            this.encoding = encoding;

            // Используем FileStream с FileShare.ReadWrite для безопасного чтения файла во время его записи другим процессом.
            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            reader = new BinaryReader(fileStream, encoding);

            ReadHeader();
        }

        /// <summary>
        /// Читает заголовок DBF-файла и дескрипторы полей.
        /// </summary>
        private void ReadHeader() {
            byte version = reader.ReadByte();  // версия файла (не используется)
            byte year = reader.ReadByte();     // год обновления
            byte month = reader.ReadByte();    // месяц обновления
            byte day = reader.ReadByte();      // день обновления

            numberOfRecords = reader.ReadInt32();  // число записей (4 байта)
            headerLength = reader.ReadInt16();     // длина заголовка (2 байта)
            recordLength = reader.ReadInt16();     // длина записи (2 байта)

            reader.ReadBytes(20); // Пропускаем оставшиеся 20 байт (резерв, флаги, кодовая страница и т.д.)

            while (true) {
                byte nextByte = reader.ReadByte();
                if (nextByte == 0x0D) // встретили терминатор заголовка
                    break;

                byte[] nameBytes = new byte[11];
                nameBytes[0] = nextByte;
                byte[] remainingName = reader.ReadBytes(10);
                Array.Copy(remainingName, 0, nameBytes, 1, 10);
                string fieldName = encoding.GetString(nameBytes).Trim('\0', ' ');

                char fieldType = (char)reader.ReadByte();
                reader.ReadBytes(4); // адрес поля (не используем)
                byte fieldLength = reader.ReadByte();
                byte decimalCount = reader.ReadByte();
                reader.ReadBytes(14); // Пропускаем оставшиеся байты дескриптора

                fields.Add(new DBFField(fieldName, fieldType, fieldLength, decimalCount));
            }

            int bytesRead = 32 + fields.Count * 32 + 1;
            int extraBytes = headerLength - bytesRead;
            if (extraBytes > 0)
                reader.ReadBytes(extraBytes);
        }

        /// <summary>
        /// Читает записи из DBF-файла с учётом возможного изменения файла другим процессом.
        /// </summary>
        /// <returns>Список записей</returns>
        public List<Dictionary<string, string>> ReadRecords() {
            var records = new List<Dictionary<string, string>>();

            reader.BaseStream.Seek(headerLength, SeekOrigin.Begin);

            for (int i = 0; i < numberOfRecords; i++) {
                byte deletionFlag = reader.ReadByte();
                if (deletionFlag == (byte)'*') {
                    reader.ReadBytes(recordLength - 1);
                    continue;
                }

                var record = new Dictionary<string, string>();
                foreach (var field in fields) {
                    byte[] rawData = reader.ReadBytes(field.Length);
                    string data = encoding.GetString(rawData).Trim();
                    record[field.Name] = data;
                }
                records.Add(record);
            }
            return records;
        }

        /// <summary>
        /// Освобождает ресурсы, включая FileStream.
        /// </summary>
        public void Dispose() {
            reader?.Close();
            reader?.Dispose();
            fileStream?.Close();
            fileStream?.Dispose();
        }

        /// <summary>
        /// Вспомогательный класс для хранения информации о поле.
        /// </summary>
        private class DBFField {
            public string Name { get; }
            public char Type { get; }
            public byte Length { get; }
            public byte DecimalCount { get; }

            public DBFField(string name, char type, byte length, byte decimalCount) {
                Name = name;
                Type = type;
                Length = length;
                DecimalCount = decimalCount;
            }
        }
    }
}

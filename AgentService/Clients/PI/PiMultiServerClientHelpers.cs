using System;
using System.Globalization;
using System.Text;

namespace Service.Clients.PI {
    public class PiMultiServerClientHelpers {

        public static DateTime GetDateTime(PointData pointData) {
            DateTime dateStamp;
            if (pointData.Value is DateTime)
                return (DateTime)pointData.Value;
            else if (DateTime.TryParse(ToSafeString(pointData.Value), out dateStamp))
                return dateStamp;

            throw new Exception();
        }

        public static decimal GetDecimal(PointData pointData) {
            return decimal.Parse(ToSafeString(pointData.Value).Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        public static string ToSafeString(object val) {
            return val == null ? string.Empty : val.ToString().Replace("\0", string.Empty);
        }

        public static string ToSafeStringWithEncoding(object val, string sourceEncoding = "Windows-1251") {
            if (val == null)
                return string.Empty;
            try {
                string decodedString = Encoding.GetEncoding(sourceEncoding).GetString(Encoding.Default.GetBytes(val.ToString()));
                return decodedString.Replace("\0", string.Empty);
            } catch {
                return val.ToString().Replace("\0", string.Empty);
            }
        }

    }
}

using System;
using System.Globalization;

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

    }
}

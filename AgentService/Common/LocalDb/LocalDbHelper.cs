using System;

namespace Service.LocalDb {
    public static class LocalDbHelper {
        public static string ToDbString(this DateTime dateTime) {
            if (dateTime == null) {
                return null;
            }
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}

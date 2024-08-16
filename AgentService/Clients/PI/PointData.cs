using System;

namespace Service.Clients.PI {
    public class PointData {
        public PointData(string name, object value, DateTime dateTime)
        {
            Name = name;
            Value = value;
            DateTimeStamp = dateTime;
        }
        public string Name { get; set; }
        public object Value { get; set; }
        public DateTime DateTimeStamp { get; set; }
    }
}
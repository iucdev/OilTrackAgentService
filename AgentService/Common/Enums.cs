namespace Service.Enums {
    
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum DboType {
        Default,
        FireBird,
        Oracle,
        MySql,
        Dbf
    }
}

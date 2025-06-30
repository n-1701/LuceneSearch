namespace WebApp2.Data.Entities
{
    public class Provider
    {
        public int ProviderId { get; set; }          // PROVIDER_ID
        public int CountryId { get; set; }           // COUNTRY_ID
        public long HhEntityId { get; set; }                  // HH_ENTITY_ID (NUMBER(15))
        public int? HhProvTypeId { get; set; }                 // HH_PROV_TYPE_ID (NUMBER(6))
        public string HhProviderType { get; set; }             // HH_PROVIDER_TYPE (VARCHAR2(400 Byte))
        public string ProviderName { get; set; }               // PROVIDER_NAME (VARCHAR2(400 Byte))
        public string ProviderLocLatitude { get; set; }        // PROVIDER_LOC_LATITUDE (VARCHAR2(32 Byte))
        public string ProviderLocLongitude { get; set; }       // PROVIDER_LOC_LONGITUDE (VARCHAR2(32 Byte))
        public int? ProviderCpe { get; set; }                  // PROVIDER_CPE (NUMBER)
        public string NntProviderNetworkId { get; set; }       // NNT_PROVIDER_NETWORK_ID (CHAR(2 Byte))
        public string CignaProviderNetwork { get; set; }       // CIGNA_PROVIDER_NETWORK (VARCHAR2(64 Byte)) 
        public int? ProviderLocCityId { get; set; }            // PROVIDER_LOC_CITY_ID (NUMBER(4)) 
        public string ProviderAddress { get; set; }            // PROVIDER_ADDRESS (VARCHAR2(256 Byte))
        public int? SysDeleted { get; set; }                   // SYS_DELETED (NUMBER)
        public DateTime? SysInsertDate { get; set; }           // SYS_INSERT_DATE (DATE)
        public DateTime? SysRefreshDate { get; set; }          // SYS_REFRESH_DATE (DATE)
    }
}

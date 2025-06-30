namespace WebApp2.Data.Entities
{
    public class ProviderKeywordMapping
    {
        public int Id { get; set; }                  // ID
        public int KeywordId { get; set; }           // KEYWORD_ID
        public int ProviderId { get; set; }          // PROVIDER_ID
        public int CountryId { get; set; }           // COUNTRY_ID
    }
}
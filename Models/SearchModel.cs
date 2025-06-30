namespace WebApp2.Models;

public class SearchModel
{
    public string Keyword { get; set; }
    public string Speciality { get; set; }
    public string Country { get; set; }
    public string Region { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class SearchResult
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public double? Distance { get; set; }
}

public class LocationSuggestion
{
    public string? PlaceId { get; set; }
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
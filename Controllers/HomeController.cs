using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using WebApp2.Models;
using WebApp2.Services;

namespace WebApp2.Controllers;


public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILuceneHProviderService _lucene;
    private readonly string MapsApiKey;

    public HomeController(IHttpClientFactory httpClientFactory , ILuceneHProviderService lucene)
    {
        MapsApiKey = "AIzaSyCdsutJxWwcJKalSjW5hEW2Fska7q_O7M8";
        _httpClientFactory = httpClientFactory;
        _lucene = lucene;
    }

    public IActionResult Index()
    {
        
        ViewBag.GoogleMapsApiKey = MapsApiKey;
        ViewBag.Language = "en";
        ViewBag.Region = "uae";
        return View(new SearchModel());
       
    }


    [HttpGet]
    public async Task<IActionResult> GetCountrySuggestions(string query)
    {
       if (string.IsNullOrEmpty(query))
            return Json(Array.Empty<LocationSuggestion>());

        var client = _httpClientFactory.CreateClient();
        var apiKey = MapsApiKey;

        try
        {
            var response = await client.GetAsync(
                $"https://maps.googleapis.com/maps/api/place/autocomplete/json?" +
                $"input={Uri.EscapeDataString(query)}&" +
                $"types=(regions)&" +
                $"key={apiKey}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // Parse and return the suggestions
                return Json(ParsePlacesResponse(content));
            }
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine(ex);
        }

        return Json(Array.Empty<LocationSuggestion>());
    }

    [HttpGet]
    public async Task<IActionResult> GetRegionSuggestions(string  query, string country)
    {
        if (string.IsNullOrEmpty(query))
            return Json(Array.Empty<LocationSuggestion>());

        var client = _httpClientFactory.CreateClient();
        var apiKey = MapsApiKey;

        try
        {
            var response = await client.GetAsync(
                $"https://maps.googleapis.com/maps/api/place/autocomplete/json?" +
                $"input={Uri.EscapeDataString(query)}&" +
                $"types=geocode&" +
                $"components=country:ae&" +
                $"key={apiKey}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // Parse and return the suggestions
                return Json(ParsePlacesResponse(content));
            }
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine(ex);
        }

        return Json(Array.Empty<LocationSuggestion>());
    }

    [HttpGet]
    public async Task<IActionResult> GetPosition(string placeid)
    {
        var client = _httpClientFactory.CreateClient();
        var apiKey = MapsApiKey;

        var geocodeResponse = await client.GetAsync(
          $"https://maps.googleapis.com/maps/api/geocode/json?" +
          $"place_id={placeid}&" +
          $"key={apiKey}");

        var geocodeJsonResponse = await geocodeResponse.Content.ReadAsStringAsync();
        var geocodeDoc = JsonDocument.Parse(geocodeJsonResponse);

        if (geocodeDoc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
        {
            var location = results[0]
                .GetProperty("geometry")
                .GetProperty("location");

            var latitude = location.GetProperty("lat").GetDouble();
            var longitude = location.GetProperty("lng").GetDouble();
            Console.WriteLine($"Latitude: {latitude}, Longitude: {longitude}");
            return Json(new { latitude= latitude, longitude = longitude });
        }
        return Json(new { latitude =0, longitude =0 });

    }

    private List<LocationSuggestion> ParsePlacesResponse(string content)
    {
        var response = new List<LocationSuggestion>();
        var jsonDoc = JsonDocument.Parse(content);
        foreach (var prediction in jsonDoc.RootElement.GetProperty("predictions").EnumerateArray())
        {
            var location = new LocationSuggestion
            {
                PlaceId = prediction.GetProperty("place_id").GetString(),
                Description = prediction.GetProperty("description").GetString(),
                Latitude = 0,
                Longitude = 0

            };
            response.Add(location);
        }
        // Implement parsing logic for Google Places API response
        // Return list of LocationSuggestion objects
        return response;
    }

    [HttpGet]
    public async Task<IActionResult> GetLocationSuggestions(string query, string type)
    {

        return Json(new List<string>() { "Location", type, query });
        if (string.IsNullOrEmpty(query))
            return Json(Array.Empty<string>());

        var client = _httpClientFactory.CreateClient();
        try
        {
            // Replace with your actual API endpoint
            var response = await client.GetAsync($"https://api.example.com/locations?q={Uri.EscapeDataString(query)}&type={type}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var suggestions = JsonSerializer.Deserialize<List<string>>(content);
                return Json(suggestions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return Json(Array.Empty<string>());
    }

    [HttpGet]
    public async Task<IActionResult> GetKeywordSuggestions(string query)
    {
        var result = _lucene.Search(new LuceneSearchRequest() { Query = query, UseFuzzySearch = true , Latitude = 25.0, Longitude = 55.0  });
        var keywordsResult = (result.ExactKeywordMatches.Count == 0
                                ? result.ProviderMatches.Select(i => i.Provider.Name)
                                : result.ExactKeywordMatches)
                                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                                .Select(g => g.First())
                                .ToList();
        //  if (string.IsNullOrEmpty(query))
        return Json(keywordsResult);

    }

    [HttpGet]
    public async Task<IActionResult> GetSpecialitySuggestions(string query)
    {
        //if (string.IsNullOrEmpty(query))
        //    return Json(Array.Empty<string>());

        return Json(new List<string>() { "Speciality", query });
    }

    [HttpPost]
    public async Task<IActionResult> Search(SearchModel model)
    {
        var result = _lucene.Search(new LuceneSearchRequest() { Query = model.Keyword, UseFuzzySearch = true, Latitude = model.Latitude, Longitude = model.Longitude });
        var searchResults = result.ProviderMatches.Select(i => new SearchResult
        {
            Id = i.Provider.Id,
            Title = i.Provider.Name,
            Description = $"Distance: {i.DistanceKm} Keyword : {string.Join(", ",i.Provider.Keywords)}"
        }).ToList();
        return PartialView("_SearchResults", searchResults);
    }
}
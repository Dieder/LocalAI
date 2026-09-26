using System.Globalization;
using System.Net.Http.Json;

namespace PhotoIt.Core.Services
{
    public class NominatimClient
    {
        private readonly HttpClient _http;

        public NominatimClient(HttpClient http)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("PhotoIt/1.0 (dieder.timmermaan@gmail.com)");
        }

        public async Task<GeoPoint?> ResolvePlaceAsync(string query, CancellationToken ct = default)
        {
            var url = $"search?q={Uri.EscapeDataString(query)}" +
                       $"&format=jsonv2" +
                       $"&featureType=settlement" +
                       $"&limit=1" +
                       $"&polygon_svg=1" +
                       $"&addressdetails=1";

            var results = await _http.GetFromJsonAsync<List<NominatimResult>>(url, ct);

            var match = results?.FirstOrDefault();
            
            if (match is null) return null;

            return new GeoPoint
            {                
                Name = match.DisplayName,               
                Country = match.Address?.Country,
                Svg = match.SvgImage,
                Latitude = double.Parse(match.Lat, CultureInfo.InvariantCulture),
                Longitude = double.Parse(match.Lon, CultureInfo.InvariantCulture)
            };
        }
    }
}

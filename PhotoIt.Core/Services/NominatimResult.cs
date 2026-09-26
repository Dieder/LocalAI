using System.Text.Json.Serialization;

namespace PhotoIt.Core.Services
{
    public class Address
    {
        [JsonPropertyName("country")]
        public string Country { get; set; } = "";

        [JsonPropertyName("city")]
        public string City { get; set; } = "";

        [JsonPropertyName("road")]
        public string Road { get; set; } = "";
    }
    public class NominatimResult
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("address")]
        public Address Address { get; set; } = new();

        [JsonPropertyName("svg")]
        public string SvgImage { get; set; } = "";

        [JsonPropertyName("lat")]
        public string Lat { get; set; } = "";

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = "";

        [JsonPropertyName("class")]
        public string Class { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = "";
    }
}

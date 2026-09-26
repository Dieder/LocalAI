namespace PhotoIt.Data
{
    public class RoutePoint
    {
        public string? Id { get; set; } = null!;
        public string? FoundName { get; set; }
        public string? Country { get; set; } = null!;
        public string Place { get; set; } = "";
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool HasGeoPoint()
        {
            return Latitude.HasValue && Longitude.HasValue;
        }

        public string? Svg { get;set; } = null!; 
        public void ResetGeo()
        {
            Id = null;
            FoundName = null;
            Latitude = null;
            Longitude = null;
            Svg = "";
        }
    }

}

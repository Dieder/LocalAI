namespace PhotoIt.Data
{
    public class TripGraph
    {
        public string UserId { get; set; } = string.Empty;
        public Guid AlbumId { get; set;  } = Guid.Empty;
        public List<RouteLeg> Legs { get; set; } = new List<RouteLeg>();
    }

}

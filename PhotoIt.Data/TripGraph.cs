namespace PhotoIt.Data
{
    public class TripGraph
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string UserId { get; set; } = string.Empty;
        public string AlbumId { get; set; } = string.Empty;
        public List<RouteLeg> Legs { get; set; } = new List<RouteLeg>();
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}

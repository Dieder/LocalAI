namespace PhotoIt.Data
{
    public class TripGraph
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
      
        public List<RouteLeg> Legs { get; set; } = new List<RouteLeg>();
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}

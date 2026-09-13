namespace PhotoIt.Data
{
    public class RouteLeg
    {
        public RoutePoint From { get; set; } = new();
        public RoutePoint To { get; set; } = new();
        public TransportType Transport { get; set; }
    }

    
  
}

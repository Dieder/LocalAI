namespace PhotoIt.Data
{
    public class RouteLeg
    {
        public RoutePoint From { get; set; } = new();
        public RoutePoint To { get; set; } = new();
        public TransportType Transport { get; set; }

        public bool IsEditing { get; set; } = false;

        public bool IsValid()
        {
            return From.IsValid() && To.IsValid();
        }
    }

    
  
}

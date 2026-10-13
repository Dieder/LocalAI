namespace PhotoIt.Data
{
    public sealed record PhotoSlot(
        int Col,          // 1-based
        int Row,          // 1-based
        PhotoType Type,
        int ColSpan = 1,
        int RowSpan = 1,
        string? PhotoId = null
    )
    {
        public string? PhotoId { get; set; } = PhotoId;
        public double PositionX { get; set; } = 50d;
        public double PositionY { get; set; } = 50d;
        public double Zoom { get; set; } = 1d;
    }
}

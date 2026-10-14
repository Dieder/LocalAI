namespace PhotoIt.Web.Components.Layout;

public sealed record LayoutDragStartEventArgs(string PhotoId, int slotCol, int slotRow,  bool IsTitlePage, int? FromPageNumber = null);

public sealed record LayoutDropEventArgs(int slotCol, int slotRow,  bool IsTitlePage);

public sealed record LayoutRemoveEventArgs(int slotCol, int slotRow, bool IsTitlePage);

public sealed record LayoutZoomChangedEventArgs(int slotCol, int slotRow, bool IsTitlePage, double Zoom);

public sealed record LayoutPositionChangedEventArgs(int slotCol, int slotRow, bool IsTitlePage, double PositionX, double PositionY);

namespace CleanCode.Web.Components.Layout;

public sealed record LayoutDragStartEventArgs(string PhotoId, int? FromSlotNumber, bool IsTitlePage, int? FromPageNumber = null);

public sealed record LayoutDropEventArgs(int SlotNumber, bool IsTitlePage);

public sealed record LayoutRemoveEventArgs(int SlotNumber, bool IsTitlePage);

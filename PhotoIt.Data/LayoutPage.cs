namespace PhotoIt.Data;

public sealed class LayoutPage
{
    public int Number { get; set; }
    public string TemplateId { get; set; } = LayoutTemplateIds.OneBig;
    public List<PhotoSlot> Photos { get; set; } = [];
}




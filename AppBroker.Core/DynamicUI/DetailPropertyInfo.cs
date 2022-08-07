namespace AppBroker.Core.DynamicUI;

public class DetailPropertyInfo : LayoutBasePropertyInfo
{
    public int TabInfoId { get; set; }
    public SpecialDetailType SpecialType { get; set; } = SpecialDetailType.None;

}


namespace AppBroker.Core.DynamicUI;

public partial class DetailPropertyInfo : LayoutBasePropertyInfo
{
    public int? TabInfoId { get; set; }
    public bool BlurryCard { get; set; }


    public string SpecialType { get; set; } = "none";

}

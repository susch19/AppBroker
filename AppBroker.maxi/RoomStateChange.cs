using AppBroker.Core.Models;

using dotVariant;

namespace AppBroker.maxi;

[Variant]
internal readonly partial struct RoomStateChange
{
    static partial void VariantOf(float temperature, bool isOpen, IHeaterConfigModel heaterConfigModel);
}

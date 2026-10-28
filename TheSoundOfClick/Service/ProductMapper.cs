using System.Text.Json;

namespace TheSoundOfClick;

public static class ProductMapper
{
    public static Int128 ConvertId(Guid id)
    {
        byte[] bytes = id.ToByteArray();

        return new Int128(
            BitConverter.ToUInt64(bytes, 8),
            BitConverter.ToUInt64(bytes, 0)
        );
    }
    public static Product MapProduct(
    Guid id,
    int stock,
    string name,
    decimal price,
    string color,
    string description,
    int netWeight,
    string category,
    string details)
    {
        using JsonDocument json = JsonDocument.Parse(details);

        Int128 productId = ConvertId(id);

        switch (category)
        {
            case "keycap":
                string theme = json.RootElement.GetProperty("Theme").GetString()!;
                int amount = json.RootElement.GetProperty("Amount").GetInt32();
                bool isBacklightCompatible = json.RootElement
                    .GetProperty("IsBacklightCompatible").GetBoolean();

                return new Keycap(
                    productId,
                    stock,
                    name,
                    price,
                    color,
                    theme,
                    isBacklightCompatible,
                    description,
                    amount,
                    netWeight
                );
            case "keyboard":
                string layout = json.RootElement.GetProperty("Layout").GetString()!;
                bool isWireless = json.RootElement.GetProperty("IsWireless").GetBoolean();
                string switchType = json.RootElement.GetProperty("SwitchType").GetString()!;
                string size = json.RootElement.GetProperty("Size").GetString()!;

                return new Keyboard(
                    productId,
                    stock,
                    name,
                    price,
                    color,
                    layout,
                    isWireless,
                    switchType,
                    description,
                    size,
                    netWeight,
                    netWeight
                );
            case "mousepad":
                int length = json.RootElement.GetProperty("Length").GetInt32();
                int width = json.RootElement.GetProperty("Width").GetInt32();
                int thickness = json.RootElement.GetProperty("Thickness").GetInt32();

                return new Mousepad(
                    productId,
                    stock,
                    name,
                    price,
                    color,
                    length,
                    width,
                    thickness,
                    description,
                    netWeight,
                    netWeight
                );
            default:
                throw new NotSupportedException(
                    $"Produktkategorien '{category}' understøttes ikke endnu.");
        }
    }
}

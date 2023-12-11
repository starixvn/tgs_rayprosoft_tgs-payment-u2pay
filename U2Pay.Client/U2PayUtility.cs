using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.U2Pay.Client;

public class U2PayUtility
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = {
        }
    };
}

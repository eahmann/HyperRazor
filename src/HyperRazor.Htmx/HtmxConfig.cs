using System.Text.Json;
using System.Text.Json.Serialization;

namespace HyperRazor.Htmx;

public sealed class HtmxConfig
{
    public bool SelfRequestsOnly { get; set; } = true;

    public bool HistoryRestoreAsHxRequest { get; set; } = false;

    public string? DefaultSwapStyle { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

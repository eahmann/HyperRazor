using System.Text.Json;
using System.Text.Json.Serialization;

namespace HyperRazor.Htmx;

public sealed class HtmxConfig
{
    public bool SelfRequestsOnly { get; set; } = true;

    public bool HistoryRestoreAsHxRequest { get; set; } = false;

    [JsonIgnore]
    public HtmxClientProfile ClientProfile { get; set; } = HtmxClientProfile.Htmx2Defaults;

    [JsonIgnore]
    public bool EnableHeadSupport { get; set; }

    [JsonIgnore]
    public bool EnableDiagnosticsInDevelopment { get; set; } = true;

    [JsonIgnore]
    public string AntiforgeryMetaName { get; set; } = "hrx-antiforgery";

    [JsonIgnore]
    public string AntiforgeryHeaderName { get; set; } = "RequestVerificationToken";

    public bool? AllowNestedOobSwaps { get; set; }

    public string? DefaultSwapStyle { get; set; }

    public IReadOnlyList<HtmxResponseHandlingRule>? ResponseHandling { get; set; }

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

public sealed class HtmxResponseHandlingRule
{
    public string Code { get; set; } = string.Empty;

    public bool Swap { get; set; }

    public bool? Error { get; set; }
}

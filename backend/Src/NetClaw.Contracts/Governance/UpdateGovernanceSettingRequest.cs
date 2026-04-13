using System.Text.Json.Serialization;

namespace NetClaw.Contracts.Governance;

public record UpdateGovernanceSettingRequest(
    [property: JsonPropertyName("enable_builtin_prompt_injection")] bool Enable_Builtin_Prompt_Injection,
    [property: JsonPropertyName("enable_custom_prompt_injection")] bool Enable_Custom_Prompt_Injection,
    [property: JsonPropertyName("enable_audit")] bool Enable_Audit,
    [property: JsonPropertyName("enable_metrics")] bool Enable_Metrics,
    [property: JsonPropertyName("enable_circuit_breaker")] bool Enable_Circuit_Breaker,
    [property: JsonPropertyName("builtin_detector_config")] string? Builtin_Detector_Config,
    [property: JsonPropertyName("is_active")] bool Is_Active);
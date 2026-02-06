using System.Text.Json.Serialization;

namespace ClaudeLogCli.Models;

public class SessionSummary
{
    public string SessionId { get; set; } = "";
    public string? GitBranch { get; set; }
    public string? FirstPrompt { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
    public int UserMessageCount { get; set; }
    public int AssistantMessageCount { get; set; }
    public int ToolUseCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string ProjectPath { get; set; } = "";
}

public class SessionMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime? Timestamp { get; set; }
    public string? Model { get; set; }
    public List<ToolUse> ToolUses { get; set; } = [];
}

public class ToolUse
{
    public string Name { get; set; } = "";
    public string? Input { get; set; }
}

// Raw JSONL line types
public class JsonlEntry
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("parentUuid")]
    public string? ParentUuid { get; set; }

    [JsonPropertyName("isSidechain")]
    public bool? IsSidechain { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("gitBranch")]
    public string? GitBranch { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; set; }

    [JsonPropertyName("message")]
    public JsonlMessage? Message { get; set; }
}

public class JsonlMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }
}

// JSON source generator for AOT compatibility
[JsonSerializable(typeof(JsonlEntry))]
[JsonSerializable(typeof(JsonlMessage))]
[JsonSerializable(typeof(List<SessionSummary>))]
[JsonSerializable(typeof(SessionSummary))]
[JsonSerializable(typeof(List<SessionMessage>))]
[JsonSerializable(typeof(SessionMessage))]
[JsonSerializable(typeof(System.Text.Json.JsonElement))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class ClaudeLogJsonContext : JsonSerializerContext
{
}

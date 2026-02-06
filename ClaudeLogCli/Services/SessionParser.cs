using System.Text.Json;
using ClaudeLogCli.Models;

namespace ClaudeLogCli.Services;

public static class SessionParser
{
    private static readonly string ClaudeProjectsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".claude", "projects");

    public static string GetProjectKey(string projectPath)
    {
        var fullPath = Path.GetFullPath(projectPath);
        return fullPath.Replace('/', '-').Replace('\\', '-');
    }

    public static string? FindProjectDir(string? projectPath = null)
    {
        if (projectPath != null)
        {
            var key = GetProjectKey(projectPath);
            var dir = Path.Combine(ClaudeProjectsDir, key);
            return Directory.Exists(dir) ? dir : null;
        }

        // Auto-detect: walk up from cwd looking for a match
        var cwd = Directory.GetCurrentDirectory();
        var candidate = cwd;
        while (candidate != null)
        {
            var key = GetProjectKey(candidate);
            var dir = Path.Combine(ClaudeProjectsDir, key);
            if (Directory.Exists(dir))
                return dir;
            candidate = Path.GetDirectoryName(candidate);
        }

        return null;
    }

    public static List<string> ListAllProjectDirs()
    {
        if (!Directory.Exists(ClaudeProjectsDir))
            return [];

        return Directory.GetDirectories(ClaudeProjectsDir)
            .Where(d => Directory.GetFiles(d, "*.jsonl").Length > 0)
            .ToList();
    }

    public static List<SessionSummary> ListSessions(string projectDir)
    {
        var sessions = new List<SessionSummary>();

        foreach (var file in Directory.GetFiles(projectDir, "*.jsonl"))
        {
            var summary = ParseSummary(file);
            if (summary != null)
                sessions.Add(summary);
        }

        return sessions.OrderBy(s => s.Created).ToList();
    }

    public static SessionSummary? ParseSummary(string jsonlPath)
    {
        var sessionId = Path.GetFileNameWithoutExtension(jsonlPath);
        var fileInfo = new FileInfo(jsonlPath);

        var summary = new SessionSummary
        {
            SessionId = sessionId,
            FileSizeBytes = fileInfo.Length,
            Modified = fileInfo.LastWriteTimeUtc
        };

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var entry = JsonSerializer.Deserialize(line, ClaudeLogJsonContext.Default.JsonlEntry);
                if (entry == null) continue;

                if (entry.Type == "user")
                {
                    summary.UserMessageCount++;

                    if (summary.GitBranch == null)
                        summary.GitBranch = entry.GitBranch;

                    if (summary.Created == null && entry.Timestamp != null)
                        summary.Created = ParseTimestamp(entry.Timestamp);

                    if (summary.ProjectPath == "" && entry.Cwd != null)
                        summary.ProjectPath = entry.Cwd;

                    if (summary.FirstPrompt == null && entry.Message?.Content != null)
                    {
                        summary.FirstPrompt = ExtractText(entry.Message.Content);
                    }
                }
                else if (entry.Message?.Role == "assistant")
                {
                    summary.AssistantMessageCount++;

                    if (entry.Message.Content != null)
                    {
                        summary.ToolUseCount += CountToolUses(entry.Message.Content);
                    }
                }
            }
            catch
            {
                // Skip malformed lines
            }
        }

        // Skip entries with no user messages (e.g. snapshot-only files)
        if (summary.UserMessageCount == 0 && summary.AssistantMessageCount == 0)
            return null;

        return summary;
    }

    public static List<SessionMessage> ParseMessages(string jsonlPath, bool userOnly = false)
    {
        var messages = new List<SessionMessage>();

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var entry = JsonSerializer.Deserialize(line, ClaudeLogJsonContext.Default.JsonlEntry);
                if (entry == null) continue;

                if (entry.Type == "user" && entry.Message?.Content != null)
                {
                    var text = ExtractText(entry.Message.Content);
                    if (!string.IsNullOrEmpty(text))
                    {
                        messages.Add(new SessionMessage
                        {
                            Role = "user",
                            Content = text,
                            Timestamp = ParseTimestamp(entry.Timestamp)
                        });
                    }
                }
                else if (!userOnly && entry.Message?.Role == "assistant" && entry.Message.Content != null)
                {
                    var text = ExtractText(entry.Message.Content);
                    var toolUses = ExtractToolUses(entry.Message.Content);

                    if (!string.IsNullOrEmpty(text) || toolUses.Count > 0)
                    {
                        messages.Add(new SessionMessage
                        {
                            Role = "assistant",
                            Content = text ?? "",
                            Model = entry.Message.Model,
                            Timestamp = ParseTimestamp(entry.Timestamp),
                            ToolUses = toolUses
                        });
                    }
                }
            }
            catch
            {
                // Skip malformed lines
            }
        }

        return messages;
    }

    public static List<SessionMessage> SearchMessages(string jsonlPath, string query)
    {
        var results = new List<SessionMessage>();
        var queryLower = query.ToLowerInvariant();

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var entry = JsonSerializer.Deserialize(line, ClaudeLogJsonContext.Default.JsonlEntry);
                if (entry?.Message?.Content == null) continue;

                var text = ExtractText(entry.Message.Content);
                if (text != null && text.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    var role = entry.Type == "user" ? "user" : (entry.Message.Role ?? "unknown");
                    results.Add(new SessionMessage
                    {
                        Role = role,
                        Content = text,
                        Timestamp = ParseTimestamp(entry.Timestamp)
                    });
                }
            }
            catch
            {
                // Skip malformed lines
            }
        }

        return results;
    }

    private static string? ExtractText(object? content)
    {
        if (content is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString();

            if (element.ValueKind == JsonValueKind.Array)
            {
                var texts = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("type", out var typeProp) &&
                        typeProp.GetString() == "text" &&
                        item.TryGetProperty("text", out var textProp))
                    {
                        var t = textProp.GetString();
                        if (!string.IsNullOrEmpty(t))
                            texts.Add(t);
                    }
                }
                return texts.Count > 0 ? string.Join("\n", texts) : null;
            }
        }

        return content?.ToString();
    }

    private static int CountToolUses(object? content)
    {
        if (content is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            int count = 0;
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("type", out var typeProp) &&
                    typeProp.GetString() == "tool_use")
                {
                    count++;
                }
            }
            return count;
        }
        return 0;
    }

    private static List<ToolUse> ExtractToolUses(object? content)
    {
        var toolUses = new List<ToolUse>();

        if (content is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("type", out var typeProp) &&
                    typeProp.GetString() == "tool_use" &&
                    item.TryGetProperty("name", out var nameProp))
                {
                    toolUses.Add(new ToolUse
                    {
                        Name = nameProp.GetString() ?? "unknown"
                    });
                }
            }
        }

        return toolUses;
    }

    private static DateTime? ParseTimestamp(string? timestamp)
    {
        if (timestamp == null) return null;
        return DateTime.TryParse(timestamp, out var dt) ? dt.ToLocalTime() : null;
    }
}

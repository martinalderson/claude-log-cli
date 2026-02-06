using ClaudeLogCli.Models;

namespace ClaudeLogCli.Output;

public class TableFormatter : IOutputFormatter
{
    public void WriteSessions(List<SessionSummary> sessions)
    {
        if (sessions.Count == 0)
        {
            Console.WriteLine("No sessions found.");
            return;
        }

        Console.WriteLine($"{"#",-4} {"Created",-18} {"Branch",-16} {"Msgs",-6} {"Tools",-7} {"Size",-8} {"First Prompt"}");
        Console.WriteLine(new string('-', 120));

        for (int i = 0; i < sessions.Count; i++)
        {
            var s = sessions[i];
            var created = s.Created?.ToString("yyyy-MM-dd HH:mm") ?? "-";
            var prompt = Truncate(s.FirstPrompt?.Replace("\n", " "), 50);
            var size = FormatBytes(s.FileSizeBytes);

            Console.WriteLine($"{i + 1,-4} {created,-18} {Truncate(s.GitBranch, 16),-16} {s.UserMessageCount,-6} {s.ToolUseCount,-7} {size,-8} {prompt}");
        }

        Console.WriteLine();
        Console.WriteLine($"{sessions.Count} sessions, {sessions.Sum(s => s.UserMessageCount)} total user messages");
    }

    public void WriteSession(SessionSummary? session)
    {
        if (session == null)
        {
            Console.WriteLine("Session not found.");
            return;
        }

        Console.WriteLine($"Session:    {session.SessionId}");
        Console.WriteLine($"Branch:     {session.GitBranch ?? "-"}");
        Console.WriteLine($"Project:    {session.ProjectPath}");
        Console.WriteLine($"Created:    {session.Created?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"}");
        Console.WriteLine($"Modified:   {session.Modified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"}");
        Console.WriteLine($"Messages:   {session.UserMessageCount} user, {session.AssistantMessageCount} assistant");
        Console.WriteLine($"Tool uses:  {session.ToolUseCount}");
        Console.WriteLine($"Size:       {FormatBytes(session.FileSizeBytes)}");
        Console.WriteLine($"Prompt:     {session.FirstPrompt ?? "-"}");
    }

    public void WriteMessages(List<SessionMessage> messages, string? sessionId = null)
    {
        if (messages.Count == 0)
        {
            Console.WriteLine("No messages found.");
            return;
        }

        foreach (var msg in messages)
        {
            var time = msg.Timestamp?.ToString("HH:mm:ss") ?? "??:??:??";
            var role = msg.Role == "user" ? "USER" : "ASST";
            var roleColor = msg.Role == "user" ? "\x1b[36m" : "\x1b[33m";
            var reset = "\x1b[0m";

            Console.WriteLine($"{roleColor}[{time}] {role}{reset}");

            if (msg.ToolUses.Count > 0)
            {
                var toolNames = string.Join(", ", msg.ToolUses.Select(t => t.Name));
                Console.WriteLine($"  Tools: {toolNames}");
            }

            if (!string.IsNullOrEmpty(msg.Content))
            {
                var lines = msg.Content.Split('\n');
                foreach (var line in lines.Take(20))
                {
                    Console.WriteLine($"  {line}");
                }
                if (lines.Length > 20)
                    Console.WriteLine($"  ... ({lines.Length - 20} more lines)");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"{messages.Count} messages");
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "-";
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.#} {sizes[order]}";
    }
}

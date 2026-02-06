using System.Text.Json;
using ClaudeLogCli.Models;

namespace ClaudeLogCli.Output;

public class JsonFormatter : IOutputFormatter
{
    public void WriteSessions(List<SessionSummary> sessions)
    {
        Console.WriteLine(JsonSerializer.Serialize(sessions, ClaudeLogJsonContext.Default.ListSessionSummary));
    }

    public void WriteSession(SessionSummary? session)
    {
        Console.WriteLine(JsonSerializer.Serialize(session, ClaudeLogJsonContext.Default.SessionSummary));
    }

    public void WriteMessages(List<SessionMessage> messages, string? sessionId = null)
    {
        Console.WriteLine(JsonSerializer.Serialize(messages, ClaudeLogJsonContext.Default.ListSessionMessage));
    }
}

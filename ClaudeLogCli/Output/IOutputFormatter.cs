using ClaudeLogCli.Models;

namespace ClaudeLogCli.Output;

public interface IOutputFormatter
{
    void WriteSessions(List<SessionSummary> sessions);
    void WriteSession(SessionSummary? session);
    void WriteMessages(List<SessionMessage> messages, string? sessionId = null);
}

public enum OutputFormat
{
    Table,
    Json
}

public static class OutputFormatterFactory
{
    public static IOutputFormatter Create(OutputFormat format) => format switch
    {
        OutputFormat.Table => new TableFormatter(),
        OutputFormat.Json => new JsonFormatter(),
        _ => new TableFormatter()
    };
}

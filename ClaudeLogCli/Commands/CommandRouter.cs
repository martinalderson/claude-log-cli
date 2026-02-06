using ClaudeLogCli.Models;
using ClaudeLogCli.Output;
using ClaudeLogCli.Services;

namespace ClaudeLogCli.Commands;

public static class CommandRouter
{
    public static int Route(string[] args)
    {
        var opts = ParseOptions(args);

        return (opts.Command, opts.SubCommand) switch
        {
            ("sessions", "list") or ("sessions", "") => SessionsList(opts),
            ("sessions", "show") => SessionsShow(opts),
            ("sessions", "messages") => SessionsMessages(opts),
            ("sessions", "prompts") => SessionsPrompts(opts),
            ("sessions", "search") => SessionsSearch(opts),
            ("sessions", "tools") => SessionsTools(opts),
            ("projects", "list") or ("projects", "") => ProjectsList(opts),
            _ => ShowError($"Unknown command: {opts.Command} {opts.SubCommand}")
        };
    }

    private static int SessionsList(CommandOptions opts)
    {
        var projectDir = SessionParser.FindProjectDir(opts.Path);
        if (projectDir == null)
            return ShowError("No Claude Code sessions found. Use --path to specify a project directory.");

        var sessions = SessionParser.ListSessions(projectDir);
        var formatter = OutputFormatterFactory.Create(opts.Format);
        formatter.WriteSessions(sessions);
        return 0;
    }

    private static int SessionsShow(CommandOptions opts)
    {
        var (jsonlPath, error) = ResolveSession(opts);
        if (jsonlPath == null) return ShowError(error!);

        var summary = SessionParser.ParseSummary(jsonlPath);
        var formatter = OutputFormatterFactory.Create(opts.Format);
        formatter.WriteSession(summary);
        return 0;
    }

    private static int SessionsMessages(CommandOptions opts)
    {
        var (jsonlPath, error) = ResolveSession(opts);
        if (jsonlPath == null) return ShowError(error!);

        var messages = SessionParser.ParseMessages(jsonlPath);
        var formatter = OutputFormatterFactory.Create(opts.Format);
        formatter.WriteMessages(messages);
        return 0;
    }

    private static int SessionsPrompts(CommandOptions opts)
    {
        var (jsonlPath, error) = ResolveSession(opts);
        if (jsonlPath == null) return ShowError(error!);

        var messages = SessionParser.ParseMessages(jsonlPath, userOnly: true);
        var formatter = OutputFormatterFactory.Create(opts.Format);
        formatter.WriteMessages(messages);
        return 0;
    }

    private static int SessionsSearch(CommandOptions opts)
    {
        if (string.IsNullOrEmpty(opts.Query))
            return ShowError("Usage: claude-log sessions search <query> [--path <project-path>]");

        var projectDir = SessionParser.FindProjectDir(opts.Path);
        if (projectDir == null)
            return ShowError("No Claude Code sessions found. Use --path to specify a project directory.");

        var sessions = SessionParser.ListSessions(projectDir);
        var formatter = OutputFormatterFactory.Create(opts.Format);
        var totalMatches = 0;

        foreach (var session in sessions)
        {
            var jsonlPath = Path.Combine(projectDir, session.SessionId + ".jsonl");
            var matches = SessionParser.SearchMessages(jsonlPath, opts.Query);
            if (matches.Count > 0)
            {
                Console.WriteLine($"\x1b[1m--- Session: {session.SessionId} ({session.GitBranch ?? "no branch"}, {session.Created:yyyy-MM-dd}) ---\x1b[0m");
                formatter.WriteMessages(matches, session.SessionId);
                totalMatches += matches.Count;
            }
        }

        if (totalMatches == 0)
            Console.WriteLine($"No matches for \"{opts.Query}\".");
        else
            Console.WriteLine($"\n{totalMatches} total matches across sessions.");

        return 0;
    }

    private static int SessionsTools(CommandOptions opts)
    {
        var projectDir = SessionParser.FindProjectDir(opts.Path);
        if (projectDir == null)
            return ShowError("No Claude Code sessions found. Use --path to specify a project directory.");

        var sessions = SessionParser.ListSessions(projectDir);

        // If a specific session is given, filter to it
        if (!string.IsNullOrEmpty(opts.Positional))
        {
            var match = ResolveSessionId(opts.Positional, sessions);
            if (match == null)
                return ShowError($"Session not found: {opts.Positional}");
            sessions = [match];
        }

        var toolCounts = new Dictionary<string, int>();

        foreach (var session in sessions)
        {
            var jsonlPath = Path.Combine(projectDir, session.SessionId + ".jsonl");
            var messages = SessionParser.ParseMessages(jsonlPath);
            foreach (var msg in messages)
            {
                foreach (var tool in msg.ToolUses)
                {
                    toolCounts.TryGetValue(tool.Name, out var count);
                    toolCounts[tool.Name] = count + 1;
                }
            }
        }

        if (toolCounts.Count == 0)
        {
            Console.WriteLine("No tool usage found.");
            return 0;
        }

        Console.WriteLine($"{"Tool",-30} {"Count",-8}");
        Console.WriteLine(new string('-', 38));

        foreach (var (tool, count) in toolCounts.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"{tool,-30} {count,-8}");
        }

        Console.WriteLine($"\n{toolCounts.Values.Sum()} total tool uses across {sessions.Count} session(s)");
        return 0;
    }

    private static int ProjectsList(CommandOptions opts)
    {
        var dirs = SessionParser.ListAllProjectDirs();
        if (dirs.Count == 0)
        {
            Console.WriteLine("No Claude Code projects found.");
            return 0;
        }

        Console.WriteLine($"{"Project Key",-60} {"Sessions",-10}");
        Console.WriteLine(new string('-', 70));

        foreach (var dir in dirs.OrderBy(d => d))
        {
            var name = Path.GetFileName(dir);
            var sessionCount = Directory.GetFiles(dir, "*.jsonl").Length;
            Console.WriteLine($"{name,-60} {sessionCount,-10}");
        }

        return 0;
    }

    private static (string? path, string? error) ResolveSession(CommandOptions opts)
    {
        var projectDir = SessionParser.FindProjectDir(opts.Path);
        if (projectDir == null)
            return (null, "No Claude Code sessions found. Use --path to specify a project directory.");

        if (string.IsNullOrEmpty(opts.Positional))
            return (null, $"Usage: claude-log sessions {opts.SubCommand} <session-id-or-number> [--path <project-path>]");

        var sessions = SessionParser.ListSessions(projectDir);
        var match = ResolveSessionId(opts.Positional, sessions);
        if (match == null)
            return (null, $"Session not found: {opts.Positional}. Use 'claude-log sessions list' to see available sessions.");

        var jsonlPath = Path.Combine(projectDir, match.SessionId + ".jsonl");
        return (jsonlPath, null);
    }

    private static SessionSummary? ResolveSessionId(string input, List<SessionSummary> sessions)
    {
        // Try as 1-based index
        if (int.TryParse(input, out var index) && index >= 1 && index <= sessions.Count)
            return sessions[index - 1];

        // Try as session ID prefix
        var matches = sessions.Where(s => s.SessionId.StartsWith(input, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 1)
            return matches[0];

        // Try exact match
        return sessions.FirstOrDefault(s => s.SessionId.Equals(input, StringComparison.OrdinalIgnoreCase));
    }

    private static int ShowError(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private static CommandOptions ParseOptions(string[] args)
    {
        var opts = new CommandOptions();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("--"))
            {
                var value = i + 1 < args.Length ? args[i + 1] : null;
                switch (arg)
                {
                    case "--format":
                        opts.Format = Enum.TryParse<OutputFormat>(value, true, out var f) ? f : OutputFormat.Table;
                        i++;
                        break;
                    case "--path":
                        opts.Path = value;
                        i++;
                        break;
                }
            }
            else if (string.IsNullOrEmpty(opts.Command))
            {
                opts.Command = arg;
            }
            else if (string.IsNullOrEmpty(opts.SubCommand))
            {
                opts.SubCommand = arg;
            }
            else if (string.IsNullOrEmpty(opts.Positional))
            {
                opts.Positional = arg;
            }
            else if (string.IsNullOrEmpty(opts.Query))
            {
                // For search, the positional becomes the query
                opts.Query = opts.Positional;
                opts.Positional = arg;
            }
        }

        // If positional was set and this is a search, move it to query
        if (opts.SubCommand == "search" && opts.Query == null && opts.Positional != null)
        {
            opts.Query = opts.Positional;
            opts.Positional = null;
        }

        return opts;
    }
}

public class CommandOptions
{
    public string Command { get; set; } = "";
    public string SubCommand { get; set; } = "";
    public string? Positional { get; set; }
    public string? Query { get; set; }
    public OutputFormat Format { get; set; } = OutputFormat.Table;
    public string? Path { get; set; }
}

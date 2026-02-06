using ClaudeLogCli.Commands;

if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
{
    PrintHelp();
    return 0;
}

try
{
    return CommandRouter.Route(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
        claude-log - Parse and analyze Claude Code chat logs

        Usage: claude-log <command> [options]

        Commands:
          sessions list                             List all sessions for current project
          sessions show <id|#>                      Show session details
          sessions messages <id|#>                  Show all messages in a session
          sessions prompts <id|#>                   Show only user prompts from a session
          sessions search <query>                   Search across all sessions
          sessions tools [<id|#>]                   Show tool usage stats

          projects list                             List all Claude Code projects

        Session Identifier:
          Sessions can be referenced by their full UUID, a UUID prefix,
          or by their 1-based index number from 'sessions list'.

        Options:
          --path <project-path>                     Project path (default: auto-detect from cwd)
          --format <table|json>                     Output format (default: table)
          --help                                    Show this help

        Examples:
          claude-log sessions list
          claude-log sessions list --path /Users/me/my-project
          claude-log sessions prompts 3
          claude-log sessions search "docker"
          claude-log sessions tools
          claude-log sessions messages 1 --format json
          claude-log projects list
        """);
}

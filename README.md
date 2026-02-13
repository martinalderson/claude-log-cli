# claude-log

A quick-and-dirty CLI tool for browsing and searching your local [Claude Code](https://docs.anthropic.com/en/docs/claude-code) chat logs. Read more about how this tool fits into a self-improving CLAUDE.md workflow on [my blog](https://martinalderson.com/posts/self-improving-claude-md-files/).

Built in C# / .NET 10 and compiled ahead-of-time (AOT) to a native binary. AOT means instant startup (~2ms, no JIT warmup), a single self-contained executable with no runtime to install, and a small binary size (~3MB).

## Usage

```
claude-log sessions list                 # List all sessions for the current project
claude-log sessions show <id|#index>     # Show session details
claude-log sessions messages <id|#index> # Show all messages in a session
claude-log sessions prompts <id|#index>  # Show only your prompts
claude-log sessions search <query>       # Search across all sessions
claude-log sessions tools [<id|#index>]  # Show tool usage stats
claude-log projects list                 # List all Claude Code projects
```

Sessions can be referenced by UUID, UUID prefix, or `#index` (1-based, most recent first).

### Options

```
--format json    Output as JSON instead of a table
--path <dir>     Override the project directory
--help           Show help
```

## Install

Grab a binary from [Releases](../../releases) - no dependencies required.

- **macOS (Apple Silicon):** `claude-log-osx-arm64`
- **Linux (x64):** `claude-log-linux-x64`

```sh
chmod +x claude-log-*
sudo mv claude-log-* /usr/local/bin/claude-log
```

## Building from source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (preview).

```sh
dotnet publish ClaudeLogCli/ClaudeLogCli.csproj -c Release
```

The native binary lands in `ClaudeLogCli/bin/Release/net10.0/<rid>/publish/claude-log`.

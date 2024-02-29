using Discord;

namespace DiscordBotTemplate.Log;

public class ConsoleLogger : Logger
{
    private static ConsoleLogger shared = new();

    public static ConsoleLogger Shared { get => shared; set => shared = value; }

    // Override Log method from ILogger, passing message to LogToConsoleAsync()
    public override Task Log(LogMessage message)
    {
        // Using Task.Run() in case there are any long running actions, to prevent blocking gateway
        _ = LogToConsoleAsync(message);
        return Task.CompletedTask;
    }

    private async Task LogToConsoleAsync(LogMessage message)
    {
        await Console.Out.WriteLineAsync($"guid:{_guid} : " + message);
    }
}
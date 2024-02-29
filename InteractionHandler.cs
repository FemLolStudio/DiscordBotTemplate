using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBotTemplate.Log;
using Microsoft.VisualBasic;
using System.Reflection;

namespace DiscordBotTemplate;

public class InteractionHandler
{
    private readonly DiscordShardedClient Client;
    private readonly InteractionService _commands;
    private readonly IServiceProvider _services;

    // Using constructor injection
    public InteractionHandler(DiscordShardedClient client, InteractionService commands, IServiceProvider services)
    {
        Client = client;
        _commands = commands;
        _services = services;
    }

    public async Task InitializeAsync()
    {
        // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        // Process the InteractionCreated payloads to execute Interactions commands
        Client.InteractionCreated += HandleInteraction;

        // Process the command execution results 
        _commands.SlashCommandExecuted += SlashCommandExecuted;
        _commands.ContextCommandExecuted += ContextCommandExecuted;
        _commands.ComponentCommandExecuted += ComponentCommandExecuted;
    }

    private Task ComponentCommandExecuted(ComponentCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
    {
        return Task.CompletedTask;
    }

    private Task ContextCommandExecuted(ContextCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
    {
        return Task.CompletedTask;
    }

    private Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
    {
        return Task.CompletedTask;
    }
    private Task HandleInteraction(SocketInteraction arg)
    {
        _ = HandleInteractionInside(arg);
        return Task.CompletedTask;
    }
    private async Task HandleInteractionInside(SocketInteraction arg)
    {
        _ = Program.OnlineStatus();
        
        EmbedTemplates.Dcbotimg ??= Client.CurrentUser.GetAvatarUrl();
        
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
            var ctx = new ShardedInteractionContext(Client, arg);
            
            if (arg is SocketSlashCommand slashCommand)
            {
                string commandName = slashCommand.Data.Name;
                string? subCommandName = slashCommand.Data.Options?.FirstOrDefault(o => o.Type == ApplicationCommandOptionType.SubCommand)?.Name;
                if (!string.IsNullOrEmpty(subCommandName))
                    commandName += ' ' + subCommandName;

                await ConsoleLogger.Shared.Log(new LogMessage(LogSeverity.Info, "Command", $"Command: {commandName}, User: {slashCommand.User.Username}", null));
            }
            await _commands.ExecuteCommandAsync(ctx, _services);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (arg.Type == InteractionType.ApplicationCommand)
                await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }
}

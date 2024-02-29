using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Discord.Commands;
using Discord;
using Microsoft.Extensions.Logging;
using DiscordBotTemplate.Log;

namespace DiscordBotTemplate;

public class Program
{
    private static int TotalShards = int.Parse(Environment.GetEnvironmentVariable("shards") ?? "1");
    private static int LoadedShards = 0;

    public static IConfigurationRoot Config { get; private set; } = null!;
    public static InteractionService Commands { get; set; } = null!;
    public static DiscordShardedClient Client { get; set; } = null!;
    public static bool Isafk { get; set; }
    public static DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;

    // Program entry point
    public static Task Main() => MainAsync();

    public static async Task MainAsync()
    {
        Config = new ConfigurationBuilder()
        // this will be used more later on
        .SetBasePath(AppContext.BaseDirectory)
        // I chose using YML files for my config data as I am familiar with them
        .AddJsonFile("config.json")
        .Build();

        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
        services
        // Add the configuration to the registered services
        .AddSingleton(Config)
        // Add the DiscordSocketClient, along with specifying the GatewayIntents and user caching
        .AddSingleton(x => new DiscordShardedClient(new DiscordSocketConfig
        {
            //GatewayIntents = GatewayIntents.None,
            //GatewayIntents = GatewayIntents.All,                              ,//
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.MessageContent,
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true,
            LogLevel = IsDebug() ? LogSeverity.Debug : LogSeverity.Info,
            TotalShards = TotalShards,
        }))
        // Adding console logging
        .AddTransient<ConsoleLogger>()
        // Used for slash commands and their registration with Discord
        .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordShardedClient>(), new InteractionServiceConfig() { DefaultRunMode = Discord.Interactions.RunMode.Async }))
        // Required to subscribe to the various client events used in conjunction with Interactions
        .AddSingleton<InteractionHandler>()
        // Required to subscribe to the various client events used in conjunction with Interactions
        //.AddSingleton<MessageHandler>()
        // Adding the prefix Command Service
        .AddSingleton(x => new CommandService(new CommandServiceConfig
        {
            LogLevel = IsDebug() ? LogSeverity.Debug : LogSeverity.Info,
            DefaultRunMode = Discord.Commands.RunMode.Async
        }))
        // Adding the prefix command handler
        //.AddSingleton<PrefixHandler>()
        )
        .ConfigureLogging(builder =>
        {
            builder.AddFilter((category, level) =>
                 level >= LogLevel.Warning);
        })
        .Build();

        await RunAsync(host);
    }

    public static async Task RunAsync(IHost host)
    {
        using IServiceScope serviceScope = host.Services.CreateScope();
        IServiceProvider provider = serviceScope.ServiceProvider;

        ConsoleLogger.Shared = provider.GetRequiredService<ConsoleLogger>();

        Commands = provider.GetRequiredService<InteractionService>();
        Client = provider.GetRequiredService<DiscordShardedClient>();
        var config = provider.GetRequiredService<IConfigurationRoot>();


        await provider.GetRequiredService<InteractionHandler>().InitializeAsync();
        //await provider.GetRequiredService<MessageHandler>().Initialize();


        // Subscribe to client log events
        Client.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);
        // Subscribe to slash command log events
        Commands.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);


        Client.ShardReady += ShardsReady;

        await Client.SetActivityAsync(new Game("/help"));

        await OnlineStatus();
        _ = AFKStatus();

        //TOKEN
        await Client.LoginAsync(TokenType.Bot, Config["token"]);

        int recomenedShards = await Client.GetRecommendedShardCountAsync();
        await ConsoleLogger.Shared.Log(new LogMessage(LogSeverity.Error, "Starting up", $"Recomened shards: {recomenedShards}"));


        if (Environment.GetEnvironmentVariable("shards") is null)
        {
            if (recomenedShards != TotalShards)
            {
                File.WriteAllText("sc.txt", recomenedShards.ToString());
                await ConsoleLogger.Shared.Log(new LogMessage(LogSeverity.Error, "Starting up", $"Shard count updated: {TotalShards} => {recomenedShards}"));
                return;
            }
        }


        await Client.StartAsync();



        await Task.Delay(-1);
    }
    public static async Task ShardsReady(DiscordSocketClient client)
    {
        // If running the bot with DEBUG flag, register all commands to guild specified in config
        // Id of the test guild can be provided from the Configuration object
        //await commands.RegisterCommandsToGuildAsync(ulong.Parse(testguild), true);
        // If not debug, register commands globally

        //await commands.RemoveModuleAsync(typeof(CharacterManagingModCommands));
        LoadedShards++;

        if (LoadedShards >= TotalShards)
        {
            await ConsoleLogger.Shared.Log(new LogMessage(LogSeverity.Info, "CommandRegister", $"Registering commands...", null));
            await Commands.RegisterCommandsGloballyAsync(true);
        }
    }
    public static async Task OnlineStatus()
    {
        LastActivity = DateTimeOffset.UtcNow;
        if (Isafk)
        {
            await Client.SetStatusAsync(UserStatus.Online);
            Isafk = false;
        }
    }

    public static async Task AFKStatus()
    {
        while (true)
        {
            if (LastActivity.AddMinutes(1) < DateTimeOffset.UtcNow && !Isafk)
            {
                await Client.SetStatusAsync(UserStatus.AFK);
                Isafk = true;
            }
            await Task.Delay(20000);
        }
    }

    public static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
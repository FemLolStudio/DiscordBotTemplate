using Discord.Interactions;

namespace DiscordBotTemplate;


public class HelpCommands : InteractionModuleBase<ShardedInteractionContext>
{
    [EnabledInDm(false)]
    [SlashCommand("help", "Some help")]
    public async Task Help()
    {
        var embed = EmbedTemplates.DefaultEmbed("Help", Context);

        embed.AddField("`/help`", "Some help.");


        await RespondAsync(embed: embed.Build());
    }
}
using Discord;
using Discord.Interactions;

namespace DiscordBotTemplate;

public static class EmbedTemplates
{
    public static string ResponseColor { get; set; } = "ffff00";
    public static string Dcbotimg { get; set; } = null!;

    public static EmbedBuilder DefaultEmbed(string title, ShardedInteractionContext? context = null)
    {
        EmbedBuilder embed = new EmbedBuilder()
        .WithColor(
            Convert.ToInt32(ResponseColor[..2], 16), // red component
            Convert.ToInt32(ResponseColor[2..4], 16), // green component
            Convert.ToInt32(ResponseColor[4..6], 16) // blue component
            )
        .WithCurrentTimestamp()
        .WithTitle(title);
        
        if(context != null)
            embed.WithAuthor(context.User);

        embed.WithThumbnailUrl(Dcbotimg);

        return embed;
    }
    public static  EmbedBuilder ErrorEmbed(string title, ShardedInteractionContext? context = null)
    {
        EmbedBuilder embed = new EmbedBuilder()
        .WithColor(255, 0, 0)
        .WithCurrentTimestamp()
        .WithTitle(title);

        if (context != null)
            embed.WithAuthor(context.User);

        embed.WithThumbnailUrl(Dcbotimg);

        return embed;
    }
}

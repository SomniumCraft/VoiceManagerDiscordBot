using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using VoiceLinkChatBot.Commands;

namespace VoiceLinkChatBot.Extensions;

public static class HostExtensions
{
    public static IHost AddDiscordCommands(this IHost host)
    {
        var discordClient = host.Services.GetService<DiscordClient>();
        var slash = discordClient!.UseSlashCommands(new SlashCommandsConfiguration { Services = host.Services });
        slash.RegisterCommands<ChannelLinkCommands>();
        
        slash.SlashCommandErrored += async (s, e) =>
        {
            if (e.Exception is SlashExecutionChecksFailedException ex)
            {
                foreach (var check in ex.FailedChecks)
                    if (check is SlashRequirePermissionsAttribute att)
                        await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder().WithContent("У вас нет прав для выполнения этой команды!"));
            }
        };
        
        return host;
    }
}
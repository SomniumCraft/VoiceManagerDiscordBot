using DSharpPlus;
using DSharpPlus.Commands;
using VoiceLinkChatBot.Commands;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Extensions;

public static class HostExtensions
{
    public static IHost AddDiscordCommands(this IHost host)
    {
        var discordClient = host.Services.GetService<DiscordClient>();
        var commandsExtension = discordClient.UseCommands();
        commandsExtension.AddCommands(typeof(ChannelLinkCommands));
        
        return host;
    }
}
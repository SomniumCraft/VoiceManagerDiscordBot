using DSharpPlus;
using DSharpPlus.Commands;
using VoiceLinkChatBot.Commands;

namespace VoiceLinkChatBot.Extensions;

public static class HostExtensions
{
    public static IHost AddDiscordCommands(this IHost host)
    {
        var discordClient = host.Services.GetService<DiscordClient>();
        var commandsExtension = discordClient.UseCommands();
        commandsExtension.AddCommands(typeof(ChannelCommands));
        
        return host;
    }
}
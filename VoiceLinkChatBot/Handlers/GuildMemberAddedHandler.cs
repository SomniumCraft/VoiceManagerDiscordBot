using DSharpPlus;
using DSharpPlus.EventArgs;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class GuildMemberAddedHandler : IDiscordEventHandler<GuildMemberAddedEventArgs>
{
    public async Task Handle(DiscordClient discordClient, GuildMemberAddedEventArgs args)
    {
        var channelsService = discordClient.ServiceProvider.GetRequiredService<ChannelsService>();

        var roleId = await channelsService.GetOnJoinRole(args.Guild.Id);
        if(roleId is null) return;
        var role = args.Guild.GetRole(roleId.Value);
        if(role is null) return;
        await args.Member.GrantRoleAsync(role);
    }
}
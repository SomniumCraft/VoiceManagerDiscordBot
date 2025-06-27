using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class GuildMemberAddedHandler(ILogger<GuildMemberAddedEventArgs> logger, ChannelsService channelsService) : IEventHandler<GuildMemberAddedEventArgs>
{
    public async Task HandleEventAsync(DiscordClient discordClient, GuildMemberAddedEventArgs args)
    {
        var member = args.Member;

        var roleId = await channelsService.GetOnJoinRole(args.Guild.Id);
        if (roleId is null) return;

        DiscordRole? role;
        try
        {
            role = await args.Guild.GetRoleAsync(roleId.Value);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get Role by Id: {RoleId} from Guild: {Guild}", roleId.Value, args.Guild);
            return;
        }

        try
        {
            await member.GrantRoleAsync(role);
            logger.LogInformation("Granted Role: {Role} to Member: {Member}", role, member);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to grant Role: {Role} to Member: {Member}", role, member);
        }
    }
}
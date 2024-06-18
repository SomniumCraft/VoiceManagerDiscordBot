using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class ThreadUpdatedHandler : IDiscordEventHandler<ThreadUpdatedEventArgs>
{
    public async Task Handle(DiscordClient discordClient, ThreadUpdatedEventArgs args)
    {
        var channelsService = discordClient.ServiceProvider.GetRequiredService<ChannelsService>();

        var lockOnArchive = await channelsService.AreChannelThreadsLockedOnArchive(args.Guild.Id, args.ThreadAfter.Id);
        if (lockOnArchive && args.ThreadAfter.ThreadMetadata.IsArchived)
        {
            await args.ThreadAfter.ModifyAsync(x =>
            {
                x.IsArchived = false;
                x.Locked = true;
                x.AutoArchiveDuration = DiscordAutoArchiveDuration.Hour;
            });
        }
    }
}
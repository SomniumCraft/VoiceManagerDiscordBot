using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class ThreadUpdatedHandler(ILogger<VoiceStateUpdatedHandler> logger, ChannelsService channelsService) : IEventHandler<ThreadUpdatedEventArgs>
{
    public async Task HandleEventAsync(DiscordClient discordClient, ThreadUpdatedEventArgs args)
    {
        var lockOnArchive = await channelsService.AreChannelThreadsLockedOnArchive(args.Guild.Id, args.ThreadAfter.Id);
        if (!lockOnArchive || !args.ThreadAfter.ThreadMetadata.IsArchived) return;

        try
        {
            await args.ThreadAfter.ModifyAsync(x =>
            {
                x.IsArchived = false;
                x.Locked = true;
                x.AutoArchiveDuration = DiscordAutoArchiveDuration.Hour;
            });
            logger.LogInformation("Locked Thread: {Thread}", args.ThreadAfter);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to lock Thread: {Thread}", args.ThreadAfter);
        }
    }
}
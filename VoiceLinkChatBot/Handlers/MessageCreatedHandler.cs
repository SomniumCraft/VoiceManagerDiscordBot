using DSharpPlus;
using DSharpPlus.EventArgs;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class MessageCreatedHandler(ILogger<VoiceStateUpdatedHandler> logger, ChannelsService channelsService) : IEventHandler<MessageCreatedEventArgs>
{
    public async Task HandleEventAsync(DiscordClient discordClient, MessageCreatedEventArgs args)
    {
        var autoThreadModel = await channelsService.GetAutoThreadChannel(args.Guild.Id, args.Channel.Id);
        if (autoThreadModel is null) return;

        try
        {
            var thread = await args.Message.CreateThreadAsync(autoThreadModel.Name, autoThreadModel.Duration);
            logger.LogInformation("Created Thread: {Thread} for Message: {Message}", thread, args.Message);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create thread for Message: {Message}", args.Message);
        }
    }
}
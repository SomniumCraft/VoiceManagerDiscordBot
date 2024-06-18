using DSharpPlus;
using DSharpPlus.EventArgs;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class MessageCreatedHandler : IDiscordEventHandler<MessageCreatedEventArgs>
{
    public async Task Handle(DiscordClient discordClient, MessageCreatedEventArgs args)
    {
        var channelsService = discordClient.ServiceProvider.GetRequiredService<ChannelsService>();
        
        var autoThreadModel = await channelsService.GetAutoThreadChannel(args.Guild.Id, args.Channel.Id);
        if (autoThreadModel != null)
            await args.Message.CreateThreadAsync(autoThreadModel.Name, autoThreadModel.Duration);
    }
}
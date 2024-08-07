using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class VoiceStateUpdatedHandler : IDiscordEventHandler<VoiceStateUpdatedEventArgs>
{
    public async Task Handle(DiscordClient discordClient, VoiceStateUpdatedEventArgs args)
    {
        if (args.After?.Channel?.Id == args.Before?.Channel?.Id) return;
        if (args.User.IsBot) return;

        var member = await args.Guild.GetMemberAsync(args.User.Id);

        var channelsService = discordClient.ServiceProvider.GetRequiredService<ChannelsService>();

        var channelLinks = await channelsService.GetLinkedChannels(args.Guild.Id);

        if (args.Before?.Channel?.Id != args.After?.Channel?.Id && args.Before?.Channel is not null)
        {
            var beforeChannelLinks = channelLinks
                .Where(x => x.VoiceChannelId == args.Before.Channel.Id)
                .ToList();

            foreach (var beforeChannelLink in beforeChannelLinks)
            {
                var tc = await discordClient.GetChannelAsync(beforeChannelLink.TextChannelId);
                await tc.DeleteOverwriteAsync(member);
            }

            if (args.Before.Channel.Users.Count == 0)
            {
                foreach (var beforeChannelLink in beforeChannelLinks)
                {
                    var tc = await discordClient.GetChannelAsync(beforeChannelLink.TextChannelId);
                    _ = PurgeChannelAsync(args.Before.Channel, tc);
                }
            }

            if (args.Before.Channel.Type == DiscordChannelType.Voice)
            {
                var message = await args.Before.Channel.SendMessageAsync(
                    new DiscordMessageBuilder()
                        .WithContent(
                            $"{(string.IsNullOrEmpty(member.Nickname) ? args.User.GlobalName : member.Nickname)} вышел"
                        )
                );
                await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{args.User.Id}> вышел"));
            }
        }

        if (args.After?.Channel is not null)
        {
            var channelLink = channelLinks
                .Where(x => x.VoiceChannelId == args.After.Channel.Id)
                .ToList();

            foreach (var channelLinkModel in channelLink)
            {
                var tc = await discordClient.GetChannelAsync(channelLinkModel.TextChannelId);
                await tc.AddOverwriteAsync(member,
                    DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages);
            }

            if (args.After.Channel.Type == DiscordChannelType.Voice)
            {
                var message = await args.After.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(
                        $"{(string.IsNullOrEmpty(member.Nickname) ? args.User.GlobalName : member.Nickname)} зашёл"
                    )
                );
                await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{args.User.Id}> зашёл"));
            }
        }
    }

    private async Task PurgeChannelAsync(DiscordChannel voiceChannel, DiscordChannel textChannel)
    {
        await Task.Delay(5000);
        
        if (voiceChannel.Users.Count == 0)
        {
            var lastMessage = await textChannel.SendMessageAsync("Очищаю канал");
            var source = textChannel.GetMessagesBeforeAsync(lastMessage.Id, 100000)
                .Where(x => DateTimeOffset.Now - x.Timestamp < TimeSpan.FromDays(13));
            await textChannel.DeleteMessagesAsync(source);
            await textChannel.DeleteMessageAsync(lastMessage);
        }
    }
}
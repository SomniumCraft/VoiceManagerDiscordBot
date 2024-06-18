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
                await tc.DeleteOverwriteAsync(args.Before.Member);
            }

            var message = await args.Before.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent(
                    $"{(string.IsNullOrEmpty(args.Before.Member.Nickname) ? args.User.GlobalName : args.Before.Member.Nickname)} вышел"));
            await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{args.User.Id}> вышел"));
        }

        if (args.After?.Channel is not null)
        {
            var channelLink = channelLinks
                .Where(x => x.VoiceChannelId == args.After.Channel.Id)
                .ToList();

            foreach (var channelLinkModel in channelLink)
            {
                var tc = await discordClient.GetChannelAsync(channelLinkModel.TextChannelId);
                await tc.AddOverwriteAsync(args.After.Member,
                    DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages);
            }

            var message = await args.After.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent(
                    $"{(string.IsNullOrEmpty(args.After.Member.Nickname) ? args.User.GlobalName : args.After.Member.Nickname)} зашёл"));
            await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{args.User.Id}> зашёл"));
        }
    }
}
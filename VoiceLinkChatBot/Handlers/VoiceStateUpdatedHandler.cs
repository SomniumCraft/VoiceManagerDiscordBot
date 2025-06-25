using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using VoiceLinkChatBot.Models;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class VoiceStateUpdatedHandler : IDiscordEventHandler<VoiceStateUpdatedEventArgs>
{
    public async Task Handle(DiscordClient discordClient, VoiceStateUpdatedEventArgs args)
    {
        if (args.After?.ChannelId == args.Before?.ChannelId) return;

        var user = await args.GetUserAsync();
        if (user?.IsBot == true) return;

        var guild = await args.GetGuildAsync();
        if (guild is null) return;

        DiscordMember member;
        try
        {
            member = await guild.GetMemberAsync(args.UserId);
        }
        catch (NotFoundException)
        {
            //TODO: logging
            return;
        }

        var channelsService = discordClient.ServiceProvider.GetRequiredService<ChannelsService>();

        var channelLinks = await channelsService.GetLinkedChannels(guild.Id);

        var beforeChannel = args.Before is null ? null : await args.Before.GetChannelAsync();
        var afterChannel = args.After is null ? null : await args.After.GetChannelAsync();

        await HandlePreviousChannel(discordClient, args, beforeChannel, channelLinks, member);
        await HandleNewChannel(discordClient, args, afterChannel, channelLinks, member);
    }

    private static async Task HandleNewChannel(
        DiscordClient discordClient,
        VoiceStateUpdatedEventArgs args,
        DiscordChannel? afterChannel,
        List<ChannelLinkModel> channelLinks,
        DiscordMember member)
    {
        if (afterChannel is null) return;

        var channelLink = channelLinks
            .Where(x => x.VoiceChannelId == afterChannel.Id)
            .ToList();

        foreach (var channelLinkModel in channelLink)
        {
            var tc = await discordClient.GetChannelAsync(channelLinkModel.TextChannelId);
            //False positive detection of DSP0007, there are no multiple calls to AddOverwriteAsync on the same channel
            #pragma warning disable DSP0007
            await tc.AddOverwriteAsync(member, new DiscordPermissions([DiscordPermission.ViewChannel, DiscordPermission.SendMessages]));
            #pragma warning restore DSP0007
        }

        if (afterChannel.Type != DiscordChannelType.Voice) return;
        var message = await afterChannel.SendMessageAsync(new DiscordMessageBuilder()
            .WithContent(
                $"{(string.IsNullOrEmpty(member.Nickname) ? member.GlobalName : member.Nickname)} зашёл"
            )
        );
        await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{args.UserId}> зашёл"));
    }

    private static async Task HandlePreviousChannel(
        DiscordClient discordClient,
        VoiceStateUpdatedEventArgs args,
        DiscordChannel? beforeChannel,
        List<ChannelLinkModel> channelLinks,
        DiscordMember member)
    {
        if (args.Before?.ChannelId == args.After?.ChannelId || beforeChannel is null) return;

        var beforeChannelLinks = channelLinks
            .Where(x => x.VoiceChannelId == beforeChannel.Id)
            .ToList();

        foreach (var beforeChannelLink in beforeChannelLinks)
        {
            var tc = await discordClient.GetChannelAsync(beforeChannelLink.TextChannelId);
            await tc.DeleteOverwriteAsync(member);
        }

        if (beforeChannel.Users.Count == 0)
        {
            foreach (var beforeChannelLink in beforeChannelLinks)
            {
                var tc = await discordClient.GetChannelAsync(beforeChannelLink.TextChannelId);
                _ = PurgeChannelAsync(beforeChannel, tc);
            }
        }

        if (beforeChannel.Type != DiscordChannelType.Voice) return;

        var message = await beforeChannel.SendMessageAsync(
            new DiscordMessageBuilder()
                .WithContent(
                    $"{(string.IsNullOrEmpty(member.Nickname) ? member.GlobalName : member.Nickname)} вышел"
                )
        );
        await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{args.UserId}> вышел"));
    }

    private static async Task PurgeChannelAsync(DiscordChannel voiceChannel, DiscordChannel textChannel)
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
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using VoiceLinkChatBot.Models;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class VoiceStateUpdatedHandler(ILogger<VoiceStateUpdatedHandler> logger, ChannelsService channelsService) : IEventHandler<VoiceStateUpdatedEventArgs>
{
    public async Task HandleEventAsync(DiscordClient discordClient, VoiceStateUpdatedEventArgs args)
    {
        if (args.After?.ChannelId == args.Before?.ChannelId) return;

        DiscordGuild? guild;
        try
        {
            guild = await args.GetGuildAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to load Guild by Id: {UserId}", args.GuildId);
            return;
        }
        if (guild is null) return;

        DiscordMember? member;
        try
        {
            member = await guild.GetMemberAsync(args.UserId);
        }
        catch (Exception e)
        {
            logger.LogError(e,"Member of Guild: {GuildId} with UserId: {UserId} not found", guild.Id, args.UserId);
            return;
        }
        if (member.IsBot) return;

        var channelLinks = await channelsService.GetLinkedChannels(guild.Id);

        DiscordChannel? beforeChannel;
        DiscordChannel? afterChannel;
        try
        {
            beforeChannel = args.Before is null ? null : await args.Before.GetChannelAsync();
            afterChannel = args.After is null ? null : await args.After.GetChannelAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to load channels");
            return;
        }

        await HandlePreviousChannel(discordClient, args, guild.Id, beforeChannel, channelLinks, member);
        await HandleNewChannel(discordClient, args, guild.Id, afterChannel, channelLinks, member);
    }

    private async Task HandleNewChannel(
        DiscordClient discordClient,
        VoiceStateUpdatedEventArgs args,
        ulong guildId,
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
            DiscordChannel? tc;
            try
            {
                tc = await discordClient.GetChannelAsync(channelLinkModel.TextChannelId);
            }
            catch (NotFoundException)
            {
                await HandleNotFoundChannel(guildId, channelLinkModel);
                continue;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to get Channel by Id: {ChannelId}", channelLinkModel.TextChannelId);
                continue;
            }

            try
            {
                //False positive detection of DSP0007, there are no multiple calls to AddOverwriteAsync on the same channel
#pragma warning disable DSP0007
                await tc.AddOverwriteAsync(member, new DiscordPermissions([DiscordPermission.ViewChannel, DiscordPermission.SendMessages]));
#pragma warning restore DSP0007
                logger.LogInformation("Added Member: {Member} to Channel: {Channel}", member, tc);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to add overwrites to Member: {Member}", member);
            }
        }

        if (afterChannel.Type != DiscordChannelType.Voice) return;

        try
        {
            var message = await afterChannel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent(
                    $"{(string.IsNullOrEmpty(member.Nickname) ? member.GlobalName : member.Nickname)} зашёл"
                )
            );
            logger.LogInformation("Sent Message: {Message} to Channel: {Channel}", message, afterChannel);

            await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{args.UserId}> зашёл"));
            logger.LogInformation("Modified Message: {Message} in Channel: {Channel}", member, afterChannel);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send or modify message");
        }
    }

    private async Task HandlePreviousChannel(
        DiscordClient discordClient,
        VoiceStateUpdatedEventArgs args,
        ulong guildId,
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
            DiscordChannel? tc;
            try
            {
                tc = await discordClient.GetChannelAsync(beforeChannelLink.TextChannelId);
            }
            catch (NotFoundException)
            {
                await HandleNotFoundChannel(guildId, beforeChannelLink);
                continue;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to get Channel by Id: {ChannelId}", beforeChannelLink.TextChannelId);
                continue;
            }

            await tc.DeleteOverwriteAsync(member);
            logger.LogInformation("Deleted overwrite for Member: {Member} for Channel: {Channel}", member, tc);
        }

        if (beforeChannel.Users.Count == 0)
        {
            foreach (var beforeChannelLink in beforeChannelLinks)
            {
                DiscordChannel? tc;
                try
                {
                    tc = await discordClient.GetChannelAsync(beforeChannelLink.TextChannelId);
                }
                catch (NotFoundException)
                {
                    await HandleNotFoundChannel(guildId, beforeChannelLink);
                    continue;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to get Channel by Id: {ChannelId}", beforeChannelLink.TextChannelId);
                    continue;
                }
                _ = PurgeChannelAsync(beforeChannel, tc);
            }
        }

        if (beforeChannel.Type != DiscordChannelType.Voice) return;

        try
        {
            var message = await beforeChannel.SendMessageAsync(
                new DiscordMessageBuilder()
                    .WithContent(
                        $"{(string.IsNullOrEmpty(member.Nickname) ? member.GlobalName : member.Nickname)} вышел"
                    )
            );
            await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{args.UserId}> вышел"));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send or modify message");
        }
    }

    private async Task PurgeChannelAsync(DiscordChannel voiceChannel, DiscordChannel textChannel)
    {
        await Task.Delay(5000);

        try
        {
            if (voiceChannel.Users.Count == 0)
            {
                var lastMessage = await textChannel.SendMessageAsync("Очищаю канал");
                var source = textChannel.GetMessagesBeforeAsync(lastMessage.Id, 100000)
                    .Where(x => DateTimeOffset.Now - x.Timestamp < TimeSpan.FromDays(13));
                await textChannel.DeleteMessagesAsync(source);
                await textChannel.DeleteMessageAsync(lastMessage);
                logger.LogInformation("Purged Channel: {Channel}", textChannel);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to purge channel");
        }
    }

    private async Task HandleNotFoundChannel(ulong guildId, ChannelLinkModel channelLink)
    {
        logger.LogInformation("Text channel not found Id: {Id}", channelLink.TextChannelId);
        await channelsService.RemoveLinkAsync(guildId, channelLink.TextChannelId, channelLink.VoiceChannelId);
        logger.LogInformation("Removed channel link GuildId: {GuildId} TextChannelId: {TextChannelId} VoiceChannelId: {VoiceChannelId}",
            guildId,
            channelLink.TextChannelId,
            channelLink.VoiceChannelId);
    }
}
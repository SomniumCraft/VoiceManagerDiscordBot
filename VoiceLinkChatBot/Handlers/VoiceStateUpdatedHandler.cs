using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using VoiceLinkChatBot.Models;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class VoiceStateUpdatedHandler(
    ILogger<VoiceStateUpdatedHandler> logger,
    ChannelsService channelsService,
    ChannelPurger channelPurger)
    : IEventHandler<VoiceStateUpdatedEventArgs>
{
    public async Task HandleEventAsync(DiscordClient discordClient, VoiceStateUpdatedEventArgs args)
    {
        var traceId = Guid.NewGuid();
        logger.LogInformation("Received VOICE_STATE_UPDATE event Before: " +
                              "{BeforeGuildId} " +
                              "{BeforeChannelId} " +
                              "{BeforeUserId} " +
                              "{BeforeSessionId} " +
                              "{BeforeIsServerDeafened} " +
                              "{BeforeIsServerMuted} " +
                              "{BeforeIsSelfDeafened} " +
                              "{BeforeIsSelfMuted} " +
                              "{BeforeIsSelfVideo} " +
                              "{BeforeIsSelfStream} " +
                              "{BeforeIsSuppressed} " +
                              "{BeforeRequestToSpeakTimestamp} " +
                              "After: " +
                              "{AfterGuildId} " +
                              "{AfterChannelId} " +
                              "{AfterUserId} " +
                              "{AfterSessionId} " +
                              "{AfterIsServerDeafened} " +
                              "{AfterIsServerMuted} " +
                              "{AfterIsSelfDeafened} " +
                              "{AfterIsSelfMuted} " +
                              "{AfterIsSelfVideo} " +
                              "{AfterIsSelfStream} " +
                              "{AfterIsSuppressed} " +
                              "{AfterRequestToSpeakTimestamp} " +
                              "TraceId: {TraceId}",
            args.Before?.GuildId,
            args.Before?.ChannelId,
            args.Before?.UserId,
            args.Before?.SessionId,
            args.Before?.IsServerDeafened,
            args.Before?.IsServerMuted,
            args.Before?.IsSelfDeafened,
            args.Before?.IsSelfMuted,
            args.Before?.IsSelfVideo,
            args.Before?.IsSelfStream,
            args.Before?.IsSuppressed,
            args.Before?.RequestToSpeakTimestamp,
            args.After?.GuildId,
            args.After?.ChannelId,
            args.After?.UserId,
            args.After?.SessionId,
            args.After?.IsServerDeafened,
            args.After?.IsServerMuted,
            args.After?.IsSelfDeafened,
            args.After?.IsSelfMuted,
            args.After?.IsSelfVideo,
            args.After?.IsSelfStream,
            args.After?.IsSuppressed,
            args.After?.RequestToSpeakTimestamp,
            traceId);

        if (args.After?.ChannelId == args.Before?.ChannelId)
        {
            logger.LogInformation("Before channel == After channel, Before channel Id: {BeforeChannelId}, After channel Id: {AfterChannelId} TraceId: {TraceId}", args.Before?.ChannelId, args.After?.ChannelId, traceId);
            return;
        }

        DiscordGuild? guild;
        try
        {
            guild = await args.GetGuildAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to load Guild by Id: {GuildId} TraceId: {TraceId}", args.GuildId, traceId);
            return;
        }
        if (guild is null)
        {
            logger.LogWarning("Guild is NULL, TraceId: {TraceId}", traceId);
            try
            {
                guild = await discordClient.GetGuildAsync(args.Before?.GuildId ?? args.After?.GuildId ?? throw new NullReferenceException());
            }
            catch (Exception e)
            {
                logger.LogError("Guild is NULL, TraceId: {TraceId}", traceId);
                return;
            }
        }

        DiscordMember? member;
        try
        {
            member = await guild.GetMemberAsync(args.UserId);
        }
        catch (Exception e)
        {
            logger.LogError(e,"Member of Guild: {GuildId} with UserId: {UserId} not found TraceId: {TraceId}", guild.Id, args.UserId, traceId);
            return;
        }

        if (member.IsBot)
        {
            logger.LogInformation("User is Bot TraceId: {TraceId}", traceId);
            return;
        }

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
            logger.LogError(e, "Failed to load channels, TraceId: {TraceId}", traceId);
            return;
        }

        await HandlePreviousChannel(discordClient, args, guild.Id, beforeChannel, afterChannel, channelLinks, member, traceId);
        await HandleNewChannel(discordClient, args, guild.Id, afterChannel, channelLinks, member, traceId);
    }

    private async Task HandleNewChannel(
        DiscordClient discordClient,
        VoiceStateUpdatedEventArgs args,
        ulong guildId,
        DiscordChannel? afterChannel,
        List<ChannelLinkModel> channelLinks,
        DiscordMember member,
        Guid traceId)
    {
        logger.LogInformation("Handling new channel After channel Id: {AfterChannelId} TraceId: {TraceId}", afterChannel?.Id, traceId);

        if (afterChannel is null)
        {
            logger.LogInformation("After channel is null, TraceId: {TraceId}", traceId);
            return;
        }

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
                await HandleNotFoundChannel(guildId, channelLinkModel, traceId);
                continue;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to get Channel by Id: {ChannelId} TraceId: {TraceId}", channelLinkModel.TextChannelId, traceId);
                continue;
            }

            try
            {
                //False positive detection of DSP0007, there are no multiple calls to AddOverwriteAsync on the same channel
#pragma warning disable DSP0007
                await tc.AddOverwriteAsync(member, new DiscordPermissions([DiscordPermission.ViewChannel, DiscordPermission.SendMessages]));
#pragma warning restore DSP0007
                logger.LogInformation("Added Member: {Member} to Channel: {Channel} TraceId: {TraceId}", member, tc, traceId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to add overwrites to Member: {Member} TraceId: {TraceId}", member, traceId);
            }
        }

        if (afterChannel.Type != DiscordChannelType.Voice)
        {
            logger.LogInformation("After channel is not voice, TraceId: {TraceId}", traceId);
            return;
        }

        try
        {
            var message = await afterChannel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent(
                    $"{(string.IsNullOrEmpty(member.Nickname) ? member.GlobalName : member.Nickname)} зашёл"
                )
            );
            logger.LogInformation("Sent Message: {Message} to Channel: {Channel} TraceId: {TraceId}", message, afterChannel, traceId);

            await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{args.UserId}> зашёл"));
            logger.LogInformation("Modified Message: {Message} in Channel: {Channel} TraceId: {TraceId}", member, afterChannel, traceId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send or modify message, TraceId: {TraceId}", traceId);
        }
    }

    private async Task HandlePreviousChannel(
        DiscordClient discordClient,
        VoiceStateUpdatedEventArgs args,
        ulong guildId,
        DiscordChannel? beforeChannel,
        DiscordChannel? afterChannel,
        List<ChannelLinkModel> channelLinks,
        DiscordMember member,
        Guid traceId)
    {
        if (beforeChannel is null)
        {
            logger.LogInformation("Before channel is null, TraceId: {TraceId}", traceId);
            return;
        }

        if (beforeChannel.Id == afterChannel?.Id)
        {
            logger.LogInformation("Before channel != After channel, Before channel Id: {BeforeChannelId}, After channel Id: {AfterChannelId} TraceId: {TraceId}", beforeChannel.Id, afterChannel?.Id, traceId);
            return;
        }

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
                await HandleNotFoundChannel(guildId, beforeChannelLink, traceId);
                continue;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to get Channel by Id: {ChannelId} TraceId {TraceId}", beforeChannelLink.TextChannelId, traceId);
                continue;
            }

            try
            {
                await tc.DeleteOverwriteAsync(member);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to delete Overwritefor Member: {Member} for Channel: {Channel} TraceId: {TraceId}", member, tc, traceId);
                return;
            }
            logger.LogInformation("Deleted overwrite for Member: {Member} for Channel: {Channel} TraceId: {TraceId}", member, tc, traceId);
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
                    await HandleNotFoundChannel(guildId, beforeChannelLink, traceId);
                    continue;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to get Channel by Id: {ChannelId} TraceId {TraceId}", beforeChannelLink.TextChannelId, traceId);
                    continue;
                }
                _ = channelPurger.PurgeChannelAsync(beforeChannel, tc);
            }
        }

        if (beforeChannel.Type != DiscordChannelType.Voice)
        {
            logger.LogInformation("Before channel is not voice, TraceId: {TraceId}", traceId);
            return;
        }

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
            logger.LogError(e, "Failed to send or modify message, TraceId: {TraceId}", traceId);
        }
    }

    private async Task HandleNotFoundChannel(ulong guildId, ChannelLinkModel channelLink, Guid traceId)
    {
        logger.LogInformation("Text channel not found Id: {Id} TraceId: {TraceId}", channelLink.TextChannelId, traceId);
        await channelsService.RemoveLinkAsync(guildId, channelLink.TextChannelId, channelLink.VoiceChannelId);
        logger.LogInformation("Removed channel link GuildId: {GuildId} TextChannelId: {TextChannelId} VoiceChannelId: {VoiceChannelId} TraceId: {TraceId}",
            guildId,
            channelLink.TextChannelId,
            channelLink.VoiceChannelId,
            traceId);
    }
}
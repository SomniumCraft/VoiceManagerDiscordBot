using DSharpPlus;
using DSharpPlus.EventArgs;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Handlers;

public class GuildAuditLogCreatedHandler(
    ILogger<VoiceStateUpdatedHandler> logger,
    ChannelsService channelsService,
    ChannelPurger channelPurger)
    : IEventHandler<GuildAuditLogCreatedEventArgs>
{
    public Task HandleEventAsync(DiscordClient sender, GuildAuditLogCreatedEventArgs eventArgs)
    {
        logger.LogInformation("Received GUILD_AUDIT_LOG_ENTRY_CREATE GuildId: {GuildId} ActionType: {ActionType} UserResponsible: {UserResponsible} Reason: {Reason} ActionCategory: {ActionCategory}",
            eventArgs.Guild.Id,
            eventArgs.AuditLogEntry.ActionType.ToString("G"),
            eventArgs.AuditLogEntry.UserResponsible?.Id,
            eventArgs.AuditLogEntry.Reason,
            eventArgs.AuditLogEntry.ActionCategory.ToString("G"));
        return Task.CompletedTask;
    }
}
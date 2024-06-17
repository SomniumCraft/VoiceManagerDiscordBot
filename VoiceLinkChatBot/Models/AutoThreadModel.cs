using DSharpPlus.Entities;

namespace VoiceLinkChatBot.Models;

public record AutoThreadModel(ulong ChannelId, string Name, DiscordAutoArchiveDuration Duration, bool LockOnArchive);
namespace VoiceLinkChatBot.Settings;

public record DiscordSettings
{
    public const string SectionName = "Discord";

    public required string Token { get; init; }
}
using DSharpPlus.Entities;

namespace VoiceLinkChatBot.Services;

public class ChannelPurger(ILogger<ChannelsService> logger)
{
    public async Task PurgeChannelAsync(DiscordChannel voiceChannel, DiscordChannel textChannel)
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
}
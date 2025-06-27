using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
using VoiceLinkChatBot.Models;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Workers;

public class Worker(ILogger<Worker> logger, DiscordClient discordClient, IConfiguration configuration, ChannelsService channelsService)
    : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting discord bot");

        await SetupDatabaseAsync(cancellationToken);

        await discordClient.ConnectAsync();

        await base.StartAsync(cancellationToken);
    }

    private async Task SetupDatabaseAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            connection.Open();

            const string createTableString = "CREATE TABLE IF NOT EXISTS linked_channel (" +
                                             "guild_id TEXT NOT NULL, " +
                                             "voice_channel_id TEXT NOT NULL, " +
                                             "text_channel_id TEXT NOT NULL);" +
                                             "CREATE UNIQUE INDEX IF NOT EXISTS unique_index " +
                                             "ON linked_channel (" +
                                             "guild_id, " +
                                             "voice_channel_id, " +
                                             "text_channel_id);" +
                                             "CREATE TABLE IF NOT EXISTS auto_threads_channel (" +
                                             "guild_id TEXT NOT NULL, " +
                                             "channel_id TEXT NOT NULL," +
                                             "name TEXT NOT NULL," +
                                             "duration TEXT NOT NULL," +
                                             "lock_on_archive INTEGER);" +
                                             "CREATE UNIQUE INDEX IF NOT EXISTS unique_index " +
                                             "ON auto_threads_channel(" +
                                             "guild_id, " +
                                             "channel_id);" +
                                             "CREATE TABLE IF NOT EXISTS role_on_join (" +
                                             "guild_id," +
                                             "role_id);";
            var createTableCommand = new SqliteCommand(createTableString, connection);
            await createTableCommand.ExecuteReaderAsync(cancellationToken);
            logger.LogInformation("Database set up");
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to setup database");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var guildIds = await channelsService.GetGuilds();
            foreach (var guildId in guildIds)
            {
                var channelLinks = await channelsService.GetLinkedChannels(guildId);
                await SyncLinkedChannels(channelLinks);
            }

            await Task.Delay(10000, stoppingToken);
        }
    }

    private async Task SyncLinkedChannels(List<ChannelLinkModel> channelLinks)
    {
        foreach (var channelLink in channelLinks)
        {
            DiscordChannel? tc;
            try
            {
                tc = await discordClient.GetChannelAsync(channelLink.TextChannelId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to load text channel by Id: {Id}", channelLink.TextChannelId);
                continue;
            }

            DiscordChannel? vc;
            try
            {
                vc = await discordClient.GetChannelAsync(channelLink.VoiceChannelId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to load voice channel by Id: {Id}", channelLink.TextChannelId);
                continue;
            }

            var overwritesToDelete = tc.PermissionOverwrites
                .Where(e => e.Type == DiscordOverwriteType.Member && vc.Users.All(x => x.Id != e.Id))
                .ToList();

            var membersToOverride = vc.Users
                .Where(e => e.IsBot == false && tc.PermissionOverwrites.All(x => x.Id != e.Id))
                .ToList();

            if (overwritesToDelete.Count == 0 && membersToOverride.Count == 0) return;

            var modifiedOverwrites = tc.PermissionOverwrites.Except(overwritesToDelete).Select(DiscordOverwriteBuilder.From).ToList();
            modifiedOverwrites.AddRange(membersToOverride.Select(member =>
                new DiscordOverwriteBuilder(member).Allow([DiscordPermission.ViewChannel, DiscordPermission.SendMessages])));

            try
            {
                await tc.ModifyAsync(x => { x.PermissionOverwrites = modifiedOverwrites; });
                logger.LogInformation("Modified overwrites in Channel: {Channel}", tc);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to modify overwrites in Channel: {Channel}", tc);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await discordClient.DisconnectAsync();
        discordClient.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
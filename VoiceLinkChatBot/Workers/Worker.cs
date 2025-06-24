using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Workers;

public class Worker(ILogger<Worker> logger, DiscordClient discordClient, IConfiguration configuration, ChannelsService channelsService)
    : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting discord bot");

        await SetupDatabaseAsync(cancellationToken);
        logger.LogInformation("Database set up");
        
        await discordClient.ConnectAsync();

        await base.StartAsync(cancellationToken);
    }

    private async Task SetupDatabaseAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

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
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var guildIds = await channelsService.GetGuilds();
            foreach (var guildId in guildIds)
            {
                var channelLinks = await channelsService.GetLinkedChannels(guildId);
                
                foreach (var channelLink in channelLinks)
                {
                    var tc = await discordClient.GetChannelAsync(channelLink.TextChannelId);
                    var vc = await discordClient.GetChannelAsync(channelLink.VoiceChannelId);

                    var overwritesToDelete = tc.PermissionOverwrites
                        .Where(e => e.Type == DiscordOverwriteType.Member && vc.Users.All(x => x.Id != e.Id))
                        .ToList();

                    foreach (var overwrite in overwritesToDelete)
                        await tc.DeleteOverwriteAsync(await overwrite.GetMemberAsync());

                    var membersToOverride = vc.Users
                        .Where(e => e.IsBot == false && tc.PermissionOverwrites.All(x => x.Id != e.Id))
                        .ToList();

                    await tc.ModifyAsync(x =>
                    {
                        x.PermissionOverwrites = membersToOverride.Select(m => new DiscordOverwriteBuilder(m).Allow([DiscordPermission.ViewChannel, DiscordPermission.SendMessages]));
                    });
                    
                    foreach (var discordMember in vc.Users)
                    {
                        if(discordMember.IsBot) continue;

                        var firstOrDefault = tc.PermissionOverwrites
                            .FirstOrDefault(e => e.Id == discordMember.Id);
                        
                        if(firstOrDefault == null) continue;
                    }
                }
            }
            
            await Task.Delay(10000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await discordClient.DisconnectAsync();
        discordClient.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Data.Sqlite;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Workers;

public class Worker(ILogger<Worker> logger, DiscordClient discordClient, IConfiguration configuration, LinkedChannelsService channelsService)
    : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting discord bot");

        await SetupDatabaseAsync(cancellationToken);
        logger.LogInformation("Database set up");

        discordClient.VoiceStateUpdated += OnVoiceStateUpdate;
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
                                         "text_channel_id);";
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
                        .Where(e => e.Type == OverwriteType.Member && vc.Users.All(x => x.Id != e.Id))
                        .ToList();

                    foreach (var overwrite in overwritesToDelete)
                        await tc.DeleteOverwriteAsync(await overwrite.GetMemberAsync());

                    var overwritesToAdd = new List<DiscordOverwrite>();

                    var membersToOverride = vc.Users
                        .Where(e => e.IsBot == false && tc.PermissionOverwrites.All(x => x.Id != e.Id))
                        .ToList();

                    foreach (var member in membersToOverride)
                        await tc.AddOverwriteAsync(member, Permissions.AccessChannels | Permissions.SendMessages);
                    
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
        discordClient.VoiceStateUpdated -= OnVoiceStateUpdate;
        await discordClient.DisconnectAsync();
        discordClient.Dispose();
        await base.StopAsync(cancellationToken);
    }
    
    private async Task OnVoiceStateUpdate(DiscordClient client, VoiceStateUpdateEventArgs e)
    {
        if(e.After?.Channel?.Id == e.Before?.Channel?.Id) return;
        if(e.User.IsBot) return;
        
        var channelLinks = await channelsService.GetLinkedChannels(e.Guild.Id);
        
        if (e.Before?.Channel?.Id != e.After?.Channel?.Id && e.Before?.Channel is not null)
        {
            var beforeChannelLinks = channelLinks
                .Where(x => x.VoiceChannelId == e.Before.Channel.Id)
                .ToList();
            
            foreach (var beforeChannelLink in beforeChannelLinks)
            {
                var tc = await discordClient.GetChannelAsync(beforeChannelLink.TextChannelId);
                await tc.DeleteOverwriteAsync(e.Before.Member);
            }
            
            var message = await e.Before.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent($"{(string.IsNullOrEmpty(e.Before.Member.Nickname) ? e.User.GlobalName : e.Before.Member.Nickname)} вышел"));
            await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{e.User.Id}> вышел"));
        }
        
        if (e.After?.Channel is not null)
        {
           var channelLink = channelLinks
               .Where(x => x.VoiceChannelId == e.After.Channel.Id)
               .ToList();
           
           foreach (var channelLinkModel in channelLink)
           {
               var tc = await discordClient.GetChannelAsync(channelLinkModel.TextChannelId);
               await tc.AddOverwriteAsync(e.After.Member, Permissions.AccessChannels | Permissions.SendMessages);
           }
           
           var message = await e.After.Channel.SendMessageAsync(new DiscordMessageBuilder()
               .WithContent($"{(string.IsNullOrEmpty(e.After.Member.Nickname) ? e.User.GlobalName : e.After.Member.Nickname)} зашёл"));
           await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{e.User.Id}> зашёл"));
        }
    }
}
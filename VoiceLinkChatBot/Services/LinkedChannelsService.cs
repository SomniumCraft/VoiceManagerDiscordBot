using Microsoft.Data.Sqlite;
using VoiceLinkChatBot.Models;

namespace VoiceLinkChatBot.Services;

public class LinkedChannelsService(IConfiguration configuration)
{
    private readonly string? _connectionString = configuration.GetConnectionString("DefaultConnection");

    public async Task<List<ulong>> GetGuilds()
    {
        const string commandText = "SELECT DISTINCT guild_id FROM linked_channel";
        
        await using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = new SqliteCommand(commandText, connection);
        
        var dataReader = await command.ExecuteReaderAsync();

        var list = new List<ulong>();
        
        while (await dataReader.ReadAsync())
        {
            list.Add(ulong.Parse(dataReader.GetString(0)));
        }

        return list;
    }
    
    public async Task<List<ChannelLinkModel>> GetLinkedChannels(ulong guildId)
    {
        const string commandText = "SELECT * FROM linked_channel WHERE guild_id=@guildId";
        
        await using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = new SqliteCommand(commandText, connection);

        command.Parameters.AddWithValue("@guildId", guildId.ToString());
        
        var dataReader = await command.ExecuteReaderAsync();

        var list = new List<ChannelLinkModel>();
        
        while (await dataReader.ReadAsync())
        {
            var vcId = ulong.Parse(dataReader.GetString(1));
            var tcId = ulong.Parse(dataReader.GetString(2));
            list.Add(new ChannelLinkModel(vcId, tcId));
        }

        return list;
    }

    public async Task AddLinkAsync(ulong guildId, ulong tcId, ulong vcId)
    {
        const string commandText = "INSERT OR IGNORE INTO linked_channel (guild_id, voice_channel_id, text_channel_id) VALUES(@guildId, @vcId, @tcId);";
        
        await using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = new SqliteCommand(commandText, connection);
        
        command.Parameters.AddWithValue("@guildId", guildId.ToString());
        command.Parameters.AddWithValue("@vcId", vcId.ToString());
        command.Parameters.AddWithValue("@tcId", tcId.ToString());
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveLinkAsync(ulong guildId, ulong tcId, ulong vcId)
    {
        const string commandText = "DELETE FROM linked_channel WHERE guild_id=@guildId AND voice_channel_id=@vcId AND text_channel_id=@tcId;";
        
        await using var connection = new SqliteConnection(_connectionString);

        connection.Open();
        var command = new SqliteCommand(commandText, connection);
        
        command.Parameters.AddWithValue("@guildId", guildId.ToString());
        command.Parameters.AddWithValue("@vcId", vcId.ToString());
        command.Parameters.AddWithValue("@tcId", tcId.ToString());

        await command.ExecuteNonQueryAsync();
    }
}
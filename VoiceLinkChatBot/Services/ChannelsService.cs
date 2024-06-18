using System.Data;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
using VoiceLinkChatBot.Models;

namespace VoiceLinkChatBot.Services;

public class ChannelsService(IConfiguration configuration)
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
        const string commandText =
            "INSERT OR IGNORE INTO linked_channel (guild_id, voice_channel_id, text_channel_id) VALUES(@guildId, @vcId, @tcId);";

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
        const string commandText =
            "DELETE FROM linked_channel WHERE guild_id=@guildId AND voice_channel_id=@vcId AND text_channel_id=@tcId;";

        await using var connection = new SqliteConnection(_connectionString);

        connection.Open();
        var command = new SqliteCommand(commandText, connection);

        command.Parameters.AddWithValue("@guildId", guildId.ToString());
        command.Parameters.AddWithValue("@vcId", vcId.ToString());
        command.Parameters.AddWithValue("@tcId", tcId.ToString());

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<AutoThreadModel>> GetAutoThreadChannels(ulong guildId)
    {
        const string commandText = "SELECT * FROM auto_threads_channel WHERE guild_id=@guildId";

        await using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = new SqliteCommand(commandText, connection);

        command.Parameters.AddWithValue("@guildId", guildId.ToString());

        var dataReader = await command.ExecuteReaderAsync();

        var list = new List<AutoThreadModel>();

        while (await dataReader.ReadAsync())
        {
            var channelId = ulong.Parse(dataReader.GetString(1));
            var message = dataReader.GetString(2);
            Enum.TryParse(dataReader.GetString(3), out DiscordAutoArchiveDuration duration);
            var lockOnArchive = Convert.ToBoolean(dataReader.GetInt16(4));
            list.Add(new AutoThreadModel(channelId, message, duration, lockOnArchive));
        }

        return list;
    }

    public async Task<bool> AreChannelThreadsLockedOnArchive(ulong guildId, ulong channelId)
    {
        const string commandText =
            "SELECT `lock_on_archive` FROM auto_threads_channel WHERE guild_id=@guildId AND channel_id=@channelId";

        await using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = new SqliteCommand(commandText, connection);

        command.Parameters.AddWithValue("@guildId", guildId.ToString());
        command.Parameters.AddWithValue("@channelId", channelId.ToString());

        var dataReader = await command.ExecuteReaderAsync();
        return dataReader.HasRows && Convert.ToBoolean(dataReader.GetInt16(0));
    }

    public async Task<AutoThreadModel?> GetAutoThreadChannel(ulong guildId, ulong channelId)
    {
        const string commandText =
            "SELECT * FROM auto_threads_channel WHERE guild_id=@guildId AND channel_id=@channelId";

        await using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = new SqliteCommand(commandText, connection);

        command.Parameters.AddWithValue("@guildId", guildId.ToString());
        command.Parameters.AddWithValue("@channelId", channelId.ToString());

        var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);

        if (!await dataReader.ReadAsync()) return null;

        return new AutoThreadModel(
            ulong.Parse(dataReader.GetString(1)),
            dataReader.GetString(2),
            Enum.Parse<DiscordAutoArchiveDuration>(dataReader.GetString(3)),
            Convert.ToBoolean(dataReader.GetInt16(4))
        );
    }

    public async Task AddAutoThreadAsync(ulong guildId, ulong channelId, string name,
        DiscordAutoArchiveDuration duration, bool lockOnArchive)
    {
        const string commandText =
            "INSERT OR IGNORE INTO auto_threads_channel (guild_id, channel_id, name, duration, lock_on_archive) VALUES(@guildId, @channelId, @name, @duration, @lockOnArchive);";

        await using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = new SqliteCommand(commandText, connection);

        command.Parameters.AddWithValue("@guildId", guildId.ToString());
        command.Parameters.AddWithValue("@channelId", channelId.ToString());
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@duration", duration.ToString());
        command.Parameters.AddWithValue("@lockOnArchive", Convert.ToBoolean(lockOnArchive));

        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveAutoThreadAsync(ulong guildId, ulong channelId)
    {
        const string commandText =
            "DELETE FROM auto_threads_channel WHERE guild_id=@guildId AND channel_id=@channelId;";

        await using var connection = new SqliteConnection(_connectionString);

        connection.Open();
        var command = new SqliteCommand(commandText, connection);

        command.Parameters.AddWithValue("@guildId", guildId.ToString());
        command.Parameters.AddWithValue("@channelId", channelId.ToString());

        await command.ExecuteNonQueryAsync();
    }
}
using System.Data;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
using VoiceLinkChatBot.Models;

namespace VoiceLinkChatBot.Services;

public class ChannelsService(IConfiguration configuration, ILogger<ChannelsService> logger)
{
    private readonly string? _connectionString = configuration.GetConnectionString("DefaultConnection");

    public async Task<List<ulong>> GetGuilds()
    {
        const string commandText = "SELECT DISTINCT guild_id FROM linked_channel";

        try
        {
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
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to get guilds");
            return [];
        }
    }

    public async Task<List<ChannelLinkModel>> GetLinkedChannels(ulong guildId)
    {
        const string commandText = "SELECT * FROM linked_channel WHERE guild_id=@guildId";

        try
        {
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
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to get linked channels for GuildId: {GuildId}", guildId);
            return [];
        }
    }

    public async Task AddLinkAsync(ulong guildId, ulong tcId, ulong vcId)
    {
        const string commandText =
            "INSERT OR IGNORE INTO linked_channel (guild_id, voice_channel_id, text_channel_id) VALUES(@guildId, @vcId, @tcId);";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = new SqliteCommand(commandText, connection);

            command.Parameters.AddWithValue("@guildId", guildId.ToString());
            command.Parameters.AddWithValue("@vcId", vcId.ToString());
            command.Parameters.AddWithValue("@tcId", tcId.ToString());

            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Added link for GuildId: {GuildId} TextChannelId: {TcId} VoiceChannelId: {VcId}", guildId, tcId, vcId);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to add Link for GuildId: {GuildId} TextChannelId: {TcId} VoiceChannelId: {VcId}", guildId, tcId, vcId);
        }
    }

    public async Task RemoveLinkAsync(ulong guildId, ulong tcId, ulong vcId)
    {
        const string commandText =
            "DELETE FROM linked_channel WHERE guild_id=@guildId AND voice_channel_id=@vcId AND text_channel_id=@tcId;";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);

            connection.Open();
            var command = new SqliteCommand(commandText, connection);

            command.Parameters.AddWithValue("@guildId", guildId.ToString());
            command.Parameters.AddWithValue("@vcId", vcId.ToString());
            command.Parameters.AddWithValue("@tcId", tcId.ToString());

            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Removed link for GuildId: {GuildId} TextChannelId: {TcId} VoiceChannelId: {VcId}", guildId, tcId, vcId);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to remove Link for GuildId: {GuildId} TextChannelId: {TcId} VoiceChannelId: {VcId}", guildId, tcId, vcId);
        }
    }

    public async Task<List<AutoThreadModel>> GetAutoThreadChannels(ulong guildId)
    {
        const string commandText = "SELECT * FROM auto_threads_channel WHERE guild_id=@guildId";

        try
        {
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
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to get auto thread channels for GuildId: {GuildId}", guildId);
            return [];
        }
    }

    public async Task<bool> AreChannelThreadsLockedOnArchive(ulong guildId, ulong channelId)
    {
        const string commandText =
            "SELECT `lock_on_archive` FROM auto_threads_channel WHERE guild_id=@guildId AND channel_id=@channelId";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = new SqliteCommand(commandText, connection);

            command.Parameters.AddWithValue("@guildId", guildId.ToString());
            command.Parameters.AddWithValue("@channelId", channelId.ToString());

            var dataReader = await command.ExecuteReaderAsync();
            return dataReader.HasRows && Convert.ToBoolean(dataReader.GetInt16(0));
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to get auto thread lock on archive for ChannelId: {ChannelId} in GuildId: {GuildId}", channelId, guildId);
            return false;
        }
    }

    public async Task<AutoThreadModel?> GetAutoThreadChannel(ulong guildId, ulong channelId)
    {
        const string commandText =
            "SELECT * FROM auto_threads_channel WHERE guild_id=@guildId AND channel_id=@channelId";

        try
        {
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
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to get auto thread parameters for ChannelId: {ChannelId} in GuildId: {GuildId}", channelId, guildId);
            return null;
        }
    }

    public async Task AddAutoThreadAsync(ulong guildId, ulong channelId, string name,
        DiscordAutoArchiveDuration duration, bool lockOnArchive)
    {
        const string commandText =
            "INSERT OR IGNORE INTO auto_threads_channel (guild_id, channel_id, name, duration, lock_on_archive) VALUES(@guildId, @channelId, @name, @duration, @lockOnArchive);";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = new SqliteCommand(commandText, connection);

            command.Parameters.AddWithValue("@guildId", guildId.ToString());
            command.Parameters.AddWithValue("@channelId", channelId.ToString());
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@duration", duration.ToString());
            command.Parameters.AddWithValue("@lockOnArchive", Convert.ToBoolean(lockOnArchive));

            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Auto threads added to ChannelId: {ChannelId} in GuildId: {GuildId} with Name: {Name}, Duration: {Duration}, LockOnArchive: {LockOnArchive}", channelId, guildId, name, duration, lockOnArchive);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to add auto threads to ChannelId: {ChannelId} in GuildId: {GuildId} with Name: {Name}, Duration: {Duration}, LockOnArchive: {LockOnArchive}", channelId, guildId, name, duration, lockOnArchive);
        }
    }

    public async Task RemoveAutoThreadAsync(ulong guildId, ulong channelId)
    {
        const string commandText =
            "DELETE FROM auto_threads_channel WHERE guild_id=@guildId AND channel_id=@channelId;";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);

            connection.Open();
            var command = new SqliteCommand(commandText, connection);

            command.Parameters.AddWithValue("@guildId", guildId.ToString());
            command.Parameters.AddWithValue("@channelId", channelId.ToString());

            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Auto threads removed from ChannelId: {ChannelId} in GuildId: {GuildId}", channelId, guildId);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to remove auto threads from ChannelId: {ChannelId} in GuildId: {GuildId}", channelId, guildId);
        }
    }

    public async Task<ulong?> GetOnJoinRole(ulong guildId)
    {
        const string commandText = "SELECT * FROM role_on_join WHERE guild_id=@guildId";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);

            connection.Open();
            var command = new SqliteCommand(commandText, connection);

            command.Parameters.AddWithValue("@guildId", guildId.ToString());

            var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);

            if (!await dataReader.ReadAsync()) return null;

            return ulong.Parse(dataReader.GetString(1));
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to get role id on join for GuildId: {GuildId}", guildId);
            return null;
        }
    }

    public async Task AddRoleOnJoin(ulong guildId, ulong roleId)
    {
        const string commandText =
            "INSERT OR IGNORE INTO role_on_join (guild_id, role_id) VALUES(@guildId, @roleId);";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);

            connection.Open();
            var command = new SqliteCommand(commandText, connection);

            command.Parameters.AddWithValue("@guildId", guildId.ToString());
            command.Parameters.AddWithValue("@roleId", roleId.ToString());

            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Set RoleId: {RoleId} on join to GuildId: {GuildId}", roleId, guildId);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to add role id on join for GuildId: {GuildId}", guildId);
        }
    }
}
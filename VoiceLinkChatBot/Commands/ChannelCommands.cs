using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Commands;

[Command("channel"), AllowedProcessors(typeof(SlashCommandProcessor))]
[Description("Команды для управления привязками каналов")]
[RequirePermissions(DiscordPermission.ManageChannels, DiscordPermission.ManageRoles)]
public class ChannelCommands(ChannelsService service, ILogger<ChannelCommands> logger)
{
    [Command("link")]
    [Description("Связывает текстовый канал с голосовым")]
    public async ValueTask Link(
        CommandContext context,
        [Description("Текстовый канал, который вы хотите привязать")]
        DiscordChannel textChannel,
        [Description("Голосовой канал, к которому будет выполнена привязка")]
        DiscordChannel voiceChannel
    )
    {
        if (!await ValidateGuildAndChannels(context, textChannel, voiceChannel)) return;

        await service.AddLinkAsync(context.Guild!.Id, textChannel.Id, voiceChannel.Id);

        await RespondAsync(context, $"Ну типа привязал {textChannel.Name} к {voiceChannel.Name}");
    }

    [Command("unlink")]
    [Description("Отвязывает текстовый канал от голосового")]
    public async Task Unlink(
        CommandContext context,
        [Description("Текстовый канал, который вы хотите отвязать")]
        DiscordChannel textChannel,
        [Description("Голосовой канал, от которго будет отвязан")]
        DiscordChannel voiceChannel
    )
    {
        if (!await ValidateGuildAndChannels(context, textChannel, voiceChannel)) return;

        await service.RemoveLinkAsync(context.Guild!.Id, textChannel.Id, voiceChannel.Id);

        await RespondAsync(context, $"Ну типа отвязал {textChannel.Name} от {voiceChannel.Name}");
    }

    [Command("autothread")]
    [Description("В указанном канале автоматически создаются ветки")]
    public async ValueTask AutoThread(
        CommandContext context,
        [Description("Текстовый канал, который вы хотите привязать")]
        DiscordChannel channel,
        [Description("Название ветки")]
        string name,
        [Description("Срок жизни ветки")]
        DiscordAutoArchiveDuration duration,
        [Description("Закрывать ветку при архивировании?")]
        bool lockOnArchive
    )
    {
        await service.AddAutoThreadAsync(context.Guild!.Id, channel.Id, name, duration, lockOnArchive);

        await RespondAsync(context, $"Ну типа теперь ветки автоматом в {channel.Name}");
    }

    [Command("noautothread")]
    [Description("В указанном канале автоматически создаются ветки")]
    public async ValueTask NoAutoThread(
        CommandContext context,
        [Description("Текстовый канал, который вы хотите привязать")]
        DiscordChannel channel
    )
    {
        await service.RemoveAutoThreadAsync(context.Guild!.Id, channel.Id);

        await RespondAsync(context, $"Ну типа теперь ветки не автоматом в {channel.Name}");
    }

    private async Task<bool> ValidateGuildAndChannels(
        CommandContext context,
        DiscordChannel textChannel,
        DiscordChannel voiceChannel
    )
    {
        if (context.Guild is null)
        {
            await RespondAsync(context, "Хрен его знает, не получилось найти гильдию");
            return false;
        }

        if (textChannel.Type != DiscordChannelType.Text)
        {
            await RespondAsync(context, "Указанный textChannel не является текстовым каналом");
            return false;
        }

        if (voiceChannel.Type != DiscordChannelType.Voice)
        {
            await RespondAsync(context, "Указанный voiceChannel не является голосовым каналом");
            return false;
        }

        return true;
    }

    private async Task RespondAsync(CommandContext context, string content)
    {
        try
        {
            await context.RespondAsync(content);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create response");
        }
    }
}
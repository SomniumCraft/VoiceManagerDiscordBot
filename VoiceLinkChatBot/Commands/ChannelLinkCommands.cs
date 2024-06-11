using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Commands;

[Command("channel"), AllowedProcessors(typeof(SlashCommandProcessor))]
[Description("Команды для управления связи текстового и голосового канала")]
[RequirePermissions(DiscordPermissions.ManageChannels | DiscordPermissions.ManageRoles)]
public class ChannelLinkCommands(LinkedChannelsService service)
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
        if (!await ValidateChannels(context, textChannel, voiceChannel)) return;

        await service.AddLinkAsync(context.Guild.Id, textChannel.Id, voiceChannel.Id);

        await context.RespondAsync($"Ну типа привязал {textChannel.Name} к {voiceChannel.Name}");
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
        if (!await ValidateChannels(context, textChannel, voiceChannel)) return;

        await service.RemoveLinkAsync(context.Guild.Id, textChannel.Id, voiceChannel.Id);

        await context.RespondAsync($"Ну типа отвязал {textChannel.Name} от {voiceChannel.Name}");
    }

    private static async Task<bool> ValidateChannels(
        CommandContext context,
        DiscordChannel textChannel,
        DiscordChannel voiceChannel
    )
    {
        if (textChannel.Type != DiscordChannelType.Text)
        {
            await context.RespondAsync("Указанный textChannel не является текстовым каналом");
            return false;
        }

        if (voiceChannel.Type != DiscordChannelType.Voice)
        {
            await context.RespondAsync("Указанный voiceChannel не является голосовым каналом");
            return false;
        }

        return true;
    }
}
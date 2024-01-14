using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Commands;

[SlashCommandGroup("channel", "Команды для управления связи текстового и голосового канала")]
[SlashRequirePermissions(Permissions.ManageChannels | Permissions.ManageRoles)]
public class ChannelLinkCommands(LinkedChannelsService service) : ApplicationCommandModule
{
    [SlashCommand("link", "Связывает текстовый канал с голосовым")]
    public async Task Link(
        InteractionContext context,
        [Option("text_channel", "Текстовый канал, который вы хотите привязать")]
        DiscordChannel textChannel,
        [Option("voice_channel", "Голосовой канал, к которому будет выполнена привязка")]
        DiscordChannel voiceChannel
    )
    {
        if (!await ValidateChannels(context, textChannel, voiceChannel)) return;

        await service.AddLinkAsync(context.Guild.Id, textChannel.Id, voiceChannel.Id);

        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent(
                $"Ну типа привязал {textChannel.Name} к {voiceChannel.Name}"));
    }

    [SlashCommand("unlink", "Отвязывает текстовый канал от голосового")]
    public async Task Unlink(InteractionContext context,
        [Option("text_channel", "Текстовый канал, который вы хотите отвязать")]
        DiscordChannel textChannel,
        [Option("voice_channel", "Голосовой канал, от которго будет отвязан")]
        DiscordChannel voiceChannel
    )
    {
        if (!await ValidateChannels(context, textChannel, voiceChannel)) return;

        await service.RemoveLinkAsync(context.Guild.Id, textChannel.Id, voiceChannel.Id);

        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent(
                $"Ну типа отвязал {textChannel.Name} от {voiceChannel.Name}"));
    }

    private static async Task<bool> ValidateChannels(
        BaseContext context,
        DiscordChannel textChannel,
        DiscordChannel voiceChannel
    )
    {
        if (textChannel.Type != ChannelType.Text)
        {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(
                    "Указанный text_channel не является текстовым каналом"
                )
            );
            return false;
        }

        if (voiceChannel.Type != ChannelType.Voice)
        {
            await context.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(
                    "Указанный voice_channel не является голосовым каналом"
                )
            );
            return false;
        }

        return true;
    }
}
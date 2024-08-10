using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Commands;

[Command("autorole"), AllowedProcessors(typeof(SlashCommandProcessor))]
[Description("Команды для настройки автороли")]
[RequirePermissions(DiscordPermissions.ManageChannels | DiscordPermissions.ManageRoles)]
public class AutoRoleCommand(ChannelsService service)
{
    [Command("link")]
    public async ValueTask Link(
        CommandContext context,
        DiscordRole role
    )
    {
        if (context.Guild is null)
        {
            await context.RespondAsync($"Не удалось найти гильдию");
            return;
        }
        await service.AddRoleOnJoin(context.Guild.Id, role.Id);

        await context.RespondAsync($"Вот ета роль теперь тут дается всем при джоине");
    }
}
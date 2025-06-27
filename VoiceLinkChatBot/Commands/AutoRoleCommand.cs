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
[RequirePermissions(DiscordPermission.ManageChannels, DiscordPermission.ManageRoles)]
public class AutoRoleCommand(ChannelsService service, ILogger<AutoRoleCommand> logger)
{
    [Command("link")]
    public async ValueTask Link(
        CommandContext context,
        DiscordRole role
    )
    {
        if (context.Guild is null)
        {
            await RespondAsync(context, "Не удалось найти гильдию");
            return;
        }
        await service.AddRoleOnJoin(context.Guild.Id, role.Id);

        await RespondAsync(context, "Вот ета роль теперь тут дается всем при джоине");
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
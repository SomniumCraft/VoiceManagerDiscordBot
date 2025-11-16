using DSharpPlus.Commands;
using DSharpPlus.Extensions;
using VoiceLinkChatBot.Commands;
using VoiceLinkChatBot.Handlers;

namespace VoiceLinkChatBot.Extensions;

public static class IServiceCollectionExtension
{
    public static IServiceCollection AddCommands(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddCommandsExtension((IServiceProvider serviceProvider, CommandsExtension extension) =>
        {
            extension.AddCommands<ChannelCommands>();
            extension.AddCommands<AutoRoleCommand>();
        });

        return serviceCollection;
    }

    public static IServiceCollection AddEventHandlers(this IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureEventHandlers(b =>
        {
            b.AddEventHandlers<VoiceStateUpdatedHandler>(ServiceLifetime.Scoped);
            b.AddEventHandlers<MessageCreatedHandler>(ServiceLifetime.Scoped);
            b.AddEventHandlers<ThreadUpdatedHandler>(ServiceLifetime.Scoped);
            b.AddEventHandlers<GuildMemberAddedHandler>(ServiceLifetime.Scoped);
            b.AddEventHandlers<GuildAuditLogCreatedHandler>(ServiceLifetime.Scoped);
        });
        return serviceCollection;
    }
}
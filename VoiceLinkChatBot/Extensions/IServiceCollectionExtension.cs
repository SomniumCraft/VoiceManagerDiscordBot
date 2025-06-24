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
        serviceCollection.AddTransient<MessageCreatedHandler>();
        serviceCollection.ConfigureEventHandlers(b =>
        {
            b.HandleVoiceStateUpdated(new VoiceStateUpdatedHandler().Handle);
            b.HandleMessageCreated(new MessageCreatedHandler().Handle);
            b.HandleThreadUpdated(new ThreadUpdatedHandler().Handle);
            b.HandleGuildMemberAdded(new GuildMemberAddedHandler().Handle);
        });
        return serviceCollection;
    }
}
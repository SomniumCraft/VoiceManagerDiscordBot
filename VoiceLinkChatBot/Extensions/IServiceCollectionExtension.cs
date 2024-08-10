using DSharpPlus.Extensions;
using VoiceLinkChatBot.Handlers;

namespace VoiceLinkChatBot.Extensions;

public static class IServiceCollectionExtension
{
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
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Extensions;
using VoiceLinkChatBot.Services;

namespace VoiceLinkChatBot.Extensions;

public static class IServiceCollectionExtension
{
    public static IServiceCollection AddEventHandlers(this IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureEventHandlers(b => b.HandleVoiceStateUpdated(VoiceStateHandler));
        return serviceCollection;
    }
    
        private static async Task VoiceStateHandler(DiscordClient client, VoiceStateUpdatedEventArgs e)
    {
        if(e.After?.Channel?.Id == e.Before?.Channel?.Id) return;
        if(e.User.IsBot) return;

        var channelsService = client.ServiceProvider.GetService<LinkedChannelsService>();
        
        var channelLinks = await channelsService.GetLinkedChannels(e.Guild.Id);
        
        if (e.Before?.Channel?.Id != e.After?.Channel?.Id && e.Before?.Channel is not null)
        {
            var beforeChannelLinks = channelLinks
                .Where(x => x.VoiceChannelId == e.Before.Channel.Id)
                .ToList();
            
            foreach (var beforeChannelLink in beforeChannelLinks)
            {
                var tc = await client.GetChannelAsync(beforeChannelLink.TextChannelId);
                await tc.DeleteOverwriteAsync(e.Before.Member);
            }
            
            var message = await e.Before.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent($"{(string.IsNullOrEmpty(e.Before.Member.Nickname) ? e.User.GlobalName : e.Before.Member.Nickname)} вышел"));
            await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{e.User.Id}> вышел"));
        }
        
        if (e.After?.Channel is not null)
        {
           var channelLink = channelLinks
               .Where(x => x.VoiceChannelId == e.After.Channel.Id)
               .ToList();
           
           foreach (var channelLinkModel in channelLink)
           {
               var tc = await client.GetChannelAsync(channelLinkModel.TextChannelId);
               await tc.AddOverwriteAsync(e.After.Member, DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages);
           }
           
           var message = await e.After.Channel.SendMessageAsync(new DiscordMessageBuilder()
               .WithContent($"{(string.IsNullOrEmpty(e.After.Member.Nickname) ? e.User.GlobalName : e.After.Member.Nickname)} зашёл"));
           await message.ModifyAsync(new DiscordMessageBuilder().WithContent($"<@{e.User.Id}> зашёл"));
        }
    }
}
using DSharpPlus;
using DSharpPlus.AsyncEvents;

namespace VoiceLinkChatBot.Handlers;

public interface IDiscordEventHandler<in TArgs> where TArgs : AsyncEventArgs
{
  public Task Handle(DiscordClient discordClient, TArgs args);
}
using DSharpPlus;
using DSharpPlus.Extensions;
using VoiceLinkChatBot.Extensions;
using VoiceLinkChatBot.Services;
using VoiceLinkChatBot.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<ChannelsService>()
    //TODO: options pattern
    .AddDiscordClient(builder.Configuration["DiscordBotToken"], DiscordIntents.All)
    .AddHostedService<Worker>()
    .AddCommands()
    .AddEventHandlers();

var host = builder.Build();
host.Run();
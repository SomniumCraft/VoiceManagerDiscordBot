using DSharpPlus;
using DSharpPlus.Extensions;
using VoiceLinkChatBot.Extensions;
using VoiceLinkChatBot.Services;
using VoiceLinkChatBot.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<LinkedChannelsService>();
builder.Services.AddDiscordClient(builder.Configuration["DiscordBotToken"], DiscordIntents.AllUnprivileged);
builder.Services.AddHostedService<Worker>();
builder.Services.AddEventHandlers();

var host = builder.Build();
host.AddDiscordCommands();
host.Run();
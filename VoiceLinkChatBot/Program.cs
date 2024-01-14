using DSharpPlus;
using VoiceLinkChatBot.Extensions;
using VoiceLinkChatBot.Services;
using VoiceLinkChatBot.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<LinkedChannelsService>();
builder.Services.AddSingleton<DiscordClient>(
    new DiscordClient(new DiscordConfiguration
        {
            Token = builder.Configuration["DiscordBotToken"],
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        }
    )
);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.AddDiscordCommands();
host.Run();
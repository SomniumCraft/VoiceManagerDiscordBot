using DSharpPlus;
using DSharpPlus.Extensions;
using VoiceLinkChatBot.Extensions;
using VoiceLinkChatBot.Services;
using VoiceLinkChatBot.Settings;
using VoiceLinkChatBot.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<ChannelsService>()
    .Configure<DiscordSettings>(builder.Configuration.GetRequiredSection(DiscordSettings.SectionName))
    .AddDiscordClient(
        builder.Configuration
            .GetRequiredSection(DiscordSettings.SectionName)
            .Get<DiscordSettings>(options => options.ErrorOnUnknownConfiguration = true)!
            .Token,
        DiscordIntents.All)
    .AddHostedService<Worker>()
    .AddCommands()
    .AddEventHandlers();

var host = builder.Build();
host.Run();
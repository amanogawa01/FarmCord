using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FarmCord.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace FarmCord;

public class Program
{
    private DiscordSocketClient _client = null!;
    private InteractionService _interactions = null!;
    private IServiceProvider _services = null!;
    private Creds _creds = null!;

    public static Task Main()
        => new Program().MainAsync();

    public async Task MainAsync()
    {
        var credsPath = Path.Combine(
            AppContext.BaseDirectory,
            "creds.json");

        if (!File.Exists(credsPath))
        {
            Console.WriteLine("creds.json missing.");
            return;
        }

        _creds = JsonSerializer.Deserialize<Creds>(
            await File.ReadAllTextAsync(credsPath))
            ?? throw new Exception("Failed to parse creds.json");

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents =
                GatewayIntents.Guilds |
                GatewayIntents.GuildMessages |
                GatewayIntents.DirectMessages,

            LogLevel = LogSeverity.Info
        });

        _interactions = new InteractionService(_client.Rest);

        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_interactions)
            .AddSingleton<MongoService>()
            .BuildServiceProvider();

        _client.Log += LogAsync;
        _interactions.Log += LogAsync;

        _client.Ready += ReadyAsync;
        _client.InteractionCreated += HandleInteraction;

        await _client.LoginAsync(
            TokenType.Bot,
            _creds.Token);

        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task ReadyAsync()
    {
        await _interactions.AddModulesAsync(
            Assembly.GetEntryAssembly(),
            _services);

        await _interactions.RegisterCommandsGloballyAsync();

        await _client.SetGameAsync(
            "Start your farm today!");

        Console.WriteLine(
            $"Connected as {_client.CurrentUser}");
    }

    private async Task HandleInteraction(
        SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(
                _client,
                interaction);

            await _interactions.ExecuteCommandAsync(
                context,
                _services);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            if (interaction.Type ==
                InteractionType.ApplicationCommand)
            {
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(async msg =>
                        await msg.Result.DeleteAsync());
            }
        }
    }

    private Task LogAsync(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());

        return Task.CompletedTask;
    }
}
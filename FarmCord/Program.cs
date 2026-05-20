using Discord;
using Discord.Commands;
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
    private CommandService _commands = null!;
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
                GatewayIntents.DirectMessages |
                GatewayIntents.MessageContent,

            LogLevel = LogSeverity.Info
        });

        _commands = new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Info,
            CaseSensitiveCommands = false
        });


        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .AddSingleton<MongoService>()
            .BuildServiceProvider();

        _client.Log += LogAsync;
        _commands.Log += LogAsync;

        await RegisterCommandsAsync();


        await _client.LoginAsync(
            TokenType.Bot,
            _creds.Token);

        await _client.StartAsync();


        _client.Ready += OnReadyAsync;

        await Task.Delay(-1);
    }

    private async Task OnReadyAsync()
    {
        await _client.SetGameAsync("Start your farm today!");

        Console.WriteLine(
            $"Connected as {_client.CurrentUser}");
    }


    private async Task RegisterCommandsAsync()
    {
        _client.MessageReceived += HandleCommandAsync;

        await _commands.AddModulesAsync(
            Assembly.GetEntryAssembly(),
            _services);
    }

    private async Task HandleCommandAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message)
            return;

        if (message.Author.IsBot)
            return;

        int argPos = 0;

        bool hasPrefix =
            message.HasStringPrefix(_creds.Prefix, ref argPos) ||
            message.HasMentionPrefix(_client.CurrentUser, ref argPos);

        if (!hasPrefix)
            return;

        var context = new SocketCommandContext(
            _client,
            message);

        var result = await _commands.ExecuteAsync(
            context,
            argPos,
            _services);

        if (!result.IsSuccess)
        {
            await context.Channel.SendMessageAsync(
                $"Error: {result.ErrorReason}");
        }
    }

    private Task LogAsync(LogMessage message)
    {
        Console.WriteLine(message.ToString());

        return Task.CompletedTask;
    }
}
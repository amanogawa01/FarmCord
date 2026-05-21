using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FarmCord.Services;
using FarmCord.Services.ServerBlackListService;
using FarmCord.Services.UserBlackListService;
using MongoDB.Driver;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace FarmCord.Owner.Module;

[RequireOwner]
public class OwnerModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MongoService _mongo;
    private readonly DiscordSocketClient _client;

    public OwnerModule(
        MongoService mongo,
        DiscordSocketClient client)
    {
        _mongo = mongo;
        _client = client;
    }


    [SlashCommand(
        "serverblacklist",
        "Blacklist a server from using the bot")]
    public async Task ServerBlackListAsync(
        [Summary("serverid", "The server ID")]
        ulong serverId,

        [Summary("reason", "Reason for blacklist")]
        string reason = "No reason provided")
    {
        var server = _client.GetGuild(serverId);

        if (server == null)
        {
            await RespondAsync(
                "Server not found.",
                ephemeral: true);

            return;
        }

        var collection = _mongo.Database
            .GetCollection<ServerBlackListDoc>(
                "ServerBlackLists");

        var doc = new ServerBlackListDoc
        {
            ServerName = server.Name,
            ServerId = server.Id,
            BanDate = DateTime.UtcNow,
            Reason = reason
        };

        await collection.ReplaceOneAsync(
            x => x.ServerId == server.Id,
            doc,
            new ReplaceOptions
            {
                IsUpsert = true
            });

        var embed = new EmbedBuilder()
            .WithColor(Color.DarkRed)
            .WithTitle("Server Blacklisted")
            .WithDescription(
                $"**{server.Name}** (`{server.Id}`)")
            .AddField("Reason", reason)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand(
        "userblacklist",
        "Blacklist a user from using the bot")]
    public async Task UserBlackListAsync(
        [Summary("userid", "The user ID")]
        ulong userId,

        [Summary("reason", "Reason for blacklist")]
        string reason = "No reason provided")
    {
        var user = await _client.GetUserAsync(userId);

        if (user == null)
        {
            await RespondAsync(
                "User not found.",
                ephemeral: true);

            return;
        }

        var collection = _mongo.Database
            .GetCollection<UserBlackListDoc>(
                "UserBlackLists");

        var doc = new UserBlackListDoc
        {
            UserId = user.Id,
            UserName = user.Username,
            BanDate = DateTime.UtcNow,
            Reason = reason
        };

        await collection.ReplaceOneAsync(
            x => x.UserId == user.Id,
            doc,
            new ReplaceOptions
            {
                IsUpsert = true
            });

        var embed = new EmbedBuilder()
            .WithColor(Color.DarkRed)
            .WithTitle("User Blacklisted")
            .WithDescription(
                $"**{user.Username}** (`{user.Id}`)")
            .AddField("Reason", reason)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand(
        "dm",
        "Send a DM to a user")]
    public async Task DmAsync(
        [Summary("userid", "The user ID")]
        ulong userId,

        [Summary("message", "Message to send")]
        string message)
    {
        var user = await _client.GetUserAsync(userId);

        if (user == null)
        {
            await RespondAsync(
                "User not found.",
                ephemeral: true);

            return;
        }

        var dm = await user.CreateDMChannelAsync();

        var embed = new EmbedBuilder()
            .WithAuthor(
                $"Message from {_client.CurrentUser.Username}")
            .WithThumbnailUrl(
                _client.CurrentUser.GetAvatarUrl())
            .WithColor(Color.Blue)
            .WithDescription(message)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await dm.SendMessageAsync(embed: embed);

        await RespondAsync(
            $"DM successfully sent to `{user.Username}`",
            ephemeral: true);
    }

    [SlashCommand(
        "listservers",
        "Lists all servers the bot is in")]
    public async Task ListServersAsync()
    {
        var guilds = _client.Guilds
            .Select(x =>
                $"{x.Name} ({x.Id})");

        var output = string.Join(
            "\n",
            guilds);

        if (string.IsNullOrWhiteSpace(output))
        {
            output = "No servers.";
        }

        if (output.Length > 1900)
        {
            output = output[..1900];
        }

        await RespondAsync(
            $"```{output}```",
            ephemeral: true);
    }

    [SlashCommand(
        "setgame",
        "Set the bot game/activity")]
    public async Task SetGameAsync(
        [Summary("game", "The game text")]
        string game)
    {
        await _client.SetGameAsync(game);

        await RespondAsync(
            $"Game set to `{game}`");
    }

    [SlashCommand(
        "setstatus",
        "Set the bot status")]
    public async Task SetStatusAsync(
        [Summary(
            "status",
            "online, idle, dnd, invisible")]
        string status)
    {
        if (!Enum.TryParse<UserStatus>(
            status,
            true,
            out var parsed))
        {
            await RespondAsync(
                "Valid statuses:\n" +
                "`online`\n" +
                "`idle`\n" +
                "`dnd`\n" +
                "`invisible`",
                ephemeral: true);

            return;
        }

        await _client.SetStatusAsync(parsed);

        await RespondAsync(
            $"Status updated to `{parsed}`");
    }

    [SlashCommand(
        "shutdown",
        "Shutdown the bot")]
    public async Task ShutdownAsync()
    {
        await RespondAsync(
            "Shutting down...");

        await _client.LogoutAsync();

        Environment.Exit(0);
    }

    [SlashCommand(
        "backup",
        "Create and DM a backup zip")]
    public async Task BackupAsync()
    {
        var outputDir = Path.Combine(
            AppContext.BaseDirectory,
            "FarmOutput");

        if (!Directory.Exists(outputDir))
        {
            await RespondAsync(
                "FarmOutput folder not found.",
                ephemeral: true);

            return;
        }

        var backupName =
            $"FarmBackup_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.zip";

        var backupPath = Path.Combine(
            AppContext.BaseDirectory,
            backupName);

        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        ZipFile.CreateFromDirectory(
            outputDir,
            backupPath);

        await Context.User.SendFileAsync(
            backupPath);

        await RespondAsync(
            "Backup created and sent.",
            ephemeral: true);
    }
}

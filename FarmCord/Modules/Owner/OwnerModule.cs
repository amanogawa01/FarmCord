using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FarmCord.Services;
using FarmCord.Services.ServerBlackListService;
using FarmCord.Services.UserBlackListService;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace FarmCord.Owner.Module;

[RequireOwner]
public class OwnerModule : ModuleBase<SocketCommandContext>
{
    private readonly MongoService _mongo;
    private readonly DiscordSocketClient _client;

    public OwnerModule(MongoService mongo, DiscordSocketClient client)
    {
        _mongo = mongo;
        _client = client;
    }

    [Command("serverblacklist")]
    [Alias("sbl")]
    public async Task ServerBlackListAsync(ulong serverId, [Remainder] string reason = "")
    {
        var server = _client.GetGuild(serverId);

        if (server == null)
        {
            await ReplyAsync("Server not found.");
            return;
        }

        var collection = _mongo.Database
            .GetCollection<ServerBlackListDoc>("ServerBlackLists");

        var doc = new ServerBlackListDoc
        {
            ServerName = server.Name,
            ServerId = server.Id,
            BanDate = DateTime.UtcNow,
            Reason = reason
        };

        await collection.InsertOneAsync(doc);

        await ReplyAsync(embed: new EmbedBuilder()
            .WithColor(Color.DarkRed)
            .WithDescription($"Blacklisted server **{server.Name}** (`{server.Id}`)")
            .Build());
    }

    [Command("userblacklist")]
    [Alias("ubl")]
    public async Task UserBlackListAsync(ulong userId, [Remainder] string reason = "")
    {
        var user = await _client.GetUserAsync(userId);

        if (user == null)
        {
            await ReplyAsync("User not found.");
            return;
        }

        var collection = _mongo.Database
            .GetCollection<UserBlackListDoc>("UserBlackLists");

        var doc = new UserBlackListDoc
        {
            UserId = user.Id,
            UserName = user.Username,
            BanDate = DateTime.UtcNow,
            Reason = reason
        };

        await collection.InsertOneAsync(doc);

        await ReplyAsync(embed: new EmbedBuilder()
            .WithColor(Color.DarkRed)
            .WithDescription($"Blacklisted user **{user.Username}** (`{user.Id}`)")
            .Build());
    }

    [Command("dm")]
    public async Task DmAsync(ulong userId, [Remainder] string message)
    {
        var user = await _client.GetUserAsync(userId);

        if (user == null)
        {
            await ReplyAsync("User not found.");
            return;
        }

        var dm = await user.CreateDMChannelAsync();

        var embed = new EmbedBuilder()
            .WithAuthor($"Message from {_client.CurrentUser.Username}")
            .WithThumbnailUrl(_client.CurrentUser.GetAvatarUrl())
            .WithDescription(message)
            .WithColor(Color.Blue)
            .Build();

        await dm.SendMessageAsync(embed: embed);

        await ReplyAsync($"DM sent to `{user.Username}`");
    }

    [Command("listservers")]
    public async Task ListServersAsync()
    {
        var guilds = _client.Guilds
            .Select(x => $"{x.Name} ({x.Id})");

        var output = string.Join("\n", guilds);

        if (string.IsNullOrWhiteSpace(output))
            output = "No servers.";

        await ReplyAsync($"```{output}```");
    }

    [Command("setgame")]
    public async Task SetGameAsync([Remainder] string game)
    {
        await _client.SetGameAsync(game);

        await ReplyAsync($"Game set to: `{game}`");
    }

    [Command("setstatus")]
    public async Task SetStatusAsync(string status)
    {
        if (!Enum.TryParse<UserStatus>(status, true, out var parsed))
        {
            await ReplyAsync("Valid statuses: online, idle, dnd, invisible");
            return;
        }

        await _client.SetStatusAsync(parsed);

        await ReplyAsync($"Status updated to `{parsed}`");
    }

    [Command("shutdown")]
    [Alias("die", "kill", "commitdie")]
    public async Task ShutdownAsync()
    {
        await ReplyAsync("Shutting down...");

        await _client.LogoutAsync();
        Environment.Exit(0);
    }

    [Command("backup")]
    public async Task BackupAsync()
    {
        var outputDir = Path.Combine(AppContext.BaseDirectory, "FarmOutput");

        if (!Directory.Exists(outputDir))
        {
            await ReplyAsync("FarmOutput folder not found.");
            return;
        }

        var backupName = $"FarmBackup_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.zip";

        var backupPath = Path.Combine(
            AppContext.BaseDirectory,
            backupName);

        ZipFile.CreateFromDirectory(outputDir, backupPath);

        await Context.User.SendFileAsync(backupPath);

        await ReplyAsync("Backup created and sent.");
    }
}
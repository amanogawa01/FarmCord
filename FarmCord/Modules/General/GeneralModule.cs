using Discord;
using Discord.Interactions;
using FarmCord.Services;
using FarmCord.Services.DailyService;
using FarmCord.Services.PrefixService;
using ImageMagick;
using ImageMagick.Drawing;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FarmCord.General.Module;

public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MongoService _mongo;

    public GeneralModule(MongoService mongo)
    {
        _mongo = mongo;
    }

    [SlashCommand("help", "Shows all commands")]
    public async Task HelpAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{Context.Client.CurrentUser.Username} Help")
            .WithColor(Color.Green)
            .WithDescription("""
/help - commands
/invite - invite bot
/ping - latency
/shop - shop items
/start - start farm
/daily - claim reward
/prefix - set server prefix
/stats - bot stats
""")
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("invite", "Get the bot invite link")]
    public async Task InviteAsync()
    {
        await RespondAsync(
            "https://discord.com/oauth2/authorize?client_id=630849680431120385&scope=bot");
    }

    [SlashCommand("ping", "Shows bot latency")]
    public async Task PingAsync()
    {
        var sw = Stopwatch.StartNew();

        await RespondAsync("🏓 Pong!");

        sw.Stop();

        await FollowupAsync(
            $"Latency: `{sw.ElapsedMilliseconds}ms`");
    }

    [SlashCommand("shop", "Shows the farm shop")]
    public async Task ShopAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Farm Shop")
            .WithColor(Color.Gold)
            .WithDescription("""
🌱 Watermelon Seed - FC$50
🌽 Corn Seed - FC$60
🍈 Cantaloupe Seed - FC$45
""")
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("start", "Create your farm image")]
    public async Task StartAsync()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Island.png");

        if (!File.Exists(path))
        {
            await RespondAsync(
                "Missing asset image.",
                ephemeral: true);

            return;
        }

        using var image = new MagickImage(path);

        new Drawables()
            .FontPointSize(48)
            .FillColor(MagickColors.White)
            .TextAlignment(TextAlignment.Center)
            .Text(
                image.Width / 2,
                50,
                $"{Context.User.Username}'s Farm")
            .Draw(image);

        var outputDir = Path.Combine(
            AppContext.BaseDirectory,
            "FarmOutput");

        Directory.CreateDirectory(outputDir);

        var file = Path.Combine(
            outputDir,
            $"Farm_{Context.User.Id}.png");

        image.Write(file);

        await RespondWithFileAsync(file);
    }

    [SlashCommand("prefix", "Set the server prefix")]
    public async Task PrefixAsync(
        [Summary("prefix", "New server prefix")]
        string prefix)
    {
        if (Context.Guild == null)
        {
            await RespondAsync(
                "Guild-only command.",
                ephemeral: true);

            return;
        }

        var collection = _mongo.Database
            .GetCollection<PrefixService>("Prefixes");

        var doc = new PrefixService
        {
            ServerId = Context.Guild.Id,
            Prefix = prefix
        };

        await collection.ReplaceOneAsync(
            x => x.ServerId == Context.Guild.Id,
            doc,
            new ReplaceOptions
            {
                IsUpsert = true
            });

        await RespondAsync(
            $"Prefix set to `{prefix}`");
    }

    [SlashCommand("stats", "Shows bot statistics")]
    public async Task StatsAsync()
    {
        var proc = Process.GetCurrentProcess();

        var embed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .AddField(
                "Uptime",
                proc.StartTime.ToString(),
                true)
            .AddField(
                "Servers",
                Context.Client.Guilds.Count,
                true)
            .AddField(
                "Users",
                Context.Client.Guilds.Sum(
                    x => x.MemberCount),
                true)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("daily", "Claim your daily reward")]
    public async Task DailyAsync()
    {
        var collection = _mongo.Database
            .GetCollection<DailyService>("Dailies");

        var userId = Context.User.Id;

        var existing = await collection
            .Find(x => x.UserID == userId)
            .FirstOrDefaultAsync();

        if (existing != null &&
            (DateTime.UtcNow - existing.DailyDate)
            .TotalHours < 24)
        {
            var remaining =
                TimeSpan.FromHours(24) -
                (DateTime.UtcNow - existing.DailyDate);

            await RespondAsync(
                $"You already claimed your daily reward.\n" +
                $"Try again in `{remaining.Hours}h {remaining.Minutes}m`.",
                ephemeral: true);

            return;
        }

        const int reward = 100;

        var doc = new DailyService
        {
            UserID = userId,
            DailyDate = DateTime.UtcNow,
            Amount = reward
        };

        await collection.ReplaceOneAsync(
            x => x.UserID == userId,
            doc,
            new ReplaceOptions
            {
                IsUpsert = true
            });

        var embed = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithTitle("Daily Reward")
            .WithDescription(
                $"You received `FC${reward}`!")
            .Build();

        await RespondAsync(embed: embed);
    }
}
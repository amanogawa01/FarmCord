using Discord;
using Discord.Commands;
using FarmCord.Services;
using FarmCord.Services.DailyService;
using FarmCord.Services.PrefixService;
using ImageMagick;
using ImageMagick.Drawing;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FarmCord.General.Module;

public class GeneralModule : ModuleBase<SocketCommandContext>
{
    private readonly MongoService _mongo;

    public GeneralModule(MongoService mongo)
    {
        _mongo = mongo;
    }
    [Command("help"), Alias("h")]
    public async Task HelpAsync()
    {
        await ReplyAsync(embed: new EmbedBuilder()
            .WithTitle($"{Context.Client.CurrentUser.Username} Help")
            .WithColor(Color.Green)
            .WithDescription(
@"help - commands
invite - invite bot
ping - latency
shop - shop items
start - start farm
daily - claim reward")
            .Build());
    }
    [Command("invite")]
    public async Task InviteAsync()
    {
        await ReplyAsync("https://discord.com/oauth2/authorize?client_id=630849680431120385&scope=bot");
    }
    [Command("ping")]
    public async Task PingAsync()
    {
        var sw = Stopwatch.StartNew();
        var msg = await ReplyAsync("🏓");
        sw.Stop();

        await msg.DeleteAsync();

        await ReplyAsync($"🏓 {sw.ElapsedMilliseconds}ms");
    }
    [Command("shop"), Alias("sh")]
    public async Task ShopAsync()
    {
        await ReplyAsync(embed: new EmbedBuilder()
            .WithTitle("Farm Shop")
            .WithColor(Color.Gold)
            .WithDescription(
@"🌱 Watermelon Seed - 50
🌽 Corn Seed - 60
🍈 Cantaloupe Seed - 45")
            .Build());
    }
    [Command("start")]
    public async Task StartAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Island.png");

        if (!File.Exists(path))
        {
            await ReplyAsync("Missing asset image.");
            return;
        }

        using var image = new MagickImage(path);

        new Drawables()
            .FontPointSize(48)
            .FillColor(MagickColors.White)
            .TextAlignment(TextAlignment.Center)
            .Text(image.Width / 2, 50, $"{Context.User.Username}'s Farm")
            .Draw(image);

        var outputDir = Path.Combine(AppContext.BaseDirectory, "FarmOutput");
        Directory.CreateDirectory(outputDir);

        var file = Path.Combine(outputDir, $"Farm_{Context.User.Id}.png");

        image.Write(file);

        await Context.Channel.SendFileAsync(file);
    }
    [Command("prefix")]
    public async Task PrefixAsync(string prefix)
    {
        if (Context.Guild == null)
        {
            await ReplyAsync("Guild-only command.");
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
            new ReplaceOptions { IsUpsert = true });

        await ReplyAsync($"Prefix set to `{prefix}`");
    }
    [Command("stats")]
    public async Task StatsAsync()
    {
        var proc = Process.GetCurrentProcess();

        await ReplyAsync(embed: new EmbedBuilder()
            .WithColor(Color.Blue)
            .AddField("Uptime", proc.StartTime.ToString())
            .AddField("Servers", Context.Client.Guilds.Count)
            .Build());
    }

    [Command("daily"), Alias("timely")]
    public async Task DailyAsync()
    {
        var collection = _mongo.Database
            .GetCollection<DailyService>("Dailies");

        var userId = Context.User.Id;

        var existing = await collection
            .Find(x => x.UserID == userId)
            .FirstOrDefaultAsync();

        if (existing != null &&
            (DateTime.UtcNow - existing.DailyDate).TotalHours < 24)
        {
            await ReplyAsync("You already claimed your daily reward.");
            return;
        }

        var reward = 100;

        var doc = new DailyService
        {
            UserID = userId,
            DailyDate = DateTime.UtcNow,
            Amount = reward
        };

        await collection.ReplaceOneAsync(
            x => x.UserID == userId,
            doc,
            new ReplaceOptions { IsUpsert = true });

        await ReplyAsync(embed: new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription($"You received FC${reward}!")
            .Build());
    }
}
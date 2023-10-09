namespace Bot.App.Modules;

using Serilog;
using Simple.API;
using Simple.BotUtils.DI;
using Simple.BotUtils.Jobs;
using Simple.DatabaseWrapper.Attributes;
using Simple.Sqlite;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

/// <summary>
/// Module to Ping sites
/// </summary>
internal class SitePing : IJob
{
    private readonly TelegramBotClient bot;
    private readonly ILogger log;
    private readonly ConnectionFactory db;
    private readonly ClientInfo client;

    public bool CanBeInvoked { get; set; } = true;
    public bool CanBeScheduled { get; set; } = true;
    public bool RunOnStartUp { get; set; } = false; // do not run on STARTUP
    public TimeSpan StartEvery { get; set; } = TimeSpan.FromMinutes(30);

    public SitePing()
    {
        client = new ClientInfo("https://quotes.toscrape.com/");
        bot = Injector.Get<TelegramBotClient>();
        log = Injector.Get<ILogger>();
        db = Injector.Get<ConnectionFactory>();

        initializeDB();
    }
    private void initializeDB()
    {
        using var cnn = db.GetConnection();
        cnn.CreateTables()
           .Add<PingTable>()
           .Commit();
    }

    // cannot be called, is not a controller
    public async Task ExecuteAsync(ExecutionTrigger trigger, object parameter)
    {
        var result = await client.GetAsync<string>("/");

        if (result.IsSuccessStatusCode) // site is alive
        {
            if (result.Duration.TotalSeconds > 5) // site is slow
            {
                // Log
                log.Warning("[SitePing] quotes.toscrape.com is slow. Response Time: {ms}ms", (int)result.Duration.TotalMilliseconds);
                // Warn user
                await bot.SendTextMessageAsync(Config.ADMIN_TELEGRAM_ID, $"Slow Ping! {result.Duration.TotalMilliseconds}ms");
            }
        }
        else // site is not ok =(
        {
            log.Warning("[SitePing] quotes.toscrape.com is unavailable. ResponseCode: {code}", result.StatusCode);
            // Warn user
            await bot.SendTextMessageAsync(Config.ADMIN_TELEGRAM_ID, $"Site Down! Code: {result.StatusCode}");
        }

        using var cnn = db.GetConnection();
        cnn.Insert(new PingTable
        {
            Id = 0, // Auto generated
            EventUTC = DateTime.UtcNow,
            ResponseCode = (int)result.StatusCode,
            ResponseTime = result.Duration
        });
    }

    public record PingTable
    {
        [PrimaryKey]
        public long Id { get; set; }
        public DateTime EventUTC { get; set; }
        public int ResponseCode { get; set; }
        public TimeSpan ResponseTime { get; set; }
    }
}

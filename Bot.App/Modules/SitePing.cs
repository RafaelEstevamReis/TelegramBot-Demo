namespace Bot.App.Modules;

using Serilog;
using Simple.API;
using Simple.BotUtils.DI;
using Simple.BotUtils.Jobs;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

/// <summary>
/// Module to Ping sites
/// </summary>
internal class SitePing : IJob
{
    private readonly ITelegramBotClient bot;
    private readonly ILogger log;
    private readonly ClientInfo client;

    public bool CanBeInvoked { get; set; } = true;
    public bool CanBeScheduled { get; set; } = true;
    public bool RunOnStartUp { get; set; } = false; // do not run on STARTUP
    public TimeSpan StartEvery { get; set; } = TimeSpan.FromMinutes(30);

    public SitePing()
    {
        bot = Injector.Get<ITelegramBotClient>();
        log = Injector.Get<ILogger>();
        client = new ClientInfo("https://quotes.toscrape.com/");
    }

    // cannot be called, is not a controller
    public async Task ExecuteAsync(ExecutionTrigger trigger, object parameter)
    {
        var result = await client.GetAsync<string>("/");

        if (result.IsSuccessStatusCode)
        {
            if(result.Duration.TotalSeconds > 5)
            {
                // Log
                log.Warning("[SitePing] quotes.toscrape.com is slow. Response Time: {ms}ms", (int)result.Duration.TotalMilliseconds);
                // Warn user
                await bot.SendTextMessageAsync(Config.ADMIN_TELEGRAM_ID, $"Slow Ping! {result.Duration.TotalMilliseconds}ms");
            }
        }
        else
        {
            log.Warning("[SitePing] quotes.toscrape.com is unavailable. ResponseCode: {code}", result.StatusCode);
            // Warn user
            await bot.SendTextMessageAsync(Config.ADMIN_TELEGRAM_ID, $"Site Down! Code: {result.StatusCode}");
        }
    }

}

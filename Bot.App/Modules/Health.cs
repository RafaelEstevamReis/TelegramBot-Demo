namespace Bot.App.Modules;

using Serilog;
using Simple.BotUtils.Controllers;
using Simple.BotUtils.DI;
using Simple.BotUtils.Jobs;
using System;
using System.Threading.Tasks;

/// <summary>
/// Module to handle Bot HeathStatus
/// </summary>
internal class Health : IJob, IController
{
    private readonly ILogger log;

    public bool CanBeInvoked { get; set; } = false;
    public bool CanBeScheduled { get; set; } = true;
    public bool RunOnStartUp { get; set; } = true;
    public TimeSpan StartEvery { get; set; } = TimeSpan.FromMinutes(10);

    public Health()
    {
        log = Injector.Get<ILogger>();
    }
    [Ignore] // Do not call via controller
    public Task ExecuteAsync(ExecutionTrigger trigger, object parameter)
    {
        log.Information("[Health] {trigger}", trigger);
        return Task.CompletedTask;
    }

    // PING command
    public async Task Ping(CommandArguments args)
    {
        await args.Message.Reply("Pong 🏓");
    }
}

using Bot.App;
using Serilog;
using Simple.BotUtils.Controllers;
using Simple.BotUtils.Data;
using Simple.BotUtils.DI;
using Simple.BotUtils.Jobs;
using Simple.BotUtils.Startup;
using Simple.Sqlite;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;


Console.WriteLine("Hello, World!");

var cancellationSource = new CancellationTokenSource();

setupConfig(args);
setupLogs();
Scheduler sch;
try
{
    setupDatabase();
    setupTelegramBot(cancellationSource.Token);
    setupTelegramCommands();
    sch = setupScheduler();
    Injector.Get<ILogger>().Information("Initialization complete");
}
catch (Exception ex)
{
    Injector.Get<ILogger>().Error(ex, "SETUP FAILED");
    throw;
}

sch.RunJobsSynchronously(cancellationSource.Token);

void setupConfig(string[] args)
{
    // Load saved config (or create a empty one)
    var cfg = Config.Load("config.json");
    // Update config with arguments, if any
    if (args.Length > 0)
    {
        ArgumentParser.ParseInto(args, cfg);
        // and save to next boot
        cfg.Save();
    }
    if (cfg.DatabasePath == null)
    {
        cfg.DatabasePath = "/db/";
        cfg.Save();
    }
    if (cfg.LogsPath == null)
    {
        cfg.LogsPath = "/db/logs";
        cfg.Save();
    }

    Injector.AddSingleton(cfg);
}
void setupLogs()
{
    var cfg = Injector.Get<Config>();

    ILogger log = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(Path.Combine(cfg.LogsPath, "eventlog.log"), rollingInterval: RollingInterval.Month)
        .CreateLogger();
    log.Information("Logging started");
    Injector.AddSingleton(log);
    // Registra log padrãop
    RafaelEstevam.Simple.Spider.Helper.FetchHelper.Logger = log;
}

void setupDatabase()
{
    var cfg = Injector.Get<Config>();

    createBaseDB(Path.Combine(cfg.DatabasePath, "dados.db"));
}
void createBaseDB(string databaseFile)
{
    var logger = Injector.Get<ILogger>();
    var connFactory = ConnectionFactory.FromFile(databaseFile);
    logger.Information("SETUP DB initialized {db}", databaseFile);
    Injector.AddSingleton(connFactory);
}
Scheduler setupScheduler()
{
    var tasker = new Scheduler();
    tasker.Error += (object? sender, TaskErrorEventArgs e) => Injector.Get<ILogger>().Error(e.Exception, e.Info.ToString());
    // pega assembly atual (este program.cs)
    var asm = Assembly.GetExecutingAssembly();
    AddJobs(asm, tasker);

    Injector.Get<ILogger>().Information("SETUP Scheduler init {TaskCount} tasks", tasker.JobCount);

    Injector.AddSingleton(tasker);
    return tasker;
}
void AddJobs(Assembly assembly, Scheduler tasker)
{
    var jobs = Simple.BotUtils.Helpers.TypeHelper.GetClassesOfType<IJob>(assembly);

    foreach (var jType in jobs)
    {
        var instance = Activator.CreateInstance(jType) as IJob;
        tasker.Add(jType, instance);
    }
}

void setupTelegramBot(CancellationToken token)
{
    var cfg = Injector.Get<Config>();

    if (cfg.TelegramToken == null) throw new Exception("Telgram bot not configured");

    var client = new TelegramBotClient(cfg.TelegramToken);
    client.StartReceiving(new DefaultUpdateHandler(UpdateHandler.HandleUpdateAsync, UpdateHandler.HandleErrorAsync), cancellationToken: token);

    Injector.Get<ILogger>().Information("SETUP Telegram bot initialized");
    Injector.AddSingleton(client);
}
void setupTelegramCommands()
{
    var ctrl = new ControllerManager()
                .AddControllers(Assembly.GetExecutingAssembly());
    ctrl.AcceptSlashInMethodName = true;
    ctrl.Filter += UpdateHandler.ControllerFilter;

    Injector.AddSingleton(ctrl);
}
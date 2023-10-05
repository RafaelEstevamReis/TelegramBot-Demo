namespace Bot.App;

using Serilog;
using Simple.BotUtils.Controllers;
using Simple.BotUtils.DI;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

internal static class UpdateHandler
{
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var log = Injector.Get<ILogger>();

        Task handler = null;
        if (update.Type == UpdateType.Message)
        {
            switch (update.Message.Type)
            {
                case MessageType.Text:
                    handler = BotOnMessageTextReceived(botClient, update.Message, log);
                    break;
                case MessageType.Location:
                    handler = BotOnMessageLocationReceived(botClient, update.Message, log);
                    break;
            }
        }
        else if (update.Type == UpdateType.EditedMessage)
        {
            if (update.EditedMessage.Type == MessageType.Location)
            {
                handler = BotOnMessageLocationReceived(botClient, update.EditedMessage, log);
            }

        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            handler = BotOnMessageCallbackReceived(botClient, update.CallbackQuery, log);
        }

        if (handler == null) handler = UnknownUpdateHandlerAsync(botClient, update);

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Err {@update}", update);
            await HandleErrorAsync(botClient, exception, cancellationToken);
        }
    }
    private static async Task BotOnMessageTextReceived(ITelegramBotClient botClient, Message message, ILogger log)
    {
        await processControllerCommands(botClient, message, log, message.Text);
    }
    private static async Task BotOnMessageLocationReceived(ITelegramBotClient botClient, Message message, ILogger log)
    {
        var loc = message.Location;
        await processControllerCommands(botClient, message, log, $"location {loc.Latitude} {loc.Longitude}");
    }
    private static async Task BotOnMessageCallbackReceived(ITelegramBotClient botClient, CallbackQuery query, ILogger log)
    {
        // replace messagrFrom
        query.Message.From = query.From;
        await processControllerCommands(botClient, query.Message, log, query.Data);
    }
    private static async Task processControllerCommands(ITelegramBotClient botClient, Message message, ILogger log, string commandText)
    {
        var ctrl = Injector.Get<ControllerManager>();
        var cfg = Injector.Get<Config>();

        if (message.From == null)
        {
            log.Information("EMPTY FROM {@message}", message);
            return;
        }

        var cArgs = new CommandArguments()
        {
            ChatID = message.Chat.Id,
            FromID = message.From.Id,
            Message = message,
        };

        try
        {
            log.Information("Message {user}[{id}] `{text}`", message.From.Username ?? message.From.FirstName, message.From.Id, commandText);
            ctrl.ExecuteFromText(context: cArgs, commandText);
        }
        catch (FilteredException)
        {
            log.Information("FILTERED {user}[{id}] `{text}`", message.From.Username, message.From.Id, commandText);
            await message.Reply($"Message filtered/blocked");
        }
        catch (UnkownMethod ex)
        {
            log.Error(ex, "Err {user}[{id}] `{text}`", message.From.Username, message.From.Id, commandText);
            await message.Reply($"Unkown command {ex.MethodName}");
        }
        catch (NoSuitableMethodFound ex)
        {
            log.Error(ex, "Err {user}[{id}] `{text}`", message.From.Username, message.From.Id, commandText);
            await message.Reply( $"Incorrect parameters for {ex.MethodName}\nUse HELP command for help");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Err {user}[{id}] `{text}`", message.From.Username, message.From.Id, commandText);
            await message.Reply($"Error:\n{ex.Message}");
        }
    }

    private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
    {
        Console.WriteLine($"Unknown type: {update.Type}");
        return Task.CompletedTask;
    }
    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception.Message);
        Console.WriteLine(exception.StackTrace);

        return Task.CompletedTask;
    }

    internal static void ControllerFilter(object? sender, FilterEventArgs e)
    {
        // TODO
    }
}
public class CommandArguments
{
    public long ChatID { get; set; }
    public long FromID { get; set; }
    public Message Message { get; set; }
}

namespace Bot.App.Modules;

using Simple.BotUtils.Controllers;

/// <summary>
/// Module to handle user info commands
/// </summary>
public class UserInfo : IController
{
    public void Me(CommandArguments args)
    {
        var text = $@"User Info:
> ChatId: {args.ChatID}
> FromId: {args.FromID}

MessageInfo:
> MessageDate: {args.Message.Date}
> FirstName: {args.Message.From?.FirstName}
> LastName: {args.Message.From?.LastName}
> Username: {args.Message.From?.Username}
> Id: {args.Message.From?.Id}
";

        args.Message.Reply(text);
    }
}

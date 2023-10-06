namespace Bot.App;

using Simple.BotUtils.DI;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public static class MessageHelper
{
    public static Task<Message> Reply(this Message message, string text,  bool reply = true, ParseMode? parser = null, IReplyMarkup? markup = null)
    {
        var client = Injector.Get<TelegramBotClient>();
        if (text.Length > 4090) // 4096 - margem
        {
            // too long
            int idxN = text.LastIndexOf('\n', 4096);
            if (idxN > 3072 && idxN <= 4224)
            {
                text = text[..idxN] + "...";
            }
            else
            {
                text = text[..4087] + "...";
            }
        }
        return client.SendTextMessageAsync(message.Chat, text, replyToMessageId: reply ? message.MessageId : null, parseMode: parser, replyMarkup: markup);
    }
    public static Task<Message> Edit(this Message message, string text, ParseMode? parser = null, InlineKeyboardMarkup kbMarkup = null)
    {
       var client = Injector.Get<TelegramBotClient>();
        return client.EditMessageTextAsync(message.Chat, message.MessageId, text, parseMode: parser, replyMarkup: kbMarkup);
    }
    public static async Task Delete(this Message message)
    {
        var client = Injector.Get<TelegramBotClient>();
        await client.DeleteMessageAsync(message.Chat, message.MessageId);
    }

}

namespace Bot.App;

using Simple.BotUtils.Data;

public class Config : ConfigBase<Config>
{
    public const long ADMIN_TELEGRAM_ID = 1234;

    public string TelegramToken { get; set; }
    public string DatabasePath { get; set; }
    public string LogsPath { get; set; }
}

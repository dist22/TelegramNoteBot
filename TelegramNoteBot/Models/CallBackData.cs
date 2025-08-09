using Telegram.Bot.Types;

namespace TelegramNoteBot.Models;

public class CallBackData
{
    public User User { get; set; }
    public int ParsedId { get; set; }
    public string CallBackCommand { get; set; } = string.Empty;
    public string ReservedCommand  { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public bool Desc { get; set; } = false;
}
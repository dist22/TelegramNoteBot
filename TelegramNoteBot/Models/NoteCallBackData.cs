using Telegram.Bot.Types;

namespace TelegramNoteBot.Models;

public class NoteCallBackData
{
    public User User { get; set; }
    public int NoteId { get; set; }
    public string CallBackCommand { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public bool Desc { get; set; } = false;
}
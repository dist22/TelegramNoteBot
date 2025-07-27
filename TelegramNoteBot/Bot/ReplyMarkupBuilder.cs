using Telegram.Bot.Types.ReplyMarkups;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Bot;

public static class ReplyMarkupBuilder
{
    public static ReplyKeyboardMarkup MainMenu()
        => new ([
            [new KeyboardButton(BotCommands.AddNote), new KeyboardButton(BotCommands.MyNotes)],
            [new KeyboardButton(BotCommands.SearchNote),new KeyboardButton(BotCommands.DeleteNote)],
            [new KeyboardButton(BotCommands.AboutDeveloper)]
        ]) { ResizeKeyboard = true };
    
    public static InlineKeyboardMarkup NotesMarkup(IEnumerable<Note> notes, string emoji, string callBackData) 
        => new (notes.Select(n => new[]
            {
                InlineKeyboardButton.WithCallbackData($"{emoji} {n.Title}", $"{callBackData}{n.Id}")
            })
            .ToArray()
        );
    
    public static ReplyKeyboardMarkup GetMarkupBack() => 
        new(new KeyboardButton("⬅️BACK"));

    public static InlineKeyboardButton AboutDeveloper()
        => new ("       My GitHub       ", "https://github.com/dist22");

}
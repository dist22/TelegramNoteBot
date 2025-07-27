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
            [new KeyboardButton(BotCommands.FilterByTag), new KeyboardButton(BotCommands.ManageTags)],
            [new KeyboardButton(BotCommands.AboutDeveloper)]
        ]) { ResizeKeyboard = true };

    public static ReplyKeyboardMarkup TagManagementMenu()
        => new([
            [new KeyboardButton(BotTagCommands.Tsgs), new KeyboardButton(BotTagCommands.AddTsgs)],
            [new KeyboardButton(BotTagCommands.RemoveTags), new KeyboardButton("⬅️ Back")]
        ]) { ResizeKeyboard = true };
    
    public static InlineKeyboardMarkup NotesMarkup(IEnumerable<Note> notes, string emoji, string callBackData) 
        => new (notes.Select(n => new[]
            {
                InlineKeyboardButton.WithCallbackData($"{emoji} {n.Title}", $"{callBackData}{n.Id}")
            })
            .ToArray()
        );

    public static InlineKeyboardButton AboutDeveloper()
        => new ("       My GitHub       ", "https://github.com/dist22");

}
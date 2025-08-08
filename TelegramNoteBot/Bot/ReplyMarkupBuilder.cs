using Telegram.Bot.Types.ReplyMarkups;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Models;
using TelegramNoteBot.Services;

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
            [new KeyboardButton(BotTagCommands.Tags), new KeyboardButton(BotTagCommands.AddTags)],
            [new KeyboardButton(BotTagCommands.RemoveTags), new KeyboardButton(BotTagCommands.Back)]
        ]) { ResizeKeyboard = true };

    public static ReplyKeyboardMarkup AddTagToNoteMenu()
        => new([
            [new KeyboardButton(AddTagToNoteCommands.JoinTag), new KeyboardButton(AddTagToNoteCommands.CreateAndJoin)],
            [new  KeyboardButton(AddTagToNoteCommands.Skip)]
        ]) { ResizeKeyboard = true };

    public static InlineKeyboardMarkup NotesMarkup(IEnumerable<Note> notes, string emoji, string callBackCommand)
    {

        var buttons = new List<InlineKeyboardButton[]>();
        buttons.Add([
            InlineKeyboardButton.WithCallbackData(SortNotesCommands.SortAsc, $"{CallBackCommands.SortNoteAsc}|{1}"),
            InlineKeyboardButton.WithCallbackData(SortNotesCommands.SortDesc, $"{CallBackCommands.SortNoteDesc}|{0}")
        ]);
        
        buttons.AddRange(notes.Select( n => new []
        {
            InlineKeyboardButton.WithCallbackData($"{emoji} {n.Title}", $"{callBackCommand}|{n.Id}")
        }).ToArray());

        return new InlineKeyboardMarkup(buttons);
    }

    public static InlineKeyboardMarkup TagMarkup(IEnumerable<Tag> tags, string emoji, string callBackCommand)
        => new(tags.Select(t => 
                InlineKeyboardButton.WithCallbackData($"{emoji} {t.Name}",$"{callBackCommand}|{t.Id}")
            ).Chunk(2).Select(row => row.ToArray())
        );
    public static InlineKeyboardButton AboutDeveloper()
        => new ("My GitHub", "https://github.com/dist22");

}
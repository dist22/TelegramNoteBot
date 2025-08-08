using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Models;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class ReplyMarkupBuilder(RedisCallBackStorage redisCallBackStorage)
{
    public ReplyKeyboardMarkup MainMenu()
        => new ([
            [new KeyboardButton(BotCommands.AddNote), new KeyboardButton(BotCommands.MyNotes)],
            [new KeyboardButton(BotCommands.SearchNote),new KeyboardButton(BotCommands.DeleteNote)],
            [new KeyboardButton(BotCommands.FilterByTag), new KeyboardButton(BotCommands.ManageTags)],
            [new KeyboardButton(BotCommands.AboutDeveloper)]
        ]) { ResizeKeyboard = true };

    public ReplyKeyboardMarkup TagManagementMenu()
        => new([
            [new KeyboardButton(BotTagCommands.Tags), new KeyboardButton(BotTagCommands.AddTags)],
            [new KeyboardButton(BotTagCommands.RemoveTags), new KeyboardButton(BotTagCommands.Back)]
        ]) { ResizeKeyboard = true };

    public ReplyKeyboardMarkup AddTagToNoteMenu()
        => new([
            [new KeyboardButton(AddTagToNoteCommands.JoinTag), new KeyboardButton(AddTagToNoteCommands.CreateAndJoin)],
            [new  KeyboardButton(AddTagToNoteCommands.Skip)]
        ]) { ResizeKeyboard = true };

    public async Task<InlineKeyboardMarkup> NotesMarkup(IEnumerable<Note> notes, string emoji, string callBackCommand, User user)
    {
        var buttons = new List<InlineKeyboardButton[]>();
        buttons.Add([
            InlineKeyboardButton.WithCallbackData(SortNotesCommands.SortAsc, $"{await CreateCallBackKeyAsync(user, 0, CallBackCommands.SortNoteAsc, emoji)}"),
            InlineKeyboardButton.WithCallbackData(SortNotesCommands.SortDesc, $"{await CreateCallBackKeyAsync(user, 0, CallBackCommands.SortNoteDesc, emoji, true)}")
        ]);

        var notesButtons = await Task.WhenAll(notes.Select(async n =>
        {
            var key = await CreateCallBackKeyAsync(user, n.Id, callBackCommand, emoji);
            return new[]
            {
                InlineKeyboardButton.WithCallbackData($"{emoji} {n.Title}", $"{key}")
            };
        }));
        
        buttons.AddRange(notesButtons);

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup TagMarkup(IEnumerable<Tag> tags, string emoji, string callBackCommand)
        => new(tags.Select(t => 
                InlineKeyboardButton.WithCallbackData($"{emoji} {t.Name}",$"{callBackCommand}|{t.Id}")
            ).Chunk(2).Select(row => row.ToArray())
        );
    public InlineKeyboardButton AboutDeveloper()
        => new ("My GitHub", "https://github.com/dist22");

    private async Task<string> CreateCallBackKeyAsync(User user, int parsedId, string callBackCommand, string emoji, bool decs = false)
    {
        var cbData = new NoteCallBackData
        {
            User = user,
            NoteId = parsedId,
            CallBackCommand = callBackCommand,
            Emoji = emoji,
            Desc = true
        };
        
        return await redisCallBackStorage.StoreCallBackAsync(cbData);
    }

}
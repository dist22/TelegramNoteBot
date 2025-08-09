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
            InlineKeyboardButton.WithCallbackData(SortNotesCommands.SortAsc, $"{await CreateCallBackKeyAsync(user, 0, CallBackCommands.SortNoteAsc,  emoji, callBackCommand)}"),
            InlineKeyboardButton.WithCallbackData(SortNotesCommands.SortDesc, $"{await CreateCallBackKeyAsync(user, 0, CallBackCommands.SortNoteDesc,  emoji,  callBackCommand, true)}")
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

    public async Task<InlineKeyboardMarkup> TagMarkup(IEnumerable<Tag> tags, string emoji, string callBackCommand, User user)
    {
        var tagButtons = await Task.WhenAll(tags.Select(async t =>
        {
            var key = await CreateCallBackKeyAsync(user, t.Id, callBackCommand, emoji);
            return InlineKeyboardButton.WithCallbackData($"{emoji} {t.Name}", $"{key}");
        }));
        
        return new InlineKeyboardMarkup(tagButtons.Chunk(2));
    }
    public InlineKeyboardButton AboutDeveloper()
        => new ("My GitHub", "https://github.com/dist22");

    private async Task<string> CreateCallBackKeyAsync(User user, int parsedId, string callBackCommand, string emoji,
        string reservedCommand = "no", bool desc = false)
    {
        var cbData = new CallBackData
        {
            User = user,
            ParsedId = parsedId,
            CallBackCommand = callBackCommand,
            ReservedCommand = reservedCommand,
            Emoji = emoji,
            Desc = desc
        };
        
        return await redisCallBackStorage.StoreCallBackAsync(cbData);
    }

}
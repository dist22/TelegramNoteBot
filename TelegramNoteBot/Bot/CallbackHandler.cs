using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Enums;
using TelegramNoteBot.Models;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class CallbackHandler
{

    private readonly NoteService _noteService;
    private readonly UserSessionService _userSessionService;
    private readonly TagService _tagService;
    private readonly TagHelperService _tagHelperService;
    private readonly ReplyMarkupBuilder _replyMarkupBuilder;
    private readonly RedisCallBackStorage _redisCallBackStorage;
    private readonly Dictionary<string, Func<long, int, NoteCallBackData, ITelegramBotClient, CancellationToken, Task>>
        _handlers;
    
    public CallbackHandler(NoteService noteService, UserSessionService userSessionService, TagService tagService, TagHelperService tagHelperService, ReplyMarkupBuilder replyMarkupBuilder, RedisCallBackStorage redisCallBackStorage)
    {
        _noteService = noteService;
        _userSessionService = userSessionService;
        _tagService = tagService;
        _tagHelperService = tagHelperService;
        _replyMarkupBuilder = replyMarkupBuilder;
        _redisCallBackStorage = redisCallBackStorage;

        _handlers = new()
        {
            [CallBackCommands.Delete] = HandleDeleteNoteAsync,
            [CallBackCommands.Info] = HandleInfoNoteAsync,
            [CallBackCommands.FilterByTag] = HandleFilterByTagAsync,
            [CallBackCommands.SelectTag] = HandleSelectTagAsync,
            [CallBackCommands.TagDelete] = HandleTagDeleteAsync,
            [CallBackCommands.SortNoteAsc] = HandleSortNoteAsync,
            [CallBackCommands.SortNoteDesc] = HandleSortNoteAsync
        };
    }

    
    public async Task HandleUpdateAsync(ITelegramBotClient client, CallbackQuery callbackQuery, CancellationToken cts)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var user = callbackQuery.From;

        var cbData = await _redisCallBackStorage.GetCallBackAsync(callbackQuery.Data);
        
        if (cbData == null)
            return;

        if (_handlers.TryGetValue(cbData.CallBackCommand, out var handler))
        {
            await handler (chatId, messageId, cbData,client, cts);
        }
    }

    private async Task HandleSortNoteAsync(long chatId, int messageId, NoteCallBackData noteCallBackData, ITelegramBotClient client, CancellationToken cts)
    {
        var notes = await _noteService.GetSortedAsync(noteCallBackData.User.Id, noteCallBackData.Desc);
        await client.SendMessage(chatId, "text", replyMarkup: await _replyMarkupBuilder.NotesMarkup(notes, 
            noteCallBackData.Emoji, noteCallBackData.CallBackCommand, noteCallBackData.User), cancellationToken:cts);
    }

    private async Task HandleDeleteNoteAsync(long chatId, int messageId, NoteCallBackData noteCallBackData, ITelegramBotClient client, CancellationToken cts)
    {
        var response = await _noteService.DeleteNote(noteCallBackData.User.Id, noteCallBackData.NoteId)
            ? "<b>✅ Note deleted successfully.</b>"
            : "<b>❌ Failed to delete the note.</b>";
        await client.EditMessageText(chatId, messageId, response, ParseMode.Html, cancellationToken: cts);
    }

    private async Task HandleInfoNoteAsync(long chatId, int messageId, NoteCallBackData noteCallBackData, ITelegramBotClient client, CancellationToken cts)
    {
        var note = await _noteService.GetNote(noteCallBackData.User.Id, noteCallBackData.NoteId);

        var tags = note?.NoteTags
            .Select(n => n.Tag.Name)
            .ToList();

        var formattedTags = tags is { Count: > 0 }
            ? $"🏷 <i>Tags:</i> {string.Join(", ", tags)}"
            : "🏷 <i>Tags:</i> <code>None</code>";

        var formattedNote = $"""
                             <b>📝 {note.Title}</b>

                             <i>{note.Text}</i>

                             <i>🗓 Created: {note.CreatedAt:dd MMM yyyy, HH:mm}</i>

                             {formattedTags}
                             """;
        await client.EditMessageText(chatId, messageId, formattedNote, ParseMode.Html,
            cancellationToken: cts);
    }

    private async Task HandleTagDeleteAsync(long chatId, int messageId, NoteCallBackData noteCallBackData, ITelegramBotClient client, CancellationToken cts)
    {
        var response = await _tagService.DeleteTag(noteCallBackData.NoteId, noteCallBackData.User.Id)
            ? "<b>✅ Tag successfully deleted.</b>"
            : "<b>❌ Failed to delete tag.</b>";
        await client.EditMessageText(chatId, messageId, response, ParseMode.Html, cancellationToken: cts);
    }

    private async Task HandleSelectTagAsync(long chatId, int messageId, NoteCallBackData noteCallBackData, ITelegramBotClient client, CancellationToken cts)
    {
        var state = _userSessionService.GetOrCreate(noteCallBackData.User.Id);
        var tag = await _tagService.GetTagAsync(noteCallBackData.User.Id, noteCallBackData.NoteId);
        if (tag == null) return;

        if (state.SelectedTags.All(t => t.Id != tag.Id))
            state.SelectedTags.Push(tag);

        await _tagHelperService.TryAddTagToNoteAsync(client, chatId, noteCallBackData.User, state, cts);
    }

    private async Task HandleFilterByTagAsync(long chatId, int messageId, NoteCallBackData noteCallBackData, ITelegramBotClient client, CancellationToken cts)
    {
        var notes = await _noteService.GetNoteByTagAsync(noteCallBackData.User.Id, noteCallBackData.NoteId);
        if (!notes.Any())
        {
            await client.EditMessageText(chatId, messageId, "<b>No notes found with this tag.</b>",
                ParseMode.Html, cancellationToken: cts);
            return;
        }
        await client.EditMessageText(chatId, messageId, $"🔍 Found {notes.Count} note(s) with this tag:",
            ParseMode.Html, replyMarkup: await _replyMarkupBuilder.NotesMarkup(notes, BotCommandEmojis.I, CallBackCommands.Info, noteCallBackData.User),
            cancellationToken: cts);
    }
}
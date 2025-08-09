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
    private readonly Dictionary<string, Func<CallBackDataContext, Task>>
        _handlers;
    
    public CallbackHandler(
        NoteService noteService, 
        UserSessionService userSessionService, 
        TagService tagService, 
        TagHelperService tagHelperService, 
        ReplyMarkupBuilder replyMarkupBuilder, 
        RedisCallBackStorage redisCallBackStorage)
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
        

        var cbData = await _redisCallBackStorage.GetCallBackAsync(callbackQuery.Data);

        if (cbData == null)
        {
            await client.AnswerCallbackQuery(callbackQuery.Id, "Command not found or out of date ❌", cancellationToken: cts);
            return;
        }

        if (_handlers.TryGetValue(cbData.CallBackCommand, out var handler))
            await handler (new CallBackDataContext(chatId, messageId, cbData, client, cts));
    }

    private async Task HandleSortNoteAsync(CallBackDataContext callBackDataContext)
    {
        var notes = await _noteService.GetSortedAsync(callBackDataContext.callBackData.User.Id, callBackDataContext.callBackData.Desc);
        await callBackDataContext.client.EditMessageReplyMarkup(callBackDataContext.chatId, callBackDataContext.messageId, replyMarkup: await _replyMarkupBuilder.NotesMarkup(notes, 
            callBackDataContext.callBackData.Emoji, callBackDataContext.callBackData.ReservedCommand, callBackDataContext.callBackData.User), cancellationToken:callBackDataContext.cts);
    }

    private async Task HandleDeleteNoteAsync(CallBackDataContext callBackDataContext)
    {
        var response = await _noteService.DeleteNote(callBackDataContext.callBackData.User.Id, callBackDataContext.callBackData.ParsedId)
            ? "<b>✅ Note deleted successfully.</b>"
            : "<b>❌ Failed to delete the note.</b>";
        await callBackDataContext.client.EditMessageText(callBackDataContext.chatId, callBackDataContext.messageId, response, ParseMode.Html, cancellationToken: callBackDataContext.cts);
    }

    private async Task HandleInfoNoteAsync(CallBackDataContext callBackDataContext)
    {
        var note = await _noteService.GetNote(callBackDataContext.callBackData.User.Id, callBackDataContext.callBackData.ParsedId);

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
        await callBackDataContext.client.EditMessageText(callBackDataContext.chatId, callBackDataContext.messageId, formattedNote, ParseMode.Html,
            cancellationToken: callBackDataContext.cts);
    }

    private async Task HandleTagDeleteAsync(CallBackDataContext callBackDataContext)
    {
        var response = await _tagService.DeleteTag(callBackDataContext.callBackData.ParsedId, callBackDataContext.callBackData.User.Id)
            ? "<b>✅ Tag successfully deleted.</b>"
            : "<b>❌ Failed to delete tag.</b>";
        await callBackDataContext.client.EditMessageText(callBackDataContext.chatId, callBackDataContext.messageId, response, ParseMode.Html, cancellationToken: callBackDataContext.cts);
    }

    private async Task HandleSelectTagAsync(CallBackDataContext callBackDataContext)
    {
        var state = _userSessionService.GetOrCreate(callBackDataContext.callBackData.User.Id);
        var tag = await _tagService.GetTagAsync(callBackDataContext.callBackData.User.Id, callBackDataContext.callBackData.ParsedId);
        if (tag == null) return;

        if (state.SelectedTags.All(t => t.Id != tag.Id))
            state.SelectedTags.Push(tag);

        await _tagHelperService.TryAddTagToNoteAsync(callBackDataContext.client, callBackDataContext.chatId, callBackDataContext.callBackData.User, state, callBackDataContext.cts);
    }

    private async Task HandleFilterByTagAsync(CallBackDataContext callBackDataContext)
    {
        var notes = await _noteService.GetNoteByTagAsync(callBackDataContext.callBackData.User.Id, callBackDataContext.callBackData.ParsedId);
        if (!notes.Any())
        {
            await callBackDataContext.client.EditMessageText(callBackDataContext.chatId, callBackDataContext.messageId, "<b>No notes found with this tag.</b>",
                ParseMode.Html, cancellationToken: callBackDataContext.cts);
            return;
        }
        await callBackDataContext.client.EditMessageText(callBackDataContext.chatId, callBackDataContext.messageId, $"🔍 Found {notes.Count} note(s) with this tag:",
            ParseMode.Html, replyMarkup: await _replyMarkupBuilder.NotesMarkup(notes, BotCommandEmojis.I, CallBackCommands.Info, callBackDataContext.callBackData.User),
            cancellationToken: callBackDataContext.cts);
    }
    
    private record CallBackDataContext(
        long chatId,
        int messageId,
        CallBackData callBackData,
        ITelegramBotClient client,
        CancellationToken cts);
    
}
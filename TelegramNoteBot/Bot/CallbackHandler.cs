using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Enums;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class CallbackHandler
{

    private readonly NoteService _noteService;
    private readonly UserSessionService _userSessionService;
    private readonly TagService _tagService;
    private readonly TagHelperService _tagHelperService;
    private readonly Dictionary<string, Func<long, int, User, int, ITelegramBotClient, CancellationToken, Task>>
        _handlers;
    
    public CallbackHandler(NoteService noteService, UserSessionService userSessionService, TagService tagService, TagHelperService tagHelperService)
    {
        _noteService = noteService;
        _userSessionService = userSessionService;
        _tagService = tagService;
        _tagHelperService = tagHelperService;

        _handlers = new()
        {
            [CallBackCommands.Delete] = HandleDeleteNoteAsync,
            [CallBackCommands.Info] = HandleInfoNoteAsync,
            [CallBackCommands.FilterByTag] = HandleFilterByTagAsync,
            [CallBackCommands.SelectTag] = HandleSelectTagAsync,
            [CallBackCommands.TagDelete] = HandleTagDeleteAsync
        };
    }

    
    public async Task HandleUpdateAsync(ITelegramBotClient client, CallbackQuery callbackQuery, CancellationToken cts)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var user = callbackQuery.From;

        var parts = callbackQuery.Data?.Split("|");
        var command = parts?.ElementAtOrDefault(0);
        var idStr = parts?.ElementAtOrDefault(1);
        
        if (!int.TryParse(idStr, out var parsedId) || command == null)
            return;

        if (_handlers.TryGetValue(command, out var handler))
        {
            await handler (chatId, messageId, user, parsedId,client, cts);
        }
    }

    private async Task HandleDeleteNoteAsync(long chatId, int messageId, User user, int noteIdToDelete, ITelegramBotClient client, CancellationToken cts)
    {
        var response = await _noteService.DeleteNote(user.Id, noteIdToDelete)
            ? "<b>✅ Note deleted successfully.</b>"
            : "<b>❌ Failed to delete the note.</b>";
        await client.EditMessageText(chatId, messageId, response, ParseMode.Html, cancellationToken: cts);
    }

    private async Task HandleInfoNoteAsync(long chatId, int messageId, User user, int noteIdToShow, ITelegramBotClient client, CancellationToken cts)
    {
        var note = await _noteService.GetNote(user.Id, noteIdToShow);

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

    private async Task HandleTagDeleteAsync(long chatId, int messageId, User user, int tagIdToDelete, ITelegramBotClient client, CancellationToken cts)
    {
        var response = await _tagService.DeleteTag(tagIdToDelete, user.Id)
            ? "<b>✅ Tag successfully deleted.</b>"
            : "<b>❌ Failed to delete tag.</b>";
        await client.EditMessageText(chatId, messageId, response, ParseMode.Html, cancellationToken: cts);
    }

    private async Task HandleSelectTagAsync(long chatId, int messageId, User user, int tagIdToAdd, ITelegramBotClient client, CancellationToken cts)
    {
        var state = _userSessionService.GetOrCreate(user.Id);
        var tag = await _tagService.GetTagAsync(user.Id, tagIdToAdd);
        if (tag == null) return;

        if (state.SelectedTags.All(t => t.Id != tag.Id))
            state.SelectedTags.Push(tag);

        await _tagHelperService.TryAddTagToNoteAsync(client, chatId, user, state, cts);
    }

    private async Task HandleFilterByTagAsync(long chatId, int messageId, User user, int tagId, ITelegramBotClient client, CancellationToken cts)
    {
        var notes = await _noteService.GetNoteByTagAsync(user.Id, tagId);
        if (!notes.Any())
        {
            await client.EditMessageText(chatId, messageId, "<b>No notes found with this tag.</b>",
                ParseMode.Html, cancellationToken: cts);
            return;
        }
        await client.EditMessageText(chatId, messageId, $"🔍 Found {notes.Count} note(s) with this tag:",
            ParseMode.Html, replyMarkup: ReplyMarkupBuilder.NotesMarkup(notes, BotCommandEmojis.I, CallBackCommands.Info),
            cancellationToken: cts);
    }

}
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Enums;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class CallbackHandler(
    NoteService noteService,
    UserSessionService userSessionService,
    TagService tagService,
    NoteTagService noteTagService)
{
    public async Task HandleUpdateAsync(ITelegramBotClient client, CallbackQuery callbackQuery, CancellationToken cts)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var user = callbackQuery.From;

        var state = userSessionService.GetOrCreate(user.Id);

        var parts = callbackQuery.Data?.Split("|");
        var command = parts?.ElementAtOrDefault(0);
        var idStr = parts?.ElementAtOrDefault(1);

        switch (command)
        {
            case CallBackCommands.Delete:
                if (int.TryParse(idStr, out var noteIdToDelete))
                {
                    var response = await noteService.DeleteNote(user.Id, noteIdToDelete)
                        ? "<b>✅ Note deleted successfully.</b>"
                        : "<b>❌ Failed to delete the note.</b>";
                    await client.EditMessageText(chatId, messageId, response, ParseMode.Html, cancellationToken: cts);
                }

                break;

            case CallBackCommands.Info:
                if (int.TryParse(idStr, out var noteIdToShow))
                {
                    var note = await noteService.GetNote(user.Id, noteIdToShow);
                    
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

                break;

            case CallBackCommands.TagDelete:
                if (int.TryParse(idStr, out var tagIdToDelete))
                {
                    var response = await tagService.DeleteTag(tagIdToDelete, user.Id)
                        ? "<b>✅ Tag successfully deleted.</b>"
                        : "<b>❌ Failed to delete tag.</b>";
                    await client.EditMessageText(chatId, messageId, response, ParseMode.Html, cancellationToken: cts);
                }

                break;

            case CallBackCommands.SelectTag:
                if (int.TryParse(idStr, out var tagIdToAdd))
                {
                    var tag = await tagService.GetTagAsync(user.Id, tagIdToAdd);
                    if (tag == null) return;

                    if (state.SelectedTags.All(t => t.Id != tag.Id))
                        state.SelectedTags.Push(tag);

                    if (state.SelectedTags.Count == 3)
                    {
                        await noteTagService.AddNoteTagAsync(state.LastAddedNoteId, state.SelectedTags.Peek().Id);
                        await client.SendMessage(chatId, "<b>✅ Max 3 tags selected. Saving note...</b>",
                            ParseMode.Html, replyMarkup: ReplyMarkupBuilder.MainMenu(), cancellationToken: cts);
                        userSessionService.Clear(user.Id);
                    }
                    else
                    {
                        var remaining = 3 - state.SelectedTags.Count;

                        await noteTagService.AddNoteTagAsync(state.LastAddedNoteId, state.SelectedTags.Peek().Id);

                        await client.SendMessage(chatId, $"✅ Tag added. You can select {remaining} more.",
                            ParseMode.Html, cancellationToken: cts);
                    }
                }

                break;
        }
    }
}
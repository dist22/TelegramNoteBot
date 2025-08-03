using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Bot;
using TelegramNoteBot.Constants;

namespace TelegramNoteBot.Services;

public class NoteDisplayService(NoteService noteService)
{
    public async Task SendNotesListAsync(ITelegramBotClient client, User user, long chatId, string emoji,
        string callBackCommand, CancellationToken cts)
    {
        var notes = await noteService.GetNotes(user.Id);
        if (!notes.Any())
        {
            await client.SendMessage(chatId, "<b>📭 No notes found.</b>\nTry creating one using the menu!",
                ParseMode.Html, cancellationToken: cts);
            return;
        }

        await client.SendMessage(chatId, $"<b>{emoji} Your notes:</b>\nSelect a note to view details.",
            replyMarkup: ReplyMarkupBuilder.NotesMarkup(notes, emoji, callBackCommand), parseMode: ParseMode.Html,
            cancellationToken: cts);
    }

    public async Task SendSearchedNoteListAsync(ITelegramBotClient client, User user, long chatId, string text,
        CancellationToken cts)
    {
        var notesList = await noteService.SearchNotes(user.Id, text.Trim());
        if (!notesList.Any())
        {
            await client.SendMessage(chatId, $"<b>😕 No results</b>\nNo notes found for <i>\"{text}\"</i>.",
                ParseMode.Html, cancellationToken: cts);
            return;
        }

        var response = $"<b>🔍 Found {notesList.Count} note(s)</b> for <i>\"{text}\"</i>:\nSelect one to view.";
        await client.SendMessage(chatId, response,
            replyMarkup: ReplyMarkupBuilder.NotesMarkup(notesList, BotCommandEmojis.I, CallBackCommands.Info),
            parseMode: ParseMode.Html,
            cancellationToken: cts);
    }
}
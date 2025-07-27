using Telegram.Bot;
using Telegram.Bot.Types;
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
            await client.SendMessage(chatId, "Sorry, no notes found.", cancellationToken: cts);
            return;
        }
        await client.SendMessage(chatId, "Notes: ", 
            replyMarkup: ReplyMarkupBuilder.NotesMarkup(notes,emoji, callBackCommand ), 
            cancellationToken: cts);
    }

    public async Task SendSearchedNoteListAsync(ITelegramBotClient client, User user, long chatId, string text, string emoji, CancellationToken cts)
    {
        var notesList = await noteService.SearchNotes(user.Id, text.Trim());
        if (!notesList.Any())
        {
            await client.SendMessage(chatId, "Empty", cancellationToken: cts);
            return;
        }
        var answer = $"🔍 Found {notesList.Count} note(s) for \"{text}\":";
        await client.SendMessage(chatId, answer, replyMarkup: ReplyMarkupBuilder.NotesMarkup(notesList,"➕", CallBackCommands.Info),
            cancellationToken: cts);
    }
}
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class MessageHandler(NoteService noteService, UserSessionService userSessionService)
{
    public async Task HandleUpdateAsync(ITelegramBotClient client, Message message, CancellationToken cts)
    {
        if (message.Text is not { } text) return;

        var user = message.From;
        var chatId = message.Chat.Id;
        var state = userSessionService.GetOrCreate(user.Id);


        switch (text)
        {
            case BotCommands.Start :
                await client.SendMessage(chatId, "WELCOME", replyMarkup: ReplyMarkupBuilder.MainMenu(), cancellationToken: cts);
                break;
            case BotCommands.AddNote:
                state.PendingTitle = null;
                await client.SendMessage(chatId, "Enter note title: ", cancellationToken: cts);
                break;
            case BotCommands.MyNotes:
                await SendNotesListAsync(client, user,chatId,  "➕", CallBackCommands.Info, cts);
                break;
            case BotCommands.SearchNote:
                state.IsSearching = true;
                await client.SendMessage(chatId, "🔍 Enter search query:", cancellationToken: cts);
                break;
            case BotCommands.DeleteNote:
                await SendNotesListAsync(client, user,chatId,  "❌", CallBackCommands.Delete, cts);
                break;
            case BotCommands.AboutDeveloper:
                await client.SendMessage(chatId, "Tg NoteBot v.01", ParseMode.Html, protectContent: true,
                    replyMarkup: ReplyMarkupBuilder.AboutDeveloper(), cancellationToken: cts);
                break;
            default:
                if (state.PendingTitle == null)
                {
                    state.PendingTitle = text;
                    await client.SendMessage(chatId, "Enter the note text", cancellationToken: cts);
                }
                else if (state.IsSearching)
                {
                    var notesList = await noteService.SearchNotes(user.Id, text.Trim());
                    if (!notesList.Any())
                    {
                        await client.SendMessage(chatId, "Empty", cancellationToken: cts);
                    }
                    var answer = $"🔍 Found {notesList.Count} note(s) for \"{text}\":";
                    await client.SendMessage(chatId, answer, replyMarkup: ReplyMarkupBuilder.NotesMarkup(notesList,"➕", CallBackCommands.Info),
                        cancellationToken: cts);
                    
                }
                else
                {
                    var title = state.PendingTitle;
                    await noteService.AddNote(user.Id, user.Username, title, text);

                    userSessionService.Clear(user.Id);
                    await client.SendMessage(chatId, "Note added", cancellationToken: cts);
                }
                break;

        }

    }

    private async Task SendNotesListAsync(ITelegramBotClient client, User user, long chatId, string emoji,
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
}
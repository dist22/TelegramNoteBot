using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Services;
using TelegramNoteBot.Enums;

namespace TelegramNoteBot.Bot;

public class MessageHandler(NoteService noteService, UserSessionService userSessionService, NoteDisplayService noteDisplayService)
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
                state.State = BotUserState.EnteringNoteTitle;
                await client.SendMessage(chatId, "Enter note title: ", cancellationToken: cts);
                break;
            case BotCommands.MyNotes:
                await noteDisplayService.SendNotesListAsync(client, user, chatId, BotCommandEmojis.I, CallBackCommands.Info, cts);
                break;
            case BotCommands.SearchNote:
                state.State = BotUserState.EnteringSearchingQuery;
                await client.SendMessage(chatId, "🔍 Enter search query:", cancellationToken: cts);
                break;
            case BotCommands.DeleteNote:
                await noteDisplayService.SendNotesListAsync(client,user, chatId, BotCommandEmojis.X, CallBackCommands.Delete, cts);
                break;
            
            case BotCommands.FilterByTag:
                break;
            
            case BotCommands.ManageTags:
                await client.SendMessage(chatId, "Tag management menu:", replyMarkup: ReplyMarkupBuilder.TagManagementMenu(),  cancellationToken: cts);
                break;
            
            case BotCommands.AboutDeveloper:
                await client.SendMessage(chatId, "Tg NoteBot v.01.3", ParseMode.Html, protectContent: true,
                    replyMarkup: ReplyMarkupBuilder.AboutDeveloper(), cancellationToken: cts);
                break;
            default:
                await HandleTextInputAsync(client, user, chatId, text, state, userSessionService, cts);
                break;
        }

    }

    private async Task HandleTextInputAsync(ITelegramBotClient client, User user, long chatId, string text, UserNoteState state, UserSessionService sessionService,
        CancellationToken cts)
    {

        switch (state.State)
        {
            case BotUserState.EnteringNoteTitle:
                state.PendingTitle = text;
                await client.SendMessage(chatId, "Enter the note text", cancellationToken: cts);
                state.State = BotUserState.EnteringNoteText;
                break;
            
            case BotUserState.EnteringNoteText:
                var title =  state.PendingTitle;
                await noteService.AddNote(user.Id, user.Username, title, text);
                sessionService.Clear(user.Id);
                await client.SendMessage(chatId, "Note added", cancellationToken: cts);
                break;
            
            case BotUserState.EnteringSearchingQuery:
                await noteDisplayService.SendSearchedNoteListAsync(client, user, chatId, BotCommandEmojis.I, CallBackCommands.Info, cts);
                sessionService.Clear(user.Id);
                break;
            
            case BotUserState.None:
                await client.SendMessage(chatId, "Unknow command", replyMarkup: ReplyMarkupBuilder.MainMenu(),
                    cancellationToken: cts);
                break;
        }
    }

}
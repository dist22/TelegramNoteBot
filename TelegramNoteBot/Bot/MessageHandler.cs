using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Services;
using TelegramNoteBot.Enums;

namespace TelegramNoteBot.Bot;

public class MessageHandler(
    NoteService noteService,
    UserSessionService userSessionService,
    NoteDisplayService noteDisplayService,
    TagCommandHandler tagCommandHandler,
    AddTagToNoteCommandHandler addTagToNoteCommandHandler)
{
    public async Task HandleUpdateAsync(ITelegramBotClient client, Message message, CancellationToken cts)
    {
        if (message.Text is not { } text) return;

        var user = message.From;
        var chatId = message.Chat.Id;
        var state = userSessionService.GetOrCreate(user.Id);

        switch (text)
        {
            case BotCommands.Start:
                await client.SendMessage(chatId,
                    "<b>👋 Welcome to NoteBot!</b>\n\nUse the menu below to manage your notes, tags, and more.",
                    replyMarkup: ReplyMarkupBuilder.MainMenu(), parseMode: ParseMode.Html, cancellationToken: cts);
                break;
            case BotCommands.AddNote:
                state.State = BotUserState.EnteringNoteTitle;
                await client.SendMessage(chatId, "<b>📝 Please enter the title for your new note:</b>", ParseMode.Html,
                    cancellationToken: cts);
                break;
            case BotCommands.MyNotes:
                await noteDisplayService.SendNotesListAsync(client, user, chatId, BotCommandEmojis.I,
                    CallBackCommands.Info, cts);
                break;
            case BotCommands.SearchNote:
                state.State = BotUserState.EnteringSearchingQuery;
                await client.SendMessage(chatId, "<b>🔍 Enter a keyword to search your notes:</b>", ParseMode.Html,
                    cancellationToken: cts);
                break;
            case BotCommands.DeleteNote:
                await noteDisplayService.SendNotesListAsync(client, user, chatId, BotCommandEmojis.X,
                    CallBackCommands.Delete, cts);
                break;

            case BotCommands.FilterByTag:
                break;

            case BotCommands.ManageTags:
                state.State = BotUserState.TagManagement;
                await client.SendMessage(chatId, "<b>Tag management menu</b>",
                    replyMarkup: ReplyMarkupBuilder.TagManagementMenu(), parseMode: ParseMode.Html,
                    cancellationToken: cts);
                break;

            case BotTagCommands.AddTags:
            case BotTagCommands.RemoveTags:
            case BotTagCommands.Tags:
            case BotTagCommands.Back:
                await tagCommandHandler.HandleCommandAsync(client, chatId, text, user, cts);
                break;
            
            case AddTagToNoteCommands.CreateAndJoin:
            case AddTagToNoteCommands.JoinTag:
            case AddTagToNoteCommands.Skip:
                await addTagToNoteCommandHandler.HandleCommandAsync(client, chatId, text, user,state, cts);
                break;

            case BotCommands.AboutDeveloper:
                await client.SendMessage(chatId, "<b>Tg NoteBot v.01.5_tag</b>", ParseMode.Html, protectContent: true,
                    replyMarkup: ReplyMarkupBuilder.AboutDeveloper(), cancellationToken: cts);
                break;

            default:
                if (state.State == BotUserState.None)
                    await client.SendMessage(chatId, "<b>❗ Unknown command</b>\nPlease use the menu below.",
                        ParseMode.Html,
                        replyMarkup: ReplyMarkupBuilder.MainMenu(),
                        cancellationToken: cts);
                else
                    await HandleTextInputAsync(client, user, chatId, text, state, userSessionService, cts);
                break;
        }
    }

    private async Task HandleTextInputAsync(ITelegramBotClient client, User user, long chatId, string text,
        UserNoteState state, UserSessionService sessionService,
        CancellationToken cts)
    {
        switch (state.State)
        {
            case BotUserState.EnteringNoteTitle:
                state.PendingTitle = text;
                await client.SendMessage(chatId, "<b>✏️ Now enter the content of your note:</b>", ParseMode.Html,
                    cancellationToken: cts);
                state.State = BotUserState.EnteringNoteText;
                break;

            case BotUserState.EnteringNoteText:
                var title = state.PendingTitle;
                state.LastAddedNoteId = await noteService.AddNote(user.Id, user.Username, title, text);
                await client.SendMessage(chatId, "<b>✅ Your note has been added successfully!</b>" +
                                                 "\n<i>Do you want to add tag?</i>", ParseMode.Html, replyMarkup: ReplyMarkupBuilder.AddTagToNoteMenu(),
                    cancellationToken: cts);
                break;

            case BotUserState.EnteringSearchingQuery:
                await noteDisplayService.SendSearchedNoteListAsync(client, user, chatId, text, cts);
                sessionService.Clear(user.Id);
                break;
            
            case BotUserState.CreatingTag:
            case BotUserState.AddingTag:
                await tagCommandHandler.HandleTextTagInputAsync(client, text, chatId, user, state, cts);
                break;
        }
    }
}
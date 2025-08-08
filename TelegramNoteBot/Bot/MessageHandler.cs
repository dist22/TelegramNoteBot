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
    TagService tagService,
    TagHelperService tagHelperService, 
    ReplyMarkupBuilder replyMarkupBuilder)
{
    private readonly Dictionary<string, Func<ITelegramBotClient, long, User, CancellationToken, Task>>
        _commandHandlers = new();

    public async Task HandleUpdateAsync(ITelegramBotClient client, Message message, CancellationToken cts)
    {
        if (message.Text is not { } text) return;

        var user = message.From;
        var chatId = message.Chat.Id;
        var state = userSessionService.GetOrCreate(user.Id);

        if (!_commandHandlers.Any())
            InitializeCommandHandlers(state);

        if (_commandHandlers.TryGetValue(text, out var handler))
        {
            await handler(client, chatId, user, cts);
        }
        else if (IsTagCommand(text))
        {
            await tagCommandHandler.HandleCommandAsync(client, chatId, text, user, cts);
        }
        else if (state.State == BotUserState.None)
        {
            await client.SendMessage(chatId, "<b>❗ Unknown command</b>\nPlease use the menu below.",
                ParseMode.Html,
                replyMarkup: replyMarkupBuilder.MainMenu(),
                cancellationToken: cts);
        }
        else
        {
            await HandleTextInputAsync(client, user, chatId, text, state, cts);
        }
    }

    private void InitializeCommandHandlers(UserNoteState state)
    {
        _commandHandlers[BotCommands.Start] = async (client, chatId, user, cts) =>
        {
            await client.SendMessage(chatId,
                "<b>👋 Welcome to NoteBot!</b>\n\nUse the menu below to manage your notes, tags, and more.",
                replyMarkup: replyMarkupBuilder.MainMenu(), parseMode: ParseMode.Html, cancellationToken: cts);
        };

        _commandHandlers[BotCommands.AddNote] = async (client, chatId, user, cts) =>
        {
            state.State = BotUserState.EnteringNoteTitle;
            await client.SendMessage(chatId, "<b>📝 Please enter the title for your new note:</b>", ParseMode.Html,
                cancellationToken: cts);
        };

        _commandHandlers[BotCommands.MyNotes] = async (client, chatId, user, cts) =>
        {
            await noteDisplayService.SendNotesListAsync(client, user, chatId, BotCommandEmojis.I,
                CallBackCommands.Info, cts);
        };

        _commandHandlers[BotCommands.SearchNote] = async (client, chatId, user, cts) =>
        {
            state.State = BotUserState.EnteringSearchingQuery;
            await client.SendMessage(chatId, "<b>🔍 Enter a keyword to search your notes:</b>", ParseMode.Html,
                cancellationToken: cts);
        };

        _commandHandlers[BotCommands.DeleteNote] = async (client, chatId, user, cts) =>
        {
            await noteDisplayService.SendNotesListAsync(client, user, chatId, BotCommandEmojis.X,
                CallBackCommands.Delete, cts);
        };

        _commandHandlers[BotCommands.FilterByTag] = async (client, chatId, user, cts) =>
        {
            var tags = await tagService.GetAllAsync(user.Id);
            var messageText = "🧩 Choose a tag to filter notes:";

            await tagHelperService.TrySendTagMarkup(client, user, chatId, messageText, tags, BotCommandEmojis.I,
                CallBackCommands.FilterByTag, replyMarkupBuilder.MainMenu(), cts);
        };

        _commandHandlers[BotCommands.AboutDeveloper] = async (client, chatId, user, cts) =>
        {
            await client.SendMessage(chatId, "<b>Tg NoteBot: v.01.7_tag_finally</b>", ParseMode.Html,
                protectContent: true,
                replyMarkup: replyMarkupBuilder.AboutDeveloper(), cancellationToken: cts);
        };

        _commandHandlers[BotCommands.ManageTags] = async (client, chatId, user, cts) =>
        {
            await client.SendMessage(chatId, "<b>Tag management menu</b>",
                replyMarkup: replyMarkupBuilder.TagManagementMenu(), parseMode: ParseMode.Html,
                cancellationToken: cts);
        };
    }

    private bool IsTagCommand(string text)
        => text is BotTagCommands.AddTags
            or BotTagCommands.RemoveTags
            or BotTagCommands.Tags
            or BotTagCommands.Back
            or AddTagToNoteCommands.CreateAndJoin
            or AddTagToNoteCommands.JoinTag
            or AddTagToNoteCommands.Skip;

    private async Task HandleTextInputAsync(ITelegramBotClient client, User user, long chatId, string text,
        UserNoteState state, CancellationToken cts)
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
                state.State = BotUserState.None;
                await client.SendMessage(chatId, "<b>✅ Your note has been added successfully!</b>" +
                                                 "\n<i>Do you want to add tag?</i>", ParseMode.Html,
                    replyMarkup: replyMarkupBuilder.AddTagToNoteMenu(),
                    cancellationToken: cts);
                break;

            case BotUserState.EnteringSearchingQuery:
                await noteDisplayService.SendSearchedNoteListAsync(client, user, chatId, text, cts);
                userSessionService.Clear(user.Id);
                break;

            case BotUserState.CreatingTag:
            case BotUserState.AddingTag:
                await tagCommandHandler.HandleTextTagInputAsync(client, text, chatId, user, state, cts);
                break;
        }
    }
}
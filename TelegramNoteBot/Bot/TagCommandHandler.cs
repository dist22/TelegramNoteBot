using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Enums;
using TelegramNoteBot.Models;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class TagCommandHandler(
    TagService tagService,
    UserSessionService userSessionService,
    TagHelperService tagHelperService,
    ReplyMarkupBuilder replyMarkupBuilder)
{
    private readonly Dictionary<string, Func<ITelegramBotClient, long, User, CancellationToken, Task>>
        _handlers = new();

    public async Task HandleCommandAsync(ITelegramBotClient client, long chatId, string text, User user,
        CancellationToken cts)
    {
        
        var tags = await tagService.GetAllAsync(user.Id);
        var state = userSessionService.GetOrCreate(user.Id);
        
        if(!_handlers.Any())
            InitializeCommandHandlers(state, tags);

        if (_handlers.TryGetValue(text, out var handler))
            await handler(client, chatId, user, cts);
    }

    private void InitializeCommandHandlers(UserNoteState state, List<Tag> tags)
    {
        _handlers[BotTagCommands.AddTags] = async (client, chatId, user, cts) =>
        {
            await tagHelperService.SendStartCreatTagMessageAsync(client, chatId, state, BotUserState.AddingTag, cts);
        };
        _handlers[BotTagCommands.Tags] = async (client, chatId, user, cts) =>
        {

            var mes = tags.Any()
                ? string.Join(", ", tags.Select(t => t.Name))
                : "😕 <i>You don't have any tags yet.</i>";
            await client.SendMessage(chatId, $"<b>🏷 Your tags:</b>\n\n{mes}", ParseMode.Html,
                cancellationToken: cts);
        };
        _handlers[BotTagCommands.Back] = async (client, chatId, user, cts) =>
        {
            
            await client.SendMessage(chatId, "<b>🔙 Returned to main menu</b>",
                replyMarkup: replyMarkupBuilder.MainMenu(), parseMode: ParseMode.Html, cancellationToken: cts);
            state.State = BotUserState.None;
        };
        _handlers[BotTagCommands.RemoveTags] = async (client, chatId, user, cts) =>
        {
            await tagHelperService.TrySendTagMarkup(client, user, chatId, "<b>🗑 Select tag to remove:</b>", tags,
                BotCommandEmojis.X, CallBackCommands.TagDelete, replyMarkupBuilder.TagManagementMenu(), cts);
        };
        _handlers[AddTagToNoteCommands.Skip] = async (client, chatId, user, cts) =>
        {
            await client.SendMessage(chatId, "<b>🔙 Returned to main menu</b>", ParseMode.Html
                , replyMarkup: replyMarkupBuilder.MainMenu(), cancellationToken: cts);
            userSessionService.Clear(user.Id);
        };
        _handlers[AddTagToNoteCommands.JoinTag] = async (client, chatId, user, cts) =>
        {

            await tagHelperService.TrySendTagMarkup(client, user, chatId, "<b>Select up to 3 tags:</b>", tags,
                BotCommandEmojis.WhiteCheckMark, CallBackCommands.SelectTag, replyMarkupBuilder.AddTagToNoteMenu(),
                cts);
        };
        _handlers[AddTagToNoteCommands.CreateAndJoin] = async (client, chatId, user, cts) =>
        {
            
            await tagHelperService.SendStartCreatTagMessageAsync(client, chatId, state, BotUserState.CreatingTag,
                cts);
        };
    }

    public async Task HandleTextTagInputAsync(ITelegramBotClient client, string text, long chatId, User user,
        UserNoteState state, CancellationToken cts)
    {
        switch (state.State)
        {
            case BotUserState.AddingTag:
                await tagHelperService.TryCreateTagAsync(client, chatId, text, user, state, cts);
                await client.SendMessage(chatId, "<b>Tag added ✅</b>", ParseMode.Html, cancellationToken: cts);
                break;

            case BotUserState.CreatingTag:
                await tagHelperService.TryCreateTagAsync(client, chatId, text, user, state, cts);
                await tagHelperService.TryAddTagToNoteAsync(client, chatId, user, state, cts);
                break;
        }
    }
}
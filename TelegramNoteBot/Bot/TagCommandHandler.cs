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
    TagHelperService tagHelperService)
{
    public async Task HandleCommandAsync(ITelegramBotClient client, long chatId, string text, User user,
        CancellationToken cts)
    {
        var state = userSessionService.GetOrCreate(user.Id);
        var tags = await tagService.GetAllAsync(user.Id);

        switch (text)
        {
            case AddTagToNoteCommands.CreateAndJoin:
                await tagHelperService.SendStartCreatTagMessageAsync(client, chatId,state, BotUserState.CreatingTag, cts);
                break;
            case BotTagCommands.AddTags:
                await tagHelperService.SendStartCreatTagMessageAsync(client, chatId,state, BotUserState.AddingTag, cts);
                break;
            
            case AddTagToNoteCommands.JoinTag:

                var messageTextForJoinTag = "<b>Select up to 3 tags:</b>";
                await tagHelperService.TrySendTagMarkup(client, user, chatId, messageTextForJoinTag, tags,
                    BotCommandEmojis.WhiteCheckMark, CallBackCommands.SelectTag,ReplyMarkupBuilder.AddTagToNoteMenu(), cts);
                break;

            case BotTagCommands.Tags:
                var mes = tags.Any()
                    ? string.Join(", ", tags.Select(t => t.Name))
                    : "😕 <i>You don't have any tags yet.</i>";
                await client.SendMessage(chatId, $"<b>🏷 Your tags:</b>\n\n{mes}", ParseMode.Html,
                    cancellationToken: cts);
                break;

            case BotTagCommands.RemoveTags:

                var messageTextForRemovingTag = "<b>🗑 Select tag to remove:</b>";
                await tagHelperService.TrySendTagMarkup(client, user, chatId, messageTextForRemovingTag, tags,
                    BotCommandEmojis.X, CallBackCommands.TagDelete,ReplyMarkupBuilder.TagManagementMenu(), cts);
                break;

            case BotTagCommands.Back:
                await client.SendMessage(chatId, "<b>🔙 Returned to main menu</b>",
                    replyMarkup: ReplyMarkupBuilder.MainMenu(), parseMode: ParseMode.Html, cancellationToken: cts);
                state.State = BotUserState.None;
                break;
            
            case AddTagToNoteCommands.Skip:
                await client.SendMessage(chatId, "<b>🔙 Returned to main menu</b>", ParseMode.Html
                    , replyMarkup: ReplyMarkupBuilder.MainMenu(), cancellationToken: cts);
                userSessionService.Clear(user.Id);
                break;
        }
    }

    public async Task HandleTextTagInputAsync(ITelegramBotClient client, string text, long chatId, User user,
        UserNoteState state, CancellationToken cts)
    {
        switch (state.State)
        {
            case BotUserState.AddingTag:
                var messageTextforAddCreateTag = "<b>Tag added ✅</b>";
                await tagHelperService.TryCreateTagAsync(client, chatId, text, user, state, cts);
                await client.SendMessage(chatId, messageTextforAddCreateTag, ParseMode.Html, cancellationToken: cts);
                break;

            case BotUserState.CreatingTag:
                await tagHelperService.TryCreateTagAsync(client, chatId, text, user, state, cts);
                await tagHelperService.TryAddTagToNoteAsync(client, chatId, user, state, cts);
                break;
        }
    }
}
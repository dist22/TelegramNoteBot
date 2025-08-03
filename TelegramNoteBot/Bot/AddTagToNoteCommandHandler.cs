using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class AddTagToNoteCommandHandler(TagService tagService, UserSessionService userSessionService)
{
    public async Task HandleCommandAsync(ITelegramBotClient client, long chatId, string text, User user,
        CancellationToken cts)
    {
        switch (text)
        {
            case AddTagToNoteCommands.CreateAndJoin:
                await client.SendMessage(chatId, "<b>✍️ Enter tag name</b>\nIt must start with <code>#</code>",
                    ParseMode.Html, cancellationToken: cts);
                break;

            case AddTagToNoteCommands.JoinTag:
                var tags = await tagService.GetAllAsync(user.Id);
                if (!tags.Any())
                {
                    await client.SendMessage(chatId, "<b>You have no tags. Saving note without tags...</b>",
                        ParseMode.Html, replyMarkup: ReplyMarkupBuilder.MainMenu(), cancellationToken: cts);
                    userSessionService.Clear(user.Id);
                    return;
                }

                await client.SendMessage(chatId, "<b>Select up to 3 tags:</b>", ParseMode.Html,
                    replyMarkup: ReplyMarkupBuilder.TagMarkup(tags, "✅", CallBackCommands.SelectTag),
                    cancellationToken: cts);
                break;

            case AddTagToNoteCommands.Skip:
                await client.SendMessage(chatId, "<b>🔙 Returned to main menu</b>", ParseMode.Html
                    , replyMarkup: ReplyMarkupBuilder.MainMenu(), cancellationToken: cts);
                userSessionService.Clear(user.Id);
                break;
        }
    }
}
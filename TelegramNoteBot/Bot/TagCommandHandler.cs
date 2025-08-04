using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Enums;
using TelegramNoteBot.Models;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class TagCommandHandler(TagService tagService, UserSessionService userSessionService, NoteTagService noteTagService)
{
    public async Task HandleCommandAsync(ITelegramBotClient client, long chatId, string text, User user,
        CancellationToken cts)
    {
        var state = userSessionService.GetOrCreate(user.Id);
        var tags = await tagService.GetAllAsync(user.Id);

        switch (text)
        {
            case BotTagCommands.AddTags:
                state.State = BotUserState.AddingTag;
                await client.SendMessage(chatId, "<b>✍️ Enter tag name</b>\nIt must start with <code>#</code>"
                    , ParseMode.Html, cancellationToken: cts);
                break;

            case BotTagCommands.Tags:
                var mes = tags.Any()
                    ? string.Join(", ", tags.Select(t => t.Name))
                    : "😕 <i>You don't have any tags yet.</i>";
                await client.SendMessage(chatId, $"<b>🏷 Your tags:</b>\n\n{mes}", ParseMode.Html,
                    cancellationToken: cts);
                break;

            case BotTagCommands.RemoveTags:
                if (!tags.Any())
                {
                    await client.SendMessage(chatId, "😕 <i>You don't have any tags yet.</i>", ParseMode.Html,
                        cancellationToken: cts);
                    return;
                }

                await client.SendMessage(chatId, "<b>🗑 Select tag to remove:</b>",
                    replyMarkup: ReplyMarkupBuilder.TagMarkup(tags, BotCommandEmojis.X, CallBackCommands.TagDelete),
                    parseMode: ParseMode.Html, cancellationToken: cts);
                break;

            case BotTagCommands.Back:
                await client.SendMessage(chatId, "<b>🔙 Returned to main menu</b>",
                    replyMarkup: ReplyMarkupBuilder.MainMenu(), parseMode: ParseMode.Html, cancellationToken: cts);
                state.State = BotUserState.None;
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
                
                await AddValidTagAsync(client, text, chatId, user, state, cts);
                await client.SendMessage(chatId, messageTextforAddCreateTag, ParseMode.Html, cancellationToken: cts);
                break;
            
            case BotUserState.CreatingTag:
                
                await AddValidTagAsync(client, text, chatId, user, state, cts);
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
                break;
        }
    }

    private async Task AddValidTagAsync(ITelegramBotClient client, string text, long chatId, User user, UserNoteState state, CancellationToken cts)
    {
        if (!text.StartsWith("#"))
        {
            await client.SendMessage(chatId,
                "❌ <b>Invalid tag</b>. It must start with <code>#</code>. Try again.", ParseMode.Html,
                cancellationToken: cts);
            return;
        }

        if (await tagService.TagExist(text, user.Id))
        {
            await client.SendMessage(chatId,
                "⚠️ <i>This tag already exists.</i> Try again.", ParseMode.Html,
                cancellationToken: cts);
            return;
        }
        
        state.SelectedTags.Push(await tagService.AddTag(text, user.Id));
        state.State = BotUserState.None;
    }

}
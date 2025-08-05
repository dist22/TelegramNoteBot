using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNoteBot.Bot;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Enums;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Services;

public class TagHelperService(TagService tagService, UserSessionService userSessionService, NoteTagService noteTagService)
{

    public async Task TryCreateTagAsync(ITelegramBotClient client, long chatId, string text, User user, UserNoteState state,CancellationToken cts)
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

    public async Task TryAddTagToNoteAsync(ITelegramBotClient client,long chatId,User user, UserNoteState state, CancellationToken cts )
    {
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
    }

    public async Task TrySendTagMarkup(ITelegramBotClient client, long chatId, User user, List<Tag> tags,string emoji, string messageText, string callBackCommand, CancellationToken cts )
    {
        if (!tags.Any())
        {
            await client.SendMessage(chatId, "<b>😕 You don't have any tags yet.</b>",
                ParseMode.Html, replyMarkup: ReplyMarkupBuilder.MainMenu(), cancellationToken: cts);
            userSessionService.Clear(user.Id);
            return;
        }
        await client.SendMessage(chatId, messageText, ParseMode.Html,
            replyMarkup: ReplyMarkupBuilder.TagMarkup(tags, emoji, callBackCommand),
            cancellationToken: cts);
    }

}
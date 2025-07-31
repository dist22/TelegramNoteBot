using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Enums;
using TelegramNoteBot.Models;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class TagCommandHandler(TagService tagService, UserSessionService userSessionService)
{
    public async Task HandleCommandAsync(ITelegramBotClient client,long chatId,string text, User user, CancellationToken cts)
    {
        var state = userSessionService.GetOrCreate(user.Id);
        var tags = await tagService.GetAllAsync();

        switch (text)
        {
            case BotTagCommands.AddTags:
                state.State = BotUserState.AddingTag;
                await client.SendMessage(chatId, "Enter tag name, he must started with #", cancellationToken: cts);
                break;
            case BotTagCommands.Tags:
                var mes =  tags.Any() ? string.Join(", ", tags.Select(t => t.Name)) : "😕 You don't have any tags yet.";
                await client.SendMessage(chatId, mes, cancellationToken: cts);
                break;
            case BotTagCommands.RemoveTags:
                if (!tags.Any())
                {
                    await client.SendMessage(chatId,"<UNK> You don't have any tags yet.", cancellationToken: cts);
                }
                var mesText = "You Tags";
                await client.SendMessage(chatId, mesText,
                    replyMarkup: ReplyMarkupBuilder.TagMarkup(tags, BotCommandEmojis.X, "no"), cancellationToken: cts);
                break;
            case BotTagCommands.Back:
                await client.SendMessage(chatId, "🔙 Returned to main menu", replyMarkup: ReplyMarkupBuilder.MainMenu(), cancellationToken: cts);
                state.State = BotUserState.None;
                break;
        }

    }

    public async Task HandleTextTagInputAsync(ITelegramBotClient client, string text, long chatId, User user, UserNoteState state, CancellationToken cts)
    {
        switch (state.State)
        {
            case BotUserState.AddingTag:
                if (!text.StartsWith("#"))
                {
                    await client.SendMessage(chatId, "Uncorrected tag, try again", cancellationToken: cts);
                    break;
                }
                await tagService.AddTag(text);
                await client.SendMessage(chatId, "Tag added", cancellationToken: cts);
                state.State = BotUserState.TagManagement;
                break;
        }
    }
}
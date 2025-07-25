using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramNoteBot.Models;
using TelegramNoteBot.Services;
using TelegramNoteBot.UserSessionService;

namespace TelegramNoteBot;

public class Worker(
    ILogger<Worker> logger,
    IConfiguration configuration,
    IServiceScopeFactory serviceScopeFactory,
    UserSessionService.UserSessionService _userSessionService) : BackgroundService
{
    private TelegramBotClient? _client = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client = new TelegramBotClient(configuration["Telegram:Token"] ?? string.Empty);

        _client.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = [] },
            cancellationToken: stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cts)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var noteService = scope.ServiceProvider.GetRequiredService<NoteService>();

        if (update.CallbackQuery is { Data : var data, From : var callbackUser } callback)
        {
            var callbackChatId = callback.Message.Chat.Id;
            var messageId = callback.Message.MessageId;

            if (data.StartsWith("del_") && int.TryParse(data[4..], out var noteIdToDelete))
            {
                var result = await noteService.DeleteNote(callbackUser.Id, noteIdToDelete);
                if (result)
                    await client.EditMessageText(callbackChatId, messageId, "Note deleted", replyMarkup: GetMarkupBack(), cancellationToken: cts);
                else await client.SendMessage(callbackChatId, "Can`t delete the note 😢", cancellationToken: cts);
            }
            else if (data.StartsWith("inf_") && int.TryParse(data[4..], out var noteIdToShow))
            {
                var note = await noteService.GetNote(callbackUser.Id, noteIdToShow);
                await client.EditMessageText(callbackChatId, messageId, note.Text, replyMarkup: GetMarkupBack(), cancellationToken: cts);
            }
        }

        if (update.Message is not { Text: { } text } message) return;

        var user = message.From;
        var chatId = message.Chat.Id;
        var state = _userSessionService.GetOrCreate(user.Id);


        if (text == "/start")
        {
            var menu = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("🟢 Add note") },
                new[] { new KeyboardButton("📄 My notes") },
                new[] { new KeyboardButton("🗑 Delete note") },
                new[] { new KeyboardButton("About developer") }
            }) { ResizeKeyboard = true };

            await client.SendMessage(chatId, "WELCOME", replyMarkup: menu, cancellationToken: cts);
        }
        else if (text == "🟢 Add note")
        {
            state.PendingTitle = null;
            await client.SendMessage(chatId, "Enter note title: ", cancellationToken: cts);
        }
        else if (text == "📄 My notes")
        {
            var notes = await noteService.GetNotes(user.Id);

            if (!notes.Any())
            {
                await client.SendMessage(chatId, "Sorry, no notes found.", cancellationToken: cts);
                return;
            }

            var markup = GetMarkupFromData(notes, "➕", "inf_");
            await client.SendMessage(chatId, "Notes: ", replyMarkup: markup, cancellationToken: cts);
        }
        else if (text == "🗑 Delete note")
        {
            var notes = await noteService.GetNotes(user.Id);
            if (!notes.Any())
            {
                await client.SendMessage(chatId, "Sorry, no notes found.", cancellationToken: cts);
                return;
            }

            var markup = GetMarkupFromData(notes, "❌", "del_" );
            await client.SendMessage(chatId, "Choice note", replyMarkup: markup, cancellationToken: cts);
        }
        else if (state.PendingTitle == null)
        {
            state.PendingTitle = text;
            await client.SendMessage(chatId, "Enter the note text", cancellationToken: cts);
        }
        else
        {
            var title = state.PendingTitle;
            await noteService.AddNote(user.Id, user.Username, title, text);

            _userSessionService.Clear(user.Id);
            await client.SendMessage(chatId, "Note added", cancellationToken: cts);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception ex, CancellationToken cts)
    {
        logger.LogError($"Exception : {ex.Message}");
        return Task.CompletedTask;
    }

    private InlineKeyboardMarkup GetMarkupFromData(List<Note> notes, string emoji, string cod)
        => new InlineKeyboardMarkup(notes.Select(n => new[]
            {
                InlineKeyboardButton.WithCallbackData($"{emoji} {n.Title}", $"{cod}{n.Id}")
            })
            .ToArray()
        );

    private InlineKeyboardMarkup GetMarkupBack()
        => new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️BACK", "some")
        });
}
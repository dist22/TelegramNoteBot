using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramNoteBot.Services;

namespace TelegramNoteBot;

public class Worker(ILogger<Worker> logger, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
    : BackgroundService
{
    private TelegramBotClient? _client = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        _client = new TelegramBotClient(configuration["Telegram:Token"] ?? string.Empty);
        
        _client.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = [] }, 
            cancellationToken: stoppingToken );
        
    }
    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cts)
    {
        if (update.Message is not { Text: { } text } message) return;
        
        var userId = message.From.Id;
        var chatId = message.Chat.Id;
        
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
                new[] {new KeyboardButton("📄 My notes")},
                new[] { new KeyboardButton("🗑 Delete note") }
            }) {ResizeKeyboard = true};
            
            await client.SendMessage(chatId, "WELCOME", replyMarkup: menu, cancellationToken: cts);
        }
        
        else if (text == "🟢 Add note")
        {
            await client.SendMessage(chatId, "Enter note text: ", cancellationToken: cts);
        }
        else if (text == "📄 My notes")
        {
            var notes = await noteService.GetNotes(userId);

            if (!notes.Any())
            {
                await client.SendMessage(chatId, "Sorry, no notes found.", cancellationToken: cts);
                return;
            }

            var markup = new InlineKeyboardMarkup(notes.Select(n => new[]
            {
                InlineKeyboardButton.WithCallbackData($"📄{n.Text}", $"inf_{n.Id}")})
                .ToArray()
            );

            await client.SendMessage(chatId, "Notes: ", replyMarkup:markup, cancellationToken: cts);
        }
        else if (text == "🗑 Delete note")
        {
            var notes = await noteService.GetNotes(userId);
            if (!notes.Any())
            {
                await client.SendMessage(chatId, "Sorry, no notes found.", cancellationToken: cts);
                return;
            }
            
            var markup = new InlineKeyboardMarkup(notes.Select(n => new []{
                InlineKeyboardButton.WithCallbackData($"❌ {n.Text}", $"del_{n.Id}")})
                .ToArray()
            );
            
            await client.SendMessage(chatId, "Choice note", replyMarkup: markup, cancellationToken: cts);
        }
        else
        {
            await noteService.AddNote(text, userId);
            await client.SendMessage(chatId, "Note added", cancellationToken: cts);
        }
    }
    
    private Task HandleErrorAsync(ITelegramBotClient client, Exception ex, CancellationToken cts)
    {
        logger.LogError($"Exception : {ex.Message}");
        return Task.CompletedTask;
    }
}

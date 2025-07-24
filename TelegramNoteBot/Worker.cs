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

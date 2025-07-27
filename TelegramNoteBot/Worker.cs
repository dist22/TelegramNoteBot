using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramNoteBot.Bot;
using TelegramNoteBot.Models;
using TelegramNoteBot.Services;

namespace TelegramNoteBot;

public class Worker(
    ILogger<Worker> logger,
    IConfiguration configuration,
    BotUpdateHandler updateHandler, IServiceScopeFactory _scope) : BackgroundService
{
    private TelegramBotClient? _client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client = new TelegramBotClient(configuration["Telegram:Token"] ?? string.Empty);

        _client.StartReceiving(
            updateHandler.HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = [] },
            cancellationToken: stoppingToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception ex, CancellationToken cts)
    {
        logger.LogError($"Exception : {ex.Message}");
        return Task.CompletedTask;
    }
    
}
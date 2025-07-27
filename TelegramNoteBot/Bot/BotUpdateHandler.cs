using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class BotUpdateHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BotUpdateHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    public async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cts)
    {
        using var  scope = _scopeFactory.CreateScope();
        var noteService = scope.ServiceProvider.GetRequiredService<NoteService>();
        var userSession = scope.ServiceProvider.GetRequiredService<UserSessionService>();
        var noteDisplayService = scope.ServiceProvider.GetRequiredService<NoteDisplayService>();
        
        var callbackHandler = new CallbackHandler(noteService, userSession);
        var messageHandler = new MessageHandler(noteService, userSession, noteDisplayService);
        
        if (update.CallbackQuery is not null)
            await callbackHandler.HandleUpdateAsync(client, update.CallbackQuery, cts);
        else if (update.Message is not null)
            await messageHandler.HandleUpdateAsync(client, update.Message, cts);
    }

}
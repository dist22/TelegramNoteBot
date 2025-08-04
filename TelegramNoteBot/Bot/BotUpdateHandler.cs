using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class BotUpdateHandler(IServiceScopeFactory scopeFactory)
{
    public async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cts)
    {
        using var  scope = scopeFactory.CreateScope();
        
        var noteService = scope.ServiceProvider.GetRequiredService<NoteService>();
        var userSession = scope.ServiceProvider.GetRequiredService<UserSessionService>();
        var noteDisplayService = scope.ServiceProvider.GetRequiredService<NoteDisplayService>();
        var tagHandler = scope.ServiceProvider.GetRequiredService<TagCommandHandler>();
        var addTagToNoteCommandHandler = scope.ServiceProvider.GetRequiredService<AddTagToNoteCommandHandler>();
        var tagService = scope.ServiceProvider.GetRequiredService<TagService>();
        var noteTagService = scope.ServiceProvider.GetRequiredService<NoteTagService>();
        
        var callbackHandler = new CallbackHandler(noteService, userSession, tagService, noteTagService);
        var messageHandler = new MessageHandler(noteService, userSession, noteDisplayService, tagHandler, addTagToNoteCommandHandler, tagService);
        
        if (update.CallbackQuery is not null)
            await callbackHandler.HandleUpdateAsync(client, update.CallbackQuery, cts);
        else if (update.Message is not null)
            await messageHandler.HandleUpdateAsync(client, update.Message, cts);
    }
}
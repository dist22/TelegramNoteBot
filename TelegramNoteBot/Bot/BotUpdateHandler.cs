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
        var tagService = scope.ServiceProvider.GetRequiredService<TagService>();
        var tagHelperService = scope.ServiceProvider.GetRequiredService<TagHelperService>();
        var replyMarkupBuilder = scope.ServiceProvider.GetRequiredService<ReplyMarkupBuilder>();
        var redisCallBackStorage = scope.ServiceProvider.GetRequiredService<RedisCallBackStorage>();
        
        var callbackHandler = new CallbackHandler(noteService, userSession, tagService, tagHelperService, replyMarkupBuilder,redisCallBackStorage );
        var messageHandler = new MessageHandler(noteService, userSession, noteDisplayService, tagHandler, tagService, tagHelperService, replyMarkupBuilder);
        
        if (update.CallbackQuery is not null)
            await callbackHandler.HandleUpdateAsync(client, update.CallbackQuery, cts);
        else if (update.Message is not null)
            await messageHandler.HandleUpdateAsync(client, update.Message, cts);
    }
}
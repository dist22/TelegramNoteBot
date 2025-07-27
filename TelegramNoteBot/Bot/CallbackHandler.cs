using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Enums;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class CallbackHandler(NoteService noteService, UserSessionService userSessionService)
{
    public async Task HandleUpdateAsync(ITelegramBotClient client, CallbackQuery callbackQuery, CancellationToken cts)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var user = callbackQuery.From;
        var data = callbackQuery.Data;
        var state = userSessionService.GetOrCreate(user.Id);
        
        if(data.StartsWith(CallBackCommands.Delete) && int.TryParse(data[4..], out var noteIdToDelete))
        {
            var result = await noteService.DeleteNote(user.Id, noteIdToDelete);
            if (result)
            {
                await client.DeleteMessage(chatId, messageId, cancellationToken: cts);
                await client.SendMessage(chatId, "Note deleted", cancellationToken: cts);
            }
            else await client.SendMessage(chatId, "Can`t delete the note 😢", cancellationToken: cts);
        }
        else if (data.StartsWith(CallBackCommands.Info) && int.TryParse(data[4..], out var noteIdToShow))
        {
            var note = await noteService.GetNote(user.Id, noteIdToShow);
            await client.DeleteMessage(chatId, messageId, cancellationToken: cts);
            await client.SendMessage(chatId, note.Text, cancellationToken: cts);
        }
    }
    
}
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramNoteBot.Constants;
using TelegramNoteBot.Enums;
using TelegramNoteBot.Services;

namespace TelegramNoteBot.Bot;

public class CallbackHandler(NoteService noteService, UserSessionService userSessionService, TagService tagService)
{
    public async Task HandleUpdateAsync(ITelegramBotClient client, CallbackQuery callbackQuery, CancellationToken cts)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var user = callbackQuery.From;
        var data = callbackQuery.Data;
        var state = userSessionService.GetOrCreate(user.Id);
        var d = 1;
        
        if(data.StartsWith(CallBackCommands.Delete) && int.TryParse(data[4..], out var noteIdToDelete))
        {
            var result = await noteService.DeleteNote(user.Id, noteIdToDelete);
            if (result)
            {
                await client.EditMessageText(chatId, messageId, "Note deleted", cancellationToken: cts);
            }
            else await client.SendMessage(chatId, "Can`t delete the note 😢", cancellationToken: cts);
        }
        else if (data.StartsWith(CallBackCommands.Info) && int.TryParse(data[4..], out var noteIdToShow))
        {
            var note = await noteService.GetNote(user.Id, noteIdToShow);
            await client.EditMessageText(chatId, messageId, note.Text, cancellationToken: cts);
        }
        else if (data.StartsWith(CallBackCommands.TagDelete) && int.TryParse(data[5..], out var tagIdToDelete))
        {
            var tag = await tagService.DeleteTag(tagIdToDelete, user.Id);
            var message = tag ? "Tag successfully deleted" : "Tag deleted";
            await client.EditMessageText(chatId, messageId, message, cancellationToken: cts);
        }
    }
    
}
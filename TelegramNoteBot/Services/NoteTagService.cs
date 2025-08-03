using Microsoft.EntityFrameworkCore;
using TelegramNoteBot.Data;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Services;

public class NoteTagService(AppDbContext dbContext, ILogger<NoteTagService> logger)
{
    private readonly DbSet<NoteTag> _dbSet = dbContext.Set<NoteTag>();


    public async Task AddNoteTagAsync(int noteId, int tagId)
    {
        await _dbSet.AddAsync(new NoteTag
        {
            NoteId = noteId, 
            TagId = tagId
        });
        
        logger.LogInformation("AddNoteTagAsync");
        
        await dbContext.SaveChangesAsync();
    }

}
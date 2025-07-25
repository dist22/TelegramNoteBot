using Microsoft.EntityFrameworkCore;
using TelegramNoteBot.Data;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Services;

public class NoteService(AppDbContext dbContext, ILogger<NoteService> logger)
{
    private readonly DbSet<Note> _dbSet = dbContext.Set<Note>();

    public async Task AddNote(string text, long userId)
    {
        await  _dbSet.AddAsync(new Note {UserId = userId, Text = text});
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Note added");
    }

    public async Task<Note?> GetNote(long userId, int noteId) 
        => await _dbSet.FirstOrDefaultAsync(n => n.UserId == userId && n.Id == noteId);


    public async Task<List<Note>> GetNotes(long userId)
    {
        logger.LogInformation("GetNotes");
        
        return await _dbSet
            .Where(n => n.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> DeleteNote(long userId, int noteId)
    {
        logger.LogInformation("started delete note");
        
        var note = await _dbSet.FirstOrDefaultAsync(n => n.UserId == userId && n.Id == noteId);
        if (note == null) return false;
        _dbSet.Remove(note);
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation("Note deleted");
        
        return true;
    }

}
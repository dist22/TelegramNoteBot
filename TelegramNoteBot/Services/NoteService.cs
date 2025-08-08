using Microsoft.EntityFrameworkCore;
using TelegramNoteBot.Data;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Services;

public class NoteService(AppDbContext dbContext, ILogger<NoteService> logger)
{
    private readonly DbSet<Note> _dbSet = dbContext.Set<Note>();

    public async Task<int> AddNote(long userId, string userName, string noteTile, string noteText)
    {

        var note = new Note
        {
            UserId = userId,
            UserName = userName,
            Title = noteTile,
            Text = noteText
        };
        
        await  _dbSet.AddAsync(note);
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation("New note added");
        
        return note.Id;
    }

    public async Task<List<Note>> GetNoteByTagAsync(long userId, int tagId)
        => await _dbSet
            .Where(n => n.UserId == userId && n.NoteTags.Any(nt => nt.TagId == tagId))
            .ToListAsync();

    public async Task<Note?> GetNote(long userId, int noteId) 
        => await _dbSet
            .Include(n => n.NoteTags)
                .ThenInclude(n => n.Tag)
            .FirstOrDefaultAsync(n => n.UserId == userId && n.Id == noteId);

    public async Task<IEnumerable<Note>> GetSortedAsync(long userId, bool desc)
    {
        var notes = await GetNotes(userId);
        
        return desc 
            ? notes.OrderByDescending(n => n.CreatedAt) 
            : notes.OrderBy(n => n.CreatedAt);
    }


    public async Task<List<Note>> GetNotes(long userId)
    {
        logger.LogInformation("GetNotes");
        
        return await _dbSet
            .Where(n => n.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<Note>> SearchNotes(long userId, string searchQuery)
    {
        var allNotes = await GetNotes(userId);
        return allNotes
            .Where(n =>
                n.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                n.Text.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
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
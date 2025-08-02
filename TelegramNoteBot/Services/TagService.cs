using Microsoft.EntityFrameworkCore;
using TelegramNoteBot.Data;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Services;

public class TagService(AppDbContext dbContext, ILogger<TagService> logger)
{
    private readonly DbSet<Tag> _dbSet = dbContext.Tag;

    public async Task AddTag(string name, long userId)
    {
        await _dbSet.AddAsync(new Tag 
            { 
                Name = name, 
                AuthorId = userId 
            });
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<Tag>> GetAllAsync(long userId)
        => await _dbSet
            .Where(t => t.AuthorId == userId)
            .ToListAsync();

    public async Task<bool> TagExist(string name, long userId)
        => await _dbSet
            .Where(t => t.AuthorId == userId)
            .AnyAsync(t => t.Name == name);

    public async Task<bool> DeleteTag(int tagId, long userId)
    {
        var tag = await _dbSet
            .Where(t => t.AuthorId == userId)
            .FirstOrDefaultAsync(n => n.Id == tagId);
        if (tag == null) return false;
        _dbSet.Remove(tag);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
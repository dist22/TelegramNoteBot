using Microsoft.EntityFrameworkCore;
using TelegramNoteBot.Data;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Services;

public class TagService(AppDbContext dbContext, ILogger<TagService> logger)
{
    private readonly DbSet<Tag> _dbSet = dbContext.Tag;

    public async Task AddTag(string name)
    {
        await _dbSet.AddAsync(new Tag { Name = name });
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<Tag>>GetAllAsync() 
        => await _dbSet.ToListAsync();

    public async Task<bool> DeleteTag(int tagId)
    {
        var tag = await _dbSet.FirstOrDefaultAsync(n => n.Id == tagId);
        if (tag == null) return false;
        _dbSet.Remove(tag);
        await dbContext.SaveChangesAsync();
        return true;
    }


}
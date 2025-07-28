namespace TelegramNoteBot.Models;

public class Note
{
    public int Id { get; set; }
    public long UserId { get; set; }

    public string UserName { get; set; } = string.Empty;
    
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow ;

    public List<NoteTag> NoteTags { get; set; } = new();
}
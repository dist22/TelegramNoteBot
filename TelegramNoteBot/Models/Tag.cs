namespace TelegramNoteBot.Models;

public class Tag
{
    
    public int Id { get; set; }
    public long AuthorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<NoteTag> NoteTags { get; set; } = new();

}
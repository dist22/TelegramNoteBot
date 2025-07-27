using TelegramNoteBot.Enums;

namespace TelegramNoteBot.Services;

public class UserNoteState
{
    public string? PendingTitle { get; set; } = string.Empty;
    public BotUserState State { get; set; } = BotUserState.None;
    
}

public class UserSessionService
{
    private readonly Dictionary<long, UserNoteState> _states = new();

    public UserNoteState GetOrCreate(long userId)
    {
        if (!_states.TryGetValue(userId, out var state))
        {
            state = new UserNoteState();
            _states[userId] = state;
        }
        return state;
    }

    public void Clear(long userId)
        => _states.Remove(userId);

}
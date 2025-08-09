using System.Text.Json;
using StackExchange.Redis;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Services;

public class RedisCallBackStorage(IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<string> StoreCallBackAsync(CallBackData callBackData)
    {
        var key = $"{callBackData.User.Id}:{Guid.NewGuid()}";
        var json = JsonSerializer.Serialize(callBackData);
        await _db.StringSetAsync(key, json, TimeSpan.FromMinutes(10));
        return key;
    }

    public async Task<CallBackData?> GetCallBackAsync(string key)
    {
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<CallBackData>(json!);
    }
}
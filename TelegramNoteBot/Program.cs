using Microsoft.EntityFrameworkCore;
using TelegramNoteBot;
using TelegramNoteBot.Bot;
using TelegramNoteBot.Data;
using TelegramNoteBot.Services;

var builder = Host.CreateApplicationBuilder(args);


builder.Services.AddDbContext<AppDbContext>(cfg =>
{
    cfg.UseNpgsql(builder.Configuration.GetConnectionString("Connection"));
});

builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<CallbackHandler>();
builder.Services.AddScoped<MessageHandler>();
builder.Services.AddScoped<TagCommandHandler>();
builder.Services.AddSingleton<UserSessionService>();
builder.Services.AddSingleton<BotUpdateHandler>();
builder.Services.AddScoped<NoteDisplayService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<NoteTagService>();
builder.Services.AddScoped<TagHelperService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using var scope = host.Services.CreateScope();
await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.EnsureCreatedAsync();

host.Run();
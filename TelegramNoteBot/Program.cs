using Microsoft.EntityFrameworkCore;
using TelegramNoteBot;
using TelegramNoteBot.Data;
using TelegramNoteBot.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddDbContext<AppDbContext>(cfg =>
{
    cfg.UseNpgsql(builder.Configuration.GetConnectionString("Connection"));
});

builder.Services.AddScoped<NoteService>();

var host = builder.Build();

using var scope = host.Services.CreateScope();
await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.EnsureCreatedAsync();

host.Run();
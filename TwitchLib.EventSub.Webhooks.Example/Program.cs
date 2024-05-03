using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TwitchLib.EventSub.Webhooks.Example;
using TwitchLib.EventSub.Webhooks.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddTwitchLibEventSubWebhooks(config =>
{
    config.CallbackPath = "/webhooks";
    config.Secret = "supersecuresecret";
});
builder.Services.AddHostedService<EventSubHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization();

app.UseTwitchLibEventSubWebhooks();

app.MapControllers();

app.Run();

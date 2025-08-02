using twitch_pinger_config_api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<IConfigService, ConfigService>();

var app = builder.Build();

app.Urls.Add("http://*:5138");
app.MapControllers();
app.Run();
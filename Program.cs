using twitch_pinger_config_api;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var location = builder.Configuration["ConfigFileLocation"] ?? "";
if (string.IsNullOrWhiteSpace(location))
{
  throw new Exception("ConfigFileLocation must be specified");
}

var repository = new TwitchPingerConfigRepository(location);

app.Urls.Add("http://*:5138");
app.UseMiddleware<CustomExceptionHandlerMiddleware>();
app.UseMiddleware<PathParameterValidationMiddleware>();

app.MapGet("/", repository.GetAll);

app.MapGet("/{sectionName}", (string sectionName) => repository.GetUsers(sectionName));

app.MapPost("/add", (AddEntryRequest? obj) =>
{
  if (obj == null)
  {
    return Results.BadRequest("Request body is required");
  }

  var (section, username, id) = obj;

  if (string.IsNullOrWhiteSpace(section))
  {
    return ResultsHelper.RequiredParameter(nameof(section));
  }

  if (string.IsNullOrWhiteSpace(username))
  {
    return ResultsHelper.RequiredParameter(nameof(username));
  }

  if (string.IsNullOrWhiteSpace(id) || !int.TryParse(id, out var idValue))
  {
    return ResultsHelper.RequiredIntParameter(nameof(id));
  }

  repository.Add(section, username, idValue);
  return Results.Ok($"Added new entry {obj}");
});

app.MapPost("/rename", (RenameEntryRequest? obj) =>
{
  if (obj == null)
  {
    return Results.BadRequest("Request body is required");
  }

  var (oldUsername, newUsername, section) = obj;

  if (string.IsNullOrWhiteSpace(oldUsername))
  {
    return ResultsHelper.RequiredParameter(nameof(oldUsername));
  }

  if (string.IsNullOrWhiteSpace(newUsername))
  {
    return ResultsHelper.RequiredParameter(nameof(newUsername));
  }

  if (string.IsNullOrWhiteSpace(section))
  {
    return ResultsHelper.RequiredParameter(nameof(section));
  }

  repository.Rename(oldUsername, newUsername, section);
  return Results.Ok($"Renamed {obj.OldUsername} to {obj.NewUsername} in {obj.Section}");
});

app.MapPost("/move", (MoveEntryRequest? obj) =>
{
  if (obj == null)
  {
    return Results.BadRequest("Request body is required");
  }

  var (oldSection, newSection, userName) = obj;

  if (string.IsNullOrWhiteSpace(oldSection))
  {
    return ResultsHelper.RequiredParameter(nameof(oldSection));
  }

  if (string.IsNullOrWhiteSpace(newSection))
  {
    return ResultsHelper.RequiredParameter(nameof(newSection));
  }

  if (string.IsNullOrWhiteSpace(userName))
  {
    return ResultsHelper.RequiredParameter(nameof(userName));
  }

  repository.Move(oldSection, newSection, userName);
  return Results.Ok($"Moved {obj.Username} from {obj.OldSection} to {obj.NewSection}");
});

app.MapDelete("/{sectionName}", (string sectionName) =>
{
  repository.DeleteSection(sectionName);
  return Results.Ok($"Deleted the {sectionName} section");
});

app.MapDelete("/{sectionName}/{userName}", (string sectionName, string userName) =>
{
  repository.DeleteUsername(sectionName, userName);
  return Results.Ok($"Deleted {userName} from the {sectionName} section");
});

app.Run();
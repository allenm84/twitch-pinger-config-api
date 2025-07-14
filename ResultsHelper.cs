namespace twitch_pinger_config_api;

public static class ResultsHelper
{
  public static IResult RequiredParameter(string name)
  {
    return Results.BadRequest($"{name} is required");
  }

  public static IResult RequiredIntParameter(string name)
  {
    return Results.BadRequest($"{name} is required and must be an integer");
  }
}

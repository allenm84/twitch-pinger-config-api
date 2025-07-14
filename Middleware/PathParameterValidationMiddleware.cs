namespace twitch_pinger_config_api;

public class PathParameterValidationMiddleware(RequestDelegate next)
{
  private readonly RequestDelegate _next = next;

  public async Task InvokeAsync(HttpContext context)
  {
    var path = context.Request.Path;
    if (path.HasValue && path.Value.Contains('{'))
    {
      var parameters = path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries)
                              .Where(p => p.StartsWith('{') && p.EndsWith('}'))
                              .Select(p => p.Trim('{', '}'));

      foreach (var parameter in parameters)
      {
        var value = context.Request.RouteValues[parameter];
        if (value == null || (value is string text && string.IsNullOrWhiteSpace(text)))
        {
          context.Response.StatusCode = StatusCodes.Status400BadRequest;
          await context.Response.WriteAsync($"Parameter '{parameter}' cannot be null.");
          return;
        }
      }
    }

    await _next(context);
  }
}

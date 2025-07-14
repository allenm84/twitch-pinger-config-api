using Microsoft.AspNetCore.Diagnostics;

namespace twitch_pinger_config_api;

public class CustomExceptionHandlerMiddleware(RequestDelegate next)
{
  private readonly RequestDelegate _next = next;

  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (Exception ex)
    {
      var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
      var exception = exceptionHandlerFeature?.Error;
      context.Response.StatusCode = 500;
      await context.Response.WriteAsync(exception?.Message ?? ex.Message);
    }
  }
}

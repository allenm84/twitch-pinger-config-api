using Microsoft.AspNetCore.Mvc;

namespace twitch_pinger_config_api;

[ApiController]
[Route("config/sections")]
public class ConfigController(IConfigService service) : ControllerBase
{
  [HttpGet]
  public IAsyncEnumerable<ChannelSectionDto> GetSections()
  {
    return service.GetAllSections();
  }
  
  [HttpGet("{sectionName}")]
  public async Task<IActionResult> GetSection(string sectionName)
  {
    var section = await service.GetSection(sectionName);
    return section == null ? NotFound() : Ok(section);
  }
  
  [HttpPost("{sectionName}")]
  public async Task<IActionResult> CreateSection(string sectionName)
  {
    var section = await service.CreateSection(sectionName);
    return CreatedAtAction(nameof(GetSections), new { sectionName }, section);
  }
  
  [HttpPatch("{sectionName}")]
  public async Task<IActionResult> RenameSection(string sectionName, [FromBody] PatchChannelSectionDto body)
  {
    var newName = body.Name;
    if (string.IsNullOrWhiteSpace(newName))
    {
      return BadRequest("Missing section name");
    }
    
    await service.RenameSection(sectionName, newName);
    return Ok($"Renamed from {sectionName} to {newName}");
  }
  
  [HttpDelete("{sectionName}")]
  public async Task<IActionResult> DeleteSection(string sectionName)
  {
    await service.DeleteSection(sectionName);
    return Ok($"{sectionName} deleted");
  }
  
  [HttpPost("{sectionName}/channels")]
  public async Task<IActionResult> CreateChannel(string sectionName, [FromBody] ChannelDto body)
  {
    var channel = await service.CreateChannel(sectionName, body);
    return CreatedAtAction(nameof(GetChannel), new { sectionName, channelName = channel.Name }, channel);
  }

  [HttpGet("{sectionName}/channels/{channelName}")]
  public async Task<IActionResult> GetChannel(string sectionName, string channelName)
  {
    var channel = await service.GetChannel(sectionName, channelName);
    return channel == null ? NotFound() : Ok(channel);
  }

  [HttpPatch("{sectionName}/channels/{channelName}")]
  public async Task<IActionResult> UpdateChannel(string sectionName, string channelName, [FromBody] PatchChannelDto body)
  {
    var updated = await service.UpdateChannel(sectionName, channelName, body);
    return Ok(updated);
  }
  
  [HttpDelete("{sectionName}/channels/{channelName}")]
  public async Task<IActionResult> DeleteChannel(string sectionName, string channelName)
  {
    await service.DeleteChannel(sectionName, channelName);
    return Ok($"{channelName} in {sectionName} deleted");
  }
  
  [HttpPost("channels/{channelName}/move")]
  public async Task<IActionResult> MoveChannel(string channelName, [FromBody] MoveChannelDto body)
  {
    await service.MoveChannel(channelName, body);
    return Ok($"{channelName} moved from {body.From} to {body.To}");
  }
}
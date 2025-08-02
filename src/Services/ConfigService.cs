using Newtonsoft.Json;

namespace twitch_pinger_config_api;

public class ConfigService : IConfigService
{
  private record ChannelSectionEntity([property: JsonProperty("name")] string Name, [property: JsonProperty("channels")] List<ChannelEntity> Channels);
  private record ChannelEntity([property: JsonProperty("id")] int Id, [property: JsonProperty("name")] string Name, [property: JsonProperty("folder")] string Folder);
  
  private readonly SemaphoreSlim _semaphore = new(1);
  private readonly string _location;
  private readonly List<ChannelSectionEntity> _sections;
  
  public ConfigService(IConfiguration configuration)
  {
    _location = configuration["config-file-location"] ?? "";
    if (string.IsNullOrWhiteSpace(_location))
    {
      throw new("ConfigFileLocation must be specified");
    }
    
    var jsonText = File.ReadAllText(_location);
    _sections = JsonConvert.DeserializeObject<List<ChannelSectionEntity>>(jsonText) ?? [];
  }
  
  private async Task WriteChangesToFile()
  {
    await _semaphore.WaitAsync();
    try
    {
      
      var jsonText = await Task.Run(() => JsonConvert.SerializeObject(_sections, Formatting.Indented));
      await File.WriteAllTextAsync(_location, jsonText);
    }
    finally
    {
      _semaphore.Release();
    }
  }

  private static ChannelSectionDto? EntityToDto(ChannelSectionEntity? entity) => 
    entity == null ? null : new(entity.Name, entity.Channels.Select(EntityToDto).ToArray()!);
  
  private static ChannelDto? EntityToDto(ChannelEntity? entity) => 
    entity == null ? null : new(entity.Id, entity.Name, entity.Folder);
  
  private ChannelSectionEntity? GetChannelSection(string name, bool throwOnNotFound)
  {
    var result = _sections.SingleOrDefault(it => it.Name == name);
    if (throwOnNotFound && result == null)
    {
      throw new($"{name} was not found"); 
    }
    return result;
  }

  private ChannelSectionEntity GetOrCreateChannelSection(string name)
  {
    var section = GetChannelSection(name, false);
    if (section != null) return section;
    
    section = new(name, []);
    _sections.Add(section);
    return section;
  }

  private static bool SectionContainsChannel(ChannelSectionEntity entity, string channelName) => 
    entity.Channels.Any(it => it.Name == channelName);
  
  async IAsyncEnumerable<ChannelSectionDto> IConfigService.GetAllSections()
  {
    await Task.CompletedTask;
    
    foreach (var item in _sections)
    {
      yield return EntityToDto(item)!;
    }
  }
  
  Task<ChannelSectionDto?> IConfigService.GetSection(string sectionName)
  {
    var section = GetChannelSection(sectionName, true);
    return Task.FromResult(EntityToDto(section));
  }
  
  Task<ChannelSectionDto> IConfigService.CreateSection(string sectionName)
  {
    var section = GetOrCreateChannelSection(sectionName);
    return Task.FromResult(EntityToDto(section)!);
  }
  
  async Task IConfigService.RenameSection(string oldName, string newName)
  {
    for (var i = 0; i < _sections.Count; i++)
    {
      var section = _sections[i];
      if (section.Name != oldName)
      {
        continue;
      }
      
      _sections[i] = section with { Name = newName };;
      await WriteChangesToFile();
      break;
    }
    
    throw new($"Section with {oldName} was not found");
  }
  
  async Task IConfigService.DeleteSection(string sectionName)
  {
    var numRemoved = _sections.RemoveAll(it => it.Name == sectionName);
    if (numRemoved == 0)
    {
      throw new($"Section with {sectionName} was not found");
    }
    
    await WriteChangesToFile();
  }

  async Task<ChannelDto> IConfigService.CreateChannel(string sectionName, ChannelDto body)
  {
    var section = GetOrCreateChannelSection(sectionName);
    if (SectionContainsChannel(section, body.Name))
    {
      throw new($"Channel {body.Name} already exists in {sectionName}");
    }

    section.Channels.Add(new(body.Id, body.Name, body.OutputFolder));
    await WriteChangesToFile();
    return body;
  }

  Task<ChannelDto?> IConfigService.GetChannel(string sectionName, string channelName)
  {
    var section = GetChannelSection(sectionName, true)!;
    var channel = section.Channels.SingleOrDefault(it => it.Name == channelName);
    return Task.FromResult(EntityToDto(channel));
  }

  async Task<ChannelDto> IConfigService.UpdateChannel(string sectionName, string channelName, PatchChannelDto body)
  {
    var section = GetChannelSection(sectionName, true)!;
    for (var i = 0; i < section.Channels.Count; i++)
    {
      var channel =  section.Channels[i];
      if (channel.Name != channelName)
      {
        continue;
      }
      
      var updated = new ChannelEntity(body.Id ?? channel.Id, body.Name ?? channel.Name, body.OutputFolder ?? channel.Folder);
      if (updated.Equals(channel))
      {
        throw new($"{channelName} in {sectionName} would not be updated");
      }
      
      section.Channels[i] = updated;
      await WriteChangesToFile();
      return EntityToDto(updated)!;
    }
    
    throw new($"{channelName} was not found in {sectionName}");
  }

  async Task IConfigService.DeleteChannel(string sectionName, string channelName)
  {
    var section = GetChannelSection(sectionName, true)!;
    var numRemoved = section.Channels.RemoveAll(it => it.Name == channelName);
    if (numRemoved == 0)
    {
      throw new($"{channelName} was not found in {sectionName}");
    }
    await WriteChangesToFile();
  }
  
  async Task IConfigService.MoveChannel(string channelName, MoveChannelDto body)
  {
    var from = GetChannelSection(body.From, true)!;
    var channel = from.Channels.PopSingle(it => it.Name == channelName);

    var to = GetOrCreateChannelSection(body.To);
    to.Channels.Add(channel);
    
    await WriteChangesToFile(); 
  }
}
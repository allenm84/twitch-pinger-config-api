using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace twitch_pinger_config_api;

public class TwitchPingerConfigRepository
{
  private readonly SemaphoreSlim _semaphore = new(1);
  private readonly string _location;
  private readonly ConfigFile _configFile;
  public TwitchPingerConfigRepository(string location)
  {
    _location = location;
    var jsonText = File.ReadAllText(location);
    _configFile = JsonConvert.DeserializeObject<ConfigFile>(jsonText) ?? new();
  }

  private async void WriteChangesToFile()
  {
    await _semaphore.WaitAsync();
    try
    {
      var jsonText = await Task.Run(() => JsonConvert.SerializeObject(_configFile, Formatting.Indented));
      await File.WriteAllTextAsync(_location, jsonText);
    }
    finally
    {
      _semaphore.Release();
    }
  }

  public IEnumerable<ChannelMapSection> GetAll()
  {
    foreach (var entry in _configFile)
    {
      var name = entry.Key;
      var list = new List<ChannelMap>();
      foreach (var (key, id) in entry.Value)
      {
        if (!key.StartsWith('#'))
        {
          list.Add(new(key, id));
        }
      }
      yield return new ChannelMapSection(name, [.. list]);
    }
  }

  public IEnumerable<ChannelMap> GetUsers(string sectionName)
  {
    if (_configFile.TryGetValue(sectionName, out var entry))
    {
      foreach (var (key, id) in entry)
      {
        yield return new(key, id);
      }
    }
  }

  public void Add(string section, string userName, int id)
  {
    var entry = _configFile.GetOrAdd(section, new ConfigFileEntry());
    if (!entry.TryAdd(userName, id))
    {
      throw new Exception($"{userName} already exists in {section}");
    }

    WriteChangesToFile();
  }

  public void Rename(string oldUsername, string newUsername, string section)
  {
    if (!_configFile.TryGetValue(section, out var entry))
    {
      throw new Exception($"{section} was not found");
    }

    if (entry.ContainsKey(newUsername))
    {
      throw new Exception($"{section} already contains {newUsername}");
    }

    if (!entry.Remove(oldUsername, out var id))
    {
      throw new Exception($"{oldUsername} was not found in {section}");
    }

    if (!entry.TryAdd(newUsername, id))
    {
      throw new Exception($"{section} already contains {newUsername}");
    }

    WriteChangesToFile();
  }

  public void Move(string oldSection, string newSection, string userName)
  {
    if (!_configFile.TryGetValue(oldSection, out var current))
    {
      throw new Exception($"{oldSection} was not found");
    }

    var desired = _configFile.GetOrAdd(newSection, new ConfigFileEntry());
    if (desired.ContainsKey(userName))
    {
      throw new Exception($"{newSection} already contains {userName}");
    }

    if (!current.TryRemove(userName, out var id))
    {
      throw new Exception($"{userName} could not be moved. Does {oldSection} contain {userName}?");
    }

    if (!desired.TryAdd(userName, id))
    {
      throw new Exception($"{newSection} already contains {userName}");
    }

    WriteChangesToFile();
  }

  public void DeleteSection(string sectionName)
  {
    if (!_configFile.TryRemove(sectionName, out var _))
    {
      throw new Exception($"{sectionName} could not be removed");
    }

    WriteChangesToFile();
  }

  public void DeleteUsername(string sectionName, string userName)
  {
    if (!_configFile.TryGetValue(sectionName, out var entry))
    {
      throw new Exception($"{sectionName} was not found");
    }

    if (!entry.TryRemove(userName, out var _))
    {
      throw new Exception($"{userName} from {sectionName} could not be removed");
    }

    WriteChangesToFile();
  }

  class ConfigFileEntry : ConcurrentDictionary<string, int> { }
  class ConfigFile : ConcurrentDictionary<string, ConfigFileEntry> { }
}
namespace twitch_pinger_config_api;

public interface IConfigService
{
  IAsyncEnumerable<ChannelSectionDto> GetAllSections();
  Task<ChannelSectionDto?> GetSection(string sectionName);
  Task<ChannelSectionDto> CreateSection(string sectionName);
  Task RenameSection(string oldName, string newName);
  Task DeleteSection(string sectionName);
  Task<ChannelDto> CreateChannel(string sectionName, ChannelDto body);
  Task<ChannelDto?> GetChannel(string sectionName, string channelName);
  Task<ChannelDto> UpdateChannel(string sectionName, string channelName, PatchChannelDto body);
  Task DeleteChannel(string sectionName, string channelName);
  Task MoveChannel(string channelName, MoveChannelDto body);
}
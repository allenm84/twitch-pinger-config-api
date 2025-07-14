namespace twitch_pinger_config_api;

public record ChannelMap(string Username, int Id);
public record ChannelMapSection(string Key, ChannelMap[] Items);
public record AddEntryRequest(string? Section, string? Username, string? Id);
public record RenameEntryRequest(string? OldUsername, string? NewUsername, string? Section);
public record MoveEntryRequest(string? OldSection, string? NewSection, string? Username);
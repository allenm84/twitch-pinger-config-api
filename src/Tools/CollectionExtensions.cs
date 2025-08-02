namespace twitch_pinger_config_api;

public static class CollectionExtensions
{
  public static T PopSingle<T>(this IList<T> list, Predicate<T> condition)
  {
    var indices = new List<int>();
    for (var i = list.Count - 1; i >= 0; i--)
    {
      var item = list[i];
      if (condition(item))
      {
        indices.Add(i);
      }
    }

    switch (indices.Count)
    {
      case 0:
        throw new("No items found with the specified condition.");
      case > 1:
        throw new("Multiple items found with the specified condition.");
      default:
        var result = list[indices[0]];
        list.RemoveAt(indices[0]);
        return result;
    }
  }
}
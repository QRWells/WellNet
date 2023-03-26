namespace QRWells.WellNet.Core.Utils;

public class MultiDictionary : Dictionary<string, List<string>>
{
    public void Add(string key, string value)
    {
        if (!TryGetValue(key, out var list))
        {
            list = new List<string>();
            Add(key, list);
        }

        list.Add(value);
    }

    public void AddRange(string key, IEnumerable<string> values)
    {
        if (!TryGetValue(key, out var list))
        {
            list = new List<string>();
            Add(key, list);
        }

        list.AddRange(values);
    }

    public void Remove(string key, string value)
    {
        if (TryGetValue(key, out var list)) list.Remove(value);
    }

    public bool TryGetValues(string key, out IEnumerable<string>? values)
    {
        if (TryGetValue(key, out var list))
        {
            values = list;
            return true;
        }

        values = null;
        return false;
    }
}
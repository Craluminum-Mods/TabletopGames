namespace TabletopGames;

public class Variant
{
    public string Key { get; protected set; }
    public string Value { get; protected set; }

    public Variant(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public static Variant? FromString(string keyVal)
    {
        string[]? list = keyVal?.Split('-', 2);
        if (list?.Length != 2)
        {
            return null;
        }
        return new Variant(list[0], list[1]);
    }

    public override string ToString()
    {
        return $"{Key}-{Value}";
    }
}
namespace HttpCacheController.Nginx;

public class ConfigurationValue
{
    public ConfigurationValue(string value, bool quoted = false)
    {
        Value = value;
        Quoted = quoted;
    }

    private bool Quoted { get; }
    private string Value { get; }

    public override string ToString()
    {
        return Quoted ? $"\"{Value}\"" : Value;
    }
}
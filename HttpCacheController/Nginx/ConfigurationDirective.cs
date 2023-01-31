using System.Text;

namespace HttpCacheController.Nginx;

public class ConfigurationDirective
{
    public ConfigurationDirective(string name, ConfigurationValue value)
    {
        Name = name;
        Values = new List<ConfigurationValue> { value };
    }
    
    public ConfigurationDirective(string name, params ConfigurationValue[] values)
    {
        Name = name;
        Values = values.ToList();
    }
    
    public string Name { get; set; }
    public List<ConfigurationValue> Values { get; }


    public string ToString(int indent)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < (indent * ControllerConstants.NGINX_CONFIG_INDENT_SPACES); i++)
        {
            sb.Append(" ");
        }
        sb.Append(Name);
        sb.Append(" ");
        sb.Append(string.Join(" ", Values));
        sb.Append(";\n");
        return sb.ToString();
    }
    
    public override string ToString()
    {
        return ToString(0);
    }
}
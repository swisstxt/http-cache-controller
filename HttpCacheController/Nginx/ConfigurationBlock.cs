using System.Text;

namespace HttpCacheController.Nginx;

public class ConfigurationBlock
{
    public ConfigurationBlock(BlockType type, string args, ConfigurationDirective[]? directives, params ConfigurationBlock[] blocks)
    {
        Type = type;
        Args = args;
        Directives = directives?.ToList();
        Blocks = blocks?.ToList();
    }
    
    public ConfigurationBlock(BlockType type, ConfigurationDirective[]? directives, params ConfigurationBlock[] blocks)
    {
        Type = type;
        Directives = directives?.ToList();
        Blocks = blocks?.ToList();
    }
    
    private string TypetoNginxString(BlockType type)
    {
        return type switch
        {
            BlockType.Http => "http",
            BlockType.Server => "server",
            BlockType.Location => "location"
        };
    }
    
    public BlockType Type { get; set; }
    public string? Args { get; set; }
    public List<ConfigurationBlock>? Blocks { get; set; }
    public List<ConfigurationDirective>? Directives { get; set; }
    
    public override string ToString()
    {
        var sb = new StringBuilder();

        if (Type != BlockType.Root)
        {
            sb.Append(TypetoNginxString(Type));
            if (Args != null)
            {
                sb.Append(" ");
                sb.Append(Args);
            }
            
            sb.Append(" {\n");

        }

        if (Directives != null)
        {
            foreach (var directive in Directives)
            {
                sb.Append(directive);
            }
        }

        if (Blocks != null)
        {
            foreach (var block in Blocks)
            {
                sb.Append(block);
            }
        }

        if (Type != BlockType.Root)
        {
            sb.Append("}\n");
        }

        return sb.ToString();
    }
}
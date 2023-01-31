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
            BlockType.Root => "",
            BlockType.Http => "http",
            BlockType.Server => "server",
            BlockType.Upstream => "upstream",
            BlockType.Location => "location",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    
    public BlockType Type { get; set; }
    public string? Args { get; set; }
    public List<ConfigurationBlock>? Blocks { get; set; }
    public List<ConfigurationDirective>? Directives { get; set; }


    public override string ToString()
    {
        return ToString(0);
    }
    
    private static void AppendIndent(StringBuilder sb, int indent)
    {
        for (var i = 0; i < (indent * ControllerConstants.NGINX_CONFIG_INDENT_SPACES); i++)
        {
            sb.Append(" ");
        }
    }
    
    public string ToString(int indent)
    {
        var sb = new StringBuilder();

        if (Type != BlockType.Root)
        {
            AppendIndent(sb, indent);
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
                sb.Append(directive.ToString(Type == BlockType.Root ? indent : indent+1));
            }
        }

        if (Blocks != null)
        {
            foreach (var block in Blocks)
            {
                sb.Append(block.ToString(Type == BlockType.Root ? indent : indent+1));
            }
        }

        if (Type != BlockType.Root)
        {
            AppendIndent(sb, indent);
            sb.Append("}\n");
        }

        return sb.ToString();
    }
}
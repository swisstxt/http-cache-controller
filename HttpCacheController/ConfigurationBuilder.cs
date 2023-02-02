using HttpCacheController.Nginx;
using k8s.Models;

namespace HttpCacheController;

public class ConfigurationBuilder
{
    private readonly List<ConfigurationBlock> _blocks;
    private readonly List<ConfigurationDirective> _rootDirectives;
    private ConfigurationBuilder()
    {
        _blocks = new List<ConfigurationBlock>();
        
        // add cache zone
        _rootDirectives = new List<ConfigurationDirective>()
        {
            new ConfigurationDirective("proxy_cache_path", new ConfigurationValue("/cache/static levels=1:2 keys_zone=static-cache:10m max_size=5g inactive=60m use_temp_path=off"))
        };

        // adding a catch all server block
        _blocks.Add(new ConfigurationBlock(BlockType.Server, new List<ConfigurationDirective>()
        {
            new ConfigurationDirective("listen", 
                new ConfigurationValue("8080"),
                new ConfigurationValue("default_server")
            ),
            new ConfigurationDirective("server_name", 
                new ConfigurationValue("_")
            ),
            new ConfigurationDirective("return", 
                new ConfigurationValue("444")
            )
        }.ToArray(), null));
    }

    public static ConfigurationBuilder CreateBuilder()
    {
        return new ConfigurationBuilder();
    }
    
    public ConfigurationBuilder AddRoutedService(V1Service item, V1Service targetService)
    {
        foreach (var port in targetService.Spec.Ports)
        {
            var upstream = new ConfigurationBlock(BlockType.Upstream, $"source-{item.Metadata.Name}-{port.Name}", new List<ConfigurationDirective>()
            {
                new ConfigurationDirective("server", new ConfigurationValue($"{item.Metadata.Name}:{item.Spec.Ports.ToList().Find(p => p.Name == port.Name).Port}"))
            }.ToArray(), null);
            
            _blocks.Add(upstream);

            var location = new ConfigurationBlock(BlockType.Location, "/", new ConfigurationDirective[]
            {
                new ConfigurationDirective("proxy_cache", new ConfigurationValue("static-cache")), 
                new ConfigurationDirective("proxy_cache_valid", new ConfigurationValue("any 100m")),
                new ConfigurationDirective("proxy_cache_use_stale", new ConfigurationValue("error timeout updating http_404 http_500 http_502 http_503 http_504")),
                new ConfigurationDirective("add_header", new ConfigurationValue("X-Cache-Status $upstream_cache_status")),
                new ConfigurationDirective("proxy_pass", new ConfigurationValue($"http://source-{item.Metadata.Name}-{port.Name}")),
            }, null);
            
            _blocks.Add(new ConfigurationBlock(BlockType.Server, new List<ConfigurationDirective>()
            {
                new ConfigurationDirective("listen", new ConfigurationValue(port.Port.ToString()))
            }.ToArray(), location));
        }

        return this;
    }

    public ConfigurationBlock Build()
    {
        return new ConfigurationBlock(BlockType.Root, _rootDirectives.ToArray(), _blocks.ToArray());
    }
}

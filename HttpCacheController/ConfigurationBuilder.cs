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
        _rootDirectives = new List<ConfigurationDirective>
        {
            new("proxy_cache_path", "/cache/static", "levels=1:2", "keys_zone=static-cache:10m", "max_size=5g",
                "inactive=60m", "use_temp_path=off")
        };

        // adding a catch all server block
        _blocks.Add(new ConfigurationBlock(BlockType.Server, new List<ConfigurationDirective>
        {
            new("listen", "8080", "default_server"),
            new("server_name", "_"),
            new("return", "444")
        }));
    }

    public static ConfigurationBuilder CreateBuilder()
    {
        return new ConfigurationBuilder();
    }

    public ConfigurationBuilder AddRoutedService(V1Service item, V1Service targetService)
    {
        foreach (var port in targetService.Spec.Ports)
        {
            var upstream = new ConfigurationBlock(BlockType.Upstream, $"source-{item.Metadata.Name}-{port.Name}",
                new List<ConfigurationDirective>
                {
                    new("server",
                        $"{item.Metadata.Name}:{item.Spec.Ports.ToList().Find(p => p.Name == port.Name).Port}")
                });

            _blocks.Add(upstream);

            var location = new ConfigurationBlock(BlockType.Location, "/", new ConfigurationDirective[]
            {
                new("proxy_cache", "static-cache"),
                new("proxy_cache_valid", "any", "100m"),
                new("proxy_cache_use_stale", "error", "timeout", "updating", "http_404", "http_500", "http_502",
                    "http_503", "http_504"),
                new("add_header", "X-Cache-Status", "$upstream_cache_status"),
                new("proxy_pass", $"http://source-{item.Metadata.Name}-{port.Name}")
            });

            _blocks.Add(new ConfigurationBlock(BlockType.Server, new List<ConfigurationDirective>
            {
                new("listen", port.Port.ToString())
            }, location));
        }

        return this;
    }

    public ConfigurationBlock Build()
    {
        return new ConfigurationBlock(BlockType.Root, _rootDirectives, _blocks);
    }
}
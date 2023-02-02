
using HttpCacheController;
using HttpCacheController.Extensions;
using HttpCacheController.Nginx;
using k8s;
using k8s.Autorest;
using k8s.Models;

KubernetesClientConfiguration? config = null;

if (KubernetesClientConfiguration.IsInCluster())
{
    config = KubernetesClientConfiguration.InClusterConfig();
}
else
{
    config = KubernetesClientConfiguration.BuildConfigFromConfigFile(".kubeconfig.local");
}


V1ConfigMap CreateConfigMap(ConfigurationBlock nginxConfig)
{
    var newCm = new V1ConfigMap()
    {
        Metadata = new V1ObjectMeta()
        {
            Name = ControllerConstants.CONFIG_MAP_NAME
        },
        Data = new Dictionary<string, string>()
        {
            { ControllerConstants.NGINX_CONFIG_KEY, nginxConfig.ToString() }
        }
    };

    return newCm;

} 

while (true)
{
    try
    {
        var client = new Kubernetes(config);

        var services = client.CoreV1.ListNamespacedService(config.Namespace).Items.ToList();
        
        var sourceServices = services.FindAll(s => s.HasAnnotation(ControllerConstants.ANNOTATION_ENABLED, ControllerConstants.ANNOTATION_ENABLED_VALUE));

        ServicePorts.Init(sourceServices);
        
        var targetServices = services.FindAll(s => s.HasAnnotation(ControllerConstants.ANNOTATION_AUTOGEN));
        
        List<ConfigurationDirective> rootDirectives = new List<ConfigurationDirective>()
        {
            new ConfigurationDirective("proxy_cache_path", new ConfigurationValue("/cache/static levels=1:2 keys_zone=static-cache:10m max_size=5g inactive=60m use_temp_path=off"))
        };

        List<ConfigurationBlock> blocks = new List<ConfigurationBlock>();
        
        // adding a catch all server block
        blocks.Add(new ConfigurationBlock(BlockType.Server, new List<ConfigurationDirective>()
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
       

        foreach (V1Service item in sourceServices)
        {
            var targetService = targetServices.Find(s => s.Metadata.Name == item.GetNameWithSuffix());
            
            if (targetService == null)
            {
                Console.WriteLine($"creating target service {item.GetNameWithSuffix()}");
                targetService = client.CoreV1.CreateNamespacedService(item.ToTargetService(item.GetNameWithSuffix()), config.Namespace);
            }
            else if (!targetService.IsAcceptable(item.ToTargetService(item.GetNameWithSuffix())))
            {
                Console.WriteLine($"updating target service {item.GetNameWithSuffix()}");


                try
                {
                    targetService = client.CoreV1.ReplaceNamespacedService(item.ToTargetService(item.GetNameWithSuffix()),
                        item.GetNameWithSuffix(), config.Namespace);
                }

                catch (HttpOperationException e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("failed to replace target service - deleting and recreating");
                    client.CoreV1.DeleteNamespacedService(item.GetNameWithSuffix(), config.Namespace, gracePeriodSeconds: 0, propagationPolicy: "Foreground");
                    targetService = client.CoreV1.CreateNamespacedService(item.ToTargetService(item.GetNameWithSuffix()), config.Namespace);
                }
            }
            else
            {
                Console.WriteLine($"target service {targetService.Metadata.Name} is up to date");
            }

            foreach (var port in targetService.Spec.Ports)
            {
                var upstream = new ConfigurationBlock(BlockType.Upstream, $"source-{item.Metadata.Name}-{port.Name}", new List<ConfigurationDirective>()
                {
                    new ConfigurationDirective("server", new ConfigurationValue($"{item.Metadata.Name}:{item.Spec.Ports.ToList().Find(p => p.Name == port.Name).Port}"))
                }.ToArray(), null);
            
                blocks.Add(upstream);

                var location = new ConfigurationBlock(BlockType.Location, "/", new ConfigurationDirective[]
                {
                    new ConfigurationDirective("proxy_cache", new ConfigurationValue("static-cache")), 
                    new ConfigurationDirective("proxy_cache_valid", new ConfigurationValue("any 100m")),
                    new ConfigurationDirective("proxy_cache_use_stale", new ConfigurationValue("error timeout updating http_404 http_500 http_502 http_503 http_504")),
                    new ConfigurationDirective("add_header", new ConfigurationValue("X-Cache-Status $upstream_cache_status")),
                    new ConfigurationDirective("proxy_pass", new ConfigurationValue($"http://source-{item.Metadata.Name}-{port.Name}")),
                }, null);
            
                blocks.Add(new ConfigurationBlock(BlockType.Server, new List<ConfigurationDirective>()
                {
                    // new ConfigurationDirective("server_name", new ConfigurationValue(item.GetNameWithSuffix())),
                    new ConfigurationDirective("listen", new ConfigurationValue(port.Port.ToString()))
                }.ToArray(), location));
            }
        }
        
        var nginxConfig = new ConfigurationBlock(BlockType.Root, rootDirectives.ToArray(),
            new ConfigurationBlock(BlockType.Root, null, blocks.ToArray()));
        
        Console.WriteLine(nginxConfig.ToString());

        V1ConfigMap? configMap = null;
        try
        {
            configMap = (await client.CoreV1.ReadNamespacedConfigMapWithHttpMessagesAsync(
                ControllerConstants.CONFIG_MAP_NAME,
                config.Namespace)).Body;
        }
        catch (Exception e)
        {
            Console.WriteLine("not found - creating config map");
            client.CoreV1.CreateNamespacedConfigMap(CreateConfigMap(nginxConfig), config.Namespace);
        }

        if (configMap == null)
        {
            // TODO: create CM
        }
        else if (!configMap.IsEqual(nginxConfig))
        {
            // TODO: update CM
            Console.WriteLine("updating config map");
            client.CoreV1.ReplaceNamespacedConfigMap(CreateConfigMap(nginxConfig), ControllerConstants.CONFIG_MAP_NAME, config.Namespace);
        }
        else
        {
            Console.WriteLine("enjoy the moment, config map up to date");
        }
    }
    catch (IOException e)
    {
        Console.Error.WriteLine($"watch failed {e.Message}");
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"unhandled exception {e.Message}");
        
        break;
    }
    finally
    {
        Console.Error.WriteLine($"sleeping");
        Thread.Sleep(10000);
    }
}


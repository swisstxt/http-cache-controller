
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

        var configurationBuilder = ConfigurationBuilder.CreateBuilder();

        foreach (V1Service item in sourceServices)
        {
            var targetService = targetServices.Find(s => s.Metadata.Name == item.GetNameWithSuffix());

            if (targetService == null)
            {
                Console.WriteLine($"creating target service {item.GetNameWithSuffix()}");
                targetService = client.CoreV1.CreateNamespacedService(item.ToTargetService(item.GetNameWithSuffix()),
                    config.Namespace);
            }
            else if (!targetService.IsAcceptable(item.ToTargetService(item.GetNameWithSuffix())))
            {
                Console.WriteLine($"updating target service {item.GetNameWithSuffix()}");

                try
                {
                    targetService = client.CoreV1.ReplaceNamespacedService(
                        item.ToTargetService(item.GetNameWithSuffix()),
                        item.GetNameWithSuffix(), config.Namespace);
                }

                catch (HttpOperationException e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("failed to replace target service - deleting and recreating");
                    client.CoreV1.DeleteNamespacedService(item.GetNameWithSuffix(), config.Namespace,
                        gracePeriodSeconds: 0, propagationPolicy: "Foreground");
                    targetService =
                        client.CoreV1.CreateNamespacedService(item.ToTargetService(item.GetNameWithSuffix()),
                            config.Namespace);
                }
            }
            else
            {
                Console.WriteLine($"target service {targetService.Metadata.Name} is up to date");
            }

            configurationBuilder.AddRoutedService(item, targetService);

        }
        var nginxConfig = configurationBuilder.Build();
        
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
            configMap = client.CoreV1.CreateNamespacedConfigMap(CreateConfigMap(nginxConfig), config.Namespace);
        }

        if (!configMap.IsEqual(nginxConfig))
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


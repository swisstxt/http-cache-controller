
using HttpCacheController;
using HttpCacheController.Extensions;
using HttpCacheController.Nginx;
using k8s;
using k8s.Models;

KubernetesClientConfiguration? config = null;

if (KubernetesClientConfiguration.IsInCluster())
{
    config = KubernetesClientConfiguration.InClusterConfig();
}
else
{
    // for local development, load static configuration from file
    config = KubernetesClientConfiguration.BuildConfigFromConfigFile(".kubeconfig.local");
}

while (true)
{
    try
    {
        var client = new Kubernetes(config);

        var services = client.CoreV1.ListNamespacedService(config.Namespace).Items.ToList();
        
        var sourceServices = services.FindAll(s => s.HasAnnotation(ControllerConstants.ANNOTATION_ENABLED, ControllerConstants.ANNOTATION_ENABLED_VALUE));
        
        var targetServices = services.FindAll(s => s.HasAnnotation(ControllerConstants.ANNOTATION_AUTOGEN));

        var configurationBuilder = ConfigurationBuilder.CreateBuilder();
        
        ServicePorts.Init(sourceServices);

        foreach (V1Service sourceService in sourceServices)
        {
            var targetService = targetServices.Find(s => s.Metadata.Name == sourceService.GetNameWithSuffix());

            if (targetService == null)
            {
                Console.WriteLine($"creating target service {sourceService.GetNameWithSuffix()}");
                targetService = client.CoreV1.CreateNamespacedService(sourceService.ToTargetService(sourceService.GetNameWithSuffix()),
                    config.Namespace);
            }
            else if (!targetService.IsAcceptable(sourceService.ToTargetService(sourceService.GetNameWithSuffix())))
            {

                Console.WriteLine($"deleting and recreating target service {sourceService.GetNameWithSuffix()}");
                client.CoreV1.DeleteNamespacedService(sourceService.GetNameWithSuffix(), config.Namespace,
                    gracePeriodSeconds: 0, propagationPolicy: "Foreground");
                targetService =
                    client.CoreV1.CreateNamespacedService(sourceService.ToTargetService(sourceService.GetNameWithSuffix()),
                        config.Namespace);
            }
            else
            {
                Console.WriteLine($"target service {targetService.Metadata.Name} is up to date");
            }

            configurationBuilder.AddRoutedService(sourceService, targetService);

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
            configMap = client.CoreV1.CreateNamespacedConfigMap(Utils.CreateConfigMap(nginxConfig), config.Namespace);
        }

        if (!configMap.IsEqual(nginxConfig))
        {
            Console.WriteLine("updating config map");
            client.CoreV1.ReplaceNamespacedConfigMap(Utils.CreateConfigMap(nginxConfig), ControllerConstants.CONFIG_MAP_NAME, config.Namespace);
        }
        else
        {
            Console.WriteLine("enjoy the moment, config map up to date");
        }
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


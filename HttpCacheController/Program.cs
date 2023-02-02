
using HttpCacheController;
using HttpCacheController.Extensions;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddJsonConsole(configure =>
        {
            configure.TimestampFormat = "HH:mm:ss ";
            configure.UseUtcTimestamp = true;
        });
});

ILogger logger = loggerFactory.CreateLogger<Program>();

KubernetesClientConfiguration? config = null;

if (KubernetesClientConfiguration.IsInCluster())
{
    logger.LogInformation("trying to load in cluster config");
    config = KubernetesClientConfiguration.InClusterConfig();
}
else
{
    logger.LogInformation("trying to load local config");
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
                logger.LogInformation("creating target service {sourceService.GetNameWithSuffix()}", sourceService.GetNameWithSuffix());
                targetService = client.CoreV1.CreateNamespacedService(sourceService.ToTargetService(sourceService.GetNameWithSuffix()),
                    config.Namespace);
            }
            else if (!targetService.IsAcceptable(sourceService.ToTargetService(sourceService.GetNameWithSuffix())))
            {

                logger.LogInformation("deleting and recreating target service {sourceService.GetNameWithSuffix()}", sourceService.GetNameWithSuffix());
                client.CoreV1.DeleteNamespacedService(sourceService.GetNameWithSuffix(), config.Namespace,
                    gracePeriodSeconds: 0, propagationPolicy: "Foreground");
                targetService =
                    client.CoreV1.CreateNamespacedService(sourceService.ToTargetService(sourceService.GetNameWithSuffix()),
                        config.Namespace);
            }
            else
            {
                logger.LogInformation("target service {targetService.Metadata.Name} is up to date", targetService.Metadata.Name);
            }

            configurationBuilder.AddRoutedService(sourceService, targetService);

        }
        var nginxConfig = configurationBuilder.Build();
        
        logger.LogTrace("{nginxConfig.ToString()}" ,nginxConfig.ToString());

        V1ConfigMap? configMap = null;
        
        try
        {
            configMap = (await client.CoreV1.ReadNamespacedConfigMapWithHttpMessagesAsync(
                ControllerConstants.CONFIG_MAP_NAME,
                config.Namespace)).Body;
        }
        catch (Exception e)
        {
            logger.LogInformation("not found - creating config map");
            configMap = client.CoreV1.CreateNamespacedConfigMap(Utils.CreateConfigMap(nginxConfig), config.Namespace);
        }

        if (!configMap.IsEqual(nginxConfig))
        {
            logger.LogInformation("updating config map");
            client.CoreV1.ReplaceNamespacedConfigMap(Utils.CreateConfigMap(nginxConfig), ControllerConstants.CONFIG_MAP_NAME, config.Namespace);
        }
        else
        {
            logger.LogInformation("enjoy the moment, config map up to date");
        }
        
        // check config map hash on cache deployment

        var hash = configMap.GetUniqeHashForConfiguration();

        var cacheDeployment= client.AppsV1.ReadNamespacedDeployment(ControllerConstants.CACHE_DEPLOYMENT_NAME, config.Namespace);

        if (cacheDeployment != null)
        {
            var currentHash = "";
            
            if (cacheDeployment.Spec.Template.Metadata.Annotations != null)
            {
                var labels = cacheDeployment.Spec.Template.Metadata.Annotations;

                if (labels.ContainsKey(ControllerConstants.CONFIG_HASH_LABEL_KEY))
                {
                    currentHash = labels[ControllerConstants.CONFIG_HASH_LABEL_KEY];
                }                
            }
            
            logger.LogInformation("hash - computed: \"{hash}\" current: \"{currentHash}\"", hash, currentHash);

            if (currentHash != hash)
            {
                logger.LogInformation("hash mismatch, update hash on deployment {ControllerConstants.CACHE_DEPLOYMENT_NAME}", ControllerConstants.CACHE_DEPLOYMENT_NAME);
                var patch = new V1Patch(
                    "{\"spec\":{\"template\":{\"metadata\":{\"annotations\":{\""
                    + ControllerConstants.CONFIG_HASH_LABEL_KEY + "\": \"" + hash +
                    "\"}}}}}", V1Patch.PatchType.MergePatch);
                client.AppsV1.PatchNamespacedDeployment(patch, ControllerConstants.CACHE_DEPLOYMENT_NAME, config.Namespace);
            }
        }
        else
        {
            logger.LogWarning("no cache deployment found!");
        }
    }
    catch (Exception e)
    {
        logger.LogCritical("unhandled exception {e.Message}", e.Message);
        break;
    }
    finally
    {
        logger.LogDebug("sleeping for {ControllerConstants.CONTROLLER_SLEEP}ms", ControllerConstants.CONTROLLER_SLEEP);
        Thread.Sleep(ControllerConstants.CONTROLLER_SLEEP);
    }
}


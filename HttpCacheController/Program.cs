using HttpCacheController;
using k8s;
using k8s.Models;

var ANNOTATION_ENABLED_VALUE = "true";
var ANNOTATION_ENABLED = "swisstxt.ch/http-cache-enabled";
var ANNOTATION_TARGET = "swisstxt.ch/http-cache-target";
var ANNOTATION_AUTOGEN = "swisstxt.ch/http-cache-autogen";

var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(".kubeconfig");

void HandleSourceServiceEvent(WatchEventType type, V1Service item)
{
    // TODO: check if target service exists
    // create target service if not
    // update target service if needed
}

void HandleTargetServiceEvent(WatchEventType type, V1Service item)
{
    // TODO: check if source service exists
    // delete target service if not
    // update target service if needed (maybe?)
}

void HandleServiceEvent(WatchEventType type, V1Service item) {
    if (item.HasAnnotation(ANNOTATION_ENABLED, ANNOTATION_ENABLED_VALUE))
    {
        HandleSourceServiceEvent(type, item);
    }
    else if (item.HasAnnotation(ANNOTATION_AUTOGEN))
    {
        HandleTargetServiceEvent(type, item);
    }
}

while (true)
{
    try
    {
        var client = new Kubernetes(config);

        var initialServices = client.CoreV1.ListNamespacedService(config.Namespace);

        if (initialServices != null)
        {
            foreach (V1Service item in initialServices)
            {
                HandleServiceEvent(WatchEventType.Added, item);
            }
        }
        
        var services = client.CoreV1.ListNamespacedServiceWithHttpMessagesAsync(config.Namespace, watch: true);

        await foreach (var (type, item) in services.WatchAsync<V1Service, V1ServiceList>())
        {
            HandleServiceEvent(type, item);
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
        Thread.Sleep(500);
    }
}


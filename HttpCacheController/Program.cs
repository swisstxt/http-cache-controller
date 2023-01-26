using k8s;
using k8s.Models;

var ANNOTATION_ENABLED = "swisstxt.ch/http-cache-enabled";
var ANNOTATION_TARGET = "swisstxt.ch/http-cache-target";
var ANNOTATION_AUTOGEN = "swisstxt.ch/http-cache-autogen";

var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(".kubeconfig");

void handleServiceEvent(WatchEventType type, V1Service item) {
    var annotations = item.Metadata.Annotations;
    if (annotations.ContainsKey(ANNOTATION_ENABLED) && annotations[ANNOTATION_ENABLED] == "true")
    {
        Console.WriteLine($"{item.Metadata.Name} matches [{type}]");
    }
    else if (annotations.ContainsKey(ANNOTATION_AUTOGEN))
    {
        Console.WriteLine($"{item.Metadata.Name} matches  autogen [{type}]");
    }
    else
    {
        Console.WriteLine($"{item.Metadata.Name} does not match");
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
                handleServiceEvent(WatchEventType.Added, item);
            }
        }
        
        var services = client.CoreV1.ListNamespacedServiceWithHttpMessagesAsync(config.Namespace, watch: true);

        await foreach (var (type, item) in services.WatchAsync<V1Service, V1ServiceList>())
        {
            handleServiceEvent(type, item);
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


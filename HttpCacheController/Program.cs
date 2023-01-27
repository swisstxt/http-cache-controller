using HttpCacheController;
using k8s;
using k8s.Models;


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
    if (item.HasAnnotation(ControllerConstants.ANNOTATION_ENABLED, ControllerConstants.ANNOTATION_ENABLED_VALUE))
    {
        HandleSourceServiceEvent(type, item);
    }
    else if (item.HasAnnotation(ControllerConstants.ANNOTATION_AUTOGEN))
    {
        HandleTargetServiceEvent(type, item);
    }
    
}

// while (true)
// {
    try
    {
        var client = new Kubernetes(config);

        var services = client.CoreV1.ListNamespacedService(config.Namespace).Items.ToList();
        
        var sourceServices = services.FindAll(s => s.HasAnnotation(ControllerConstants.ANNOTATION_ENABLED, ControllerConstants.ANNOTATION_ENABLED_VALUE));
        var targetServices = services.FindAll(s => s.HasAnnotation(ControllerConstants.ANNOTATION_AUTOGEN));

        foreach (V1Service item in sourceServices)
        {
            var targetService = targetServices.Find(s => s.Metadata.Name == item.GetNameWithSuffix());
            
            if (targetService == null)
            {
                Console.WriteLine($"creating target service {item.GetNameWithSuffix()}");
                client.CoreV1.CreateNamespacedService(item.ToTargetService(item.GetNameWithSuffix()), config.Namespace);
            }
            else if (!targetService.IsAcceptable(item.ToTargetService(item.GetNameWithSuffix())))
            {
                Console.WriteLine($"updating target service {item.GetNameWithSuffix()}");
                client.CoreV1.ReplaceNamespacedService(item.ToTargetService(item.GetNameWithSuffix()), item.GetNameWithSuffix(), config.Namespace);
            }
        }
    }
    catch (IOException e)
    {
        Console.Error.WriteLine($"watch failed {e.Message}");
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"unhandled exception {e.Message}");
        
        //break;
    }
    finally
    {
        Console.Error.WriteLine($"sleeping");
        Thread.Sleep(500);
    }


// }



using HttpCacheController;
using HttpCacheController.Nginx;
using k8s;
using k8s.Models;

List<ConfigurationDirective> rootDirectives = new List<ConfigurationDirective>()
{
    new ConfigurationDirective("daemon", new ConfigurationValue("off")),
    new ConfigurationDirective("worker_processes", new ConfigurationValue("1")),
    new ConfigurationDirective("pid", new ConfigurationValue("/tmp/nginx.pid")),
};

List<ConfigurationDirective> serverDirectives = new List<ConfigurationDirective>()
{
    new ConfigurationDirective("listen", 
new ConfigurationValue("80"),
            new ConfigurationValue("proxy_protocol"),
            new ConfigurationValue("default_server"), new ConfigurationValue("reuseport")
    ),
};

var nginxConfig = new ConfigurationBlock(BlockType.Root, rootDirectives.ToArray(),
    new ConfigurationBlock(BlockType.Http, null, 
    new ConfigurationBlock(BlockType.Server, serverDirectives.ToArray(), 
    new ConfigurationBlock(BlockType.Location, "/", null)
)));

Console.WriteLine(nginxConfig);

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
            else
            {
                Console.WriteLine($"target service {targetService.Metadata.Name} is up to date");
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


using k8s.Models;

namespace HttpCacheController;

public static class ServicePorts
{
    private static readonly Dictionary<string, List<V1ServicePort>> servicePorts = new();

    public static void Init(IEnumerable<V1Service> sourceServices)
    {
        servicePorts.Clear();
        var currentPort = 9000;

        foreach (var service in sourceServices.OrderBy(s => s.Metadata.Name))
        {
            var ports = new List<V1ServicePort>();

            foreach (var port in service.Spec.Ports) ports.Add(new V1ServicePort(currentPort++, name: port.Name));
            servicePorts.Add(service.Metadata.Name, ports);
        }
    }

    public static List<V1ServicePort> GetPortsForService(string serviceName)
    {
        return servicePorts[serviceName];
    }
}
using System.ComponentModel;
using k8s.Models;

namespace HttpCacheController;

public static class ServicePorts
{
    private static int currentPort = 9000;

    private static Dictionary<string, List<V1ServicePort>> servicePorts = new() {};

    public static void Init(IEnumerable<V1Service> sourceServices)
    {
        servicePorts.Clear();
        
        foreach (var service in sourceServices.OrderBy(s => s.Metadata.Name))
        {
            var ports = new List<V1ServicePort>();

            foreach (var port in service.Spec.Ports)
            {
                ports.Add(new V1ServicePort(port: currentPort++, name: port.Name));
            }
            servicePorts.Add(service.Metadata.Name, ports);
        }
    }

    public static List<V1ServicePort> GetPortsForService(string serviceName)
    {
        return servicePorts[serviceName];
    }
}
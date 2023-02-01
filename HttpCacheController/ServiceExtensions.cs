using k8s.Models;

namespace HttpCacheController;

public static class ServiceExtensions
{
    public static bool IsAcceptable(this V1Service x, V1Service y)
    {

        for (var i = 0; i < x.Spec.Ports.Count; i++)
        {
            if (x.Spec.Ports[i].Name != y.Spec.Ports[i].Name)
            {
                return false;
            }
        }
        
        foreach (var (key, value) in x.Spec.Selector)
        {
            if (!y.Spec.Selector.ContainsKey(key) || y.Spec.Selector[key] != value)
            {
                return false;
            }
        }

        return x.Metadata.Name == y.Metadata.Name;
    }

    public static V1Service ToTargetService(this V1Service source, string targetName)
    {
        var target = new V1Service
        {
            Metadata = new V1ObjectMeta
            {
                Name = targetName,
                Annotations = new Dictionary<string, string>
                {
                    { ControllerConstants.ANNOTATION_AUTOGEN, ControllerConstants.ANNOTATION_AUTOGEN_VALUE }
                }
            },
            Spec = new V1ServiceSpec
            {
                Ports = ServicePorts.GetPortsForService(source.Metadata.Name),
                Selector = new Dictionary<string, string>()
                {
                    {
                        ControllerConstants.NGINX_SELECTOR_KEY,
                        ControllerConstants.NGINX_SELECTOR_VALUE
                    }
                }
            }
        };

        return target;
    }
    
    
    public static string GetNameWithSuffix(this V1Service svc)
    {
        return svc.Metadata.Name + ControllerConstants.SERVICE_NAME_SUFFIX;
    }
    
    public static bool HasAnnotation(this V1Service svc, string expectedAnnotation, string? expectedValue = null)
    {
        var annotations = svc.Metadata.Annotations;

        if (annotations == null)
        {
            return false;
        }
        
        return expectedValue != null
            ? annotations.ContainsKey(expectedAnnotation) &&
              annotations[expectedAnnotation] == expectedValue
            : annotations.ContainsKey(expectedAnnotation); 
    }
}
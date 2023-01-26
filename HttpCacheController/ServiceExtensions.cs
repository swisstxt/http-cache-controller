using k8s.Models;

namespace HttpCacheController;

public static class ServiceExtensions
{
    public static bool Equals(this V1Service source, V1Service otherService)
    {
        return source.Metadata.Name == otherService.Metadata.Name &&
               source.Spec.Ports.SequenceEqual(otherService.Spec.Ports) &&
               source.Spec.Selector.SequenceEqual(otherService.Spec.Selector);
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
                Ports = source.Spec.Ports,
                Selector = source.Spec.Selector
            }
        };

        return target;
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
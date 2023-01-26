using k8s.Models;

namespace HttpCacheController;

public static class ServiceExtensions
{
    public static V1Service SourceServiceToTargetService(V1Service source, string targetName)
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
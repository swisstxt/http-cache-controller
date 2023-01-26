using k8s.Models;

namespace HttpCacheController;

public static class ServiceExtensions
{
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
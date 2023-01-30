using HttpCacheController.Nginx;
using k8s.Models;

namespace HttpCacheController;

public static class ConfigMapExtensions
{
    public static bool IsEqual(this V1ConfigMap cm, V1ConfigMap otherCm)
    {
        var cmValue = cm.Data[ControllerConstants.NGINX_CONFIG_KEY];
        var otherCmValue = otherCm.Data[ControllerConstants.NGINX_CONFIG_KEY];
        return cmValue == otherCmValue;
    }
    
    public static bool IsEqual(this V1ConfigMap cm, ConfigurationBlock block)
    {
        var cmValue = cm.Data[ControllerConstants.NGINX_CONFIG_KEY];
        return cmValue == block.ToString();
    }
}
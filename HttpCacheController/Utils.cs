using HttpCacheController.Nginx;
using k8s.Models;

namespace HttpCacheController;

public static class Utils
{
    public static V1ConfigMap CreateConfigMap(ConfigurationBlock nginxConfig)
    {
        var newCm = new V1ConfigMap()
        {
            Metadata = new V1ObjectMeta()
            {
                Name = ControllerConstants.CONFIG_MAP_NAME
            },
            Data = new Dictionary<string, string>()
            {
                { ControllerConstants.NGINX_CONFIG_KEY, nginxConfig.ToString() }
            }
        };

        return newCm;
    }
}
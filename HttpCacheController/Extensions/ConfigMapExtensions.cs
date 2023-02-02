using System.Security.Cryptography;
using System.Text;
using HttpCacheController.Nginx;
using k8s.Models;

namespace HttpCacheController.Extensions;

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

    public static string GetUniqeHashForConfiguration(this V1ConfigMap cm)
    {
        using (SHA1 hash = SHA1.Create())  
        {  
            byte[] bytes = SHA1.HashData(Encoding.UTF8.GetBytes(cm.Data[ControllerConstants.NGINX_CONFIG_KEY]));
  
            StringBuilder builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        } 
    }
}
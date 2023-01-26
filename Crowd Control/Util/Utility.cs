using CrowdControl.ChatHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CrowdControl.Util
{
    internal static class Utility
    {
        internal static class LoadInternalFile
        {
            public static string TextFile(string resourceName)
            {
                string ResourceFileName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith(resourceName));
                return new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName)).ReadToEnd();
            }
            public static ChatCommands JsonFile(string resourceName)
            {
                string ResourceFileName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith(resourceName));
                return JsonConvert.DeserializeObject<ChatCommands>(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName)).ReadToEnd());
            }
        }
    }
}

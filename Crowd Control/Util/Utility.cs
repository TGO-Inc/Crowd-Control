using CrowdControl.ChatHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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
                return new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName)!).ReadToEnd();
            }
            public static T JsonFile<T> (string resourceName)
            {
                string ResourceFileName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith(resourceName));
                return JsonConvert.DeserializeObject<T>(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName)!).ReadToEnd())!;
            }
            public static IEnumerable<(string FileName, string FileContent)> AllFiles(string resourceName)
            {
                var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(str => str.EndsWith(resourceName));
                foreach (var resource in resources)
                {
                    yield return (
                        string.Join('.', resource.Split('.')[^2..]),
                        new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)!).ReadToEnd()
                    );
                }
            }
        }
    }
}

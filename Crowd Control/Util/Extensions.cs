using CrowdControl.ChatHandler;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace CrowdControl.Extensions
{
    internal static class Extensions
    {
        public static string CapitilzeFirst(this string s)
        {
            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s);
        }
        public static string ToString(this List<ChatCommand> c, bool _ = false)
        {
            string ret = string.Empty;
            if (c.Count > 1)
            {
                ret = "[ " + string.Join(", ", c) + " ]";
            }
            else if (c.Count == 1)
            {
                ret = c[0].ToString(false);
            }
            return ret;
        }
        public static JToken ToJToken<TKey, TValue>(this Dictionary<TKey, TValue> c, bool _ = false)
        {
            var ret = new JArray();
            foreach(var chunk in c)
            {
                ret.Add(
                    JToken.FromObject(chunk)
                );
            }
            return ret;
        }
        public static JToken ToJToken(this List<ChatCommand> c, bool _ = false)
        {
            var ret = JArray.Parse("[ \"" + string.Join("\", \"", c) + "\" ]");
            if (ret.Count < 2)
                return ret[0];
            else
                return ret;
        }
    }
}

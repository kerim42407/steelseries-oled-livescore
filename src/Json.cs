using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace OledLiveScore
{
    // Thin dynamic accessors over JavaScriptSerializer output (in-box, no extra DLLs).
    // Objects become Dictionary<string, object>, arrays become object[].
    internal static class Json
    {
        private static readonly JavaScriptSerializer Ser =
            new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 512 };

        public static object Parse(string s) { return Ser.DeserializeObject(s); }
        public static string Write(object o) { return Ser.Serialize(o); }

        public static object Get(object node, string key)
        {
            var d = node as Dictionary<string, object>;
            object v;
            if (d != null && d.TryGetValue(key, out v)) return v;
            return null;
        }

        public static object Path(object node, params string[] keys)
        {
            foreach (var k in keys)
            {
                node = Get(node, k);
                if (node == null) return null;
            }
            return node;
        }

        public static object[] Arr(object node)
        {
            return node as object[] ?? new object[0];
        }

        public static string Str(object node)
        {
            return node == null ? "" : node.ToString();
        }

        public static int Int(object node)
        {
            if (node == null) return 0;
            int i;
            if (int.TryParse(node.ToString(), out i)) return i;
            double d;
            if (double.TryParse(node.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return (int)d;
            return 0;
        }

        public static bool Bool(object node)
        {
            return node is bool && (bool)node;
        }
    }
}

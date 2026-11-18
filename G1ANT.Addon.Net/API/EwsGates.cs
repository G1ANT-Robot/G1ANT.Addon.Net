using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G1ANT.Addon.Net.API
{
    internal static class EwsGate
    {
        private static bool _ready;

        public static void EnsureReady()
        {
            if (_ready) return;

            var folder = System.IO.Path.GetDirectoryName(typeof(EwsGate).Assembly.Location);

            Unpack(folder, "Microsoft.Exchange.WebServices.dll", Properties.Resources.Microsoft_Exchange_WebServices);
            Unpack(folder, "Microsoft.Exchange.WebServices.Auth.dll", Properties.Resources.Microsoft_Exchange_WebServices_Auth);

            _ready = true;
        }

        private static void Unpack(string folder, string name, byte[] bytes)
        {
            var path = System.IO.Path.Combine(folder, name);

            if (System.IO.File.Exists(path) && new System.IO.FileInfo(path).Length == bytes.Length) return;

            System.IO.File.WriteAllBytes(path, bytes);
        }
    }
}

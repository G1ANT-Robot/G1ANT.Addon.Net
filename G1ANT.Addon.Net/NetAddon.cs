/**
*    Copyright(C) G1ANT Robot Ltd, All rights reserved
*    Solution G1ANT.Addon.net, Project G1ANT.Addon.Net
*    www.g1ant.com
*
*    Licensed under the G1ANT license.
*    See License.txt file in the project root for full license information.
*
*/
using G1ANT.Addon.Net.Properties;
using G1ANT.Language;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace G1ANT.Addon.Net
{
    [Addon(Name = "Net", Tooltip = "Net Commands")]
    [Copyright(Author = "G1ANT Robot Ltd", Copyright = "G1ANT Robot Ltd", Email = "hi@g1ant.com", Website = "www.g1ant.com")]
    [License(Type = "LGPL", ResourceName = "License.txt")]
    [CommandGroup(Name = "as400", Tooltip = "A command used to work with IBM AS/400 platform.")]
    [CommandGroup(Name = "is", Tooltip = "A command used for checking sth.")]
    [CommandGroup(Name = "mail", Tooltip = "A command used with email")]
    [CommandGroup(Name = "rest", Tooltip = "This command prepares a request to the desired url with selected method.")]
    [CommandGroup(Name = "vnc", Tooltip = "A command connected with a VNC server.")]
    public class NetAddon : Language.Addon
    {
        private static Assembly _executingAssembly = Assembly.GetExecutingAssembly();
        private static string[] _embeddedResources = _executingAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray();

        public override void Initialize()
        {
            CreateToDiskEmbeddedManifests();
            base.Initialize();
        }

        public override void LoadDlls()
        {
            //CreateToDiskEmbeddedManifests();
            UnpackDrivers();
            base.LoadDlls();

            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = $"{new AssemblyName(args.Name).Name}.dll";

            var resourceName = _embeddedResources.FirstOrDefault(l => l.EndsWith(assemblyName));

            if (resourceName == null) return null;

            var manifests = new List<string>()
            {
                 "Microsoft.Exchange.WebServices",
                 "Microsoft.Exchange.WebServices.Auth"
            };

            if (manifests.Contains(resourceName))
            {
                var unpackFolder = Path.GetDirectoryName(typeof(NetAddon).Assembly.Location);

                using (var stream = _executingAssembly.GetManifestResourceStream(resourceName))
                {
                    return Assembly.LoadFile(Path.Combine(unpackFolder, $"{resourceName}.dll"));
                }
            }

            return null;
        }

        private void UnpackDrivers()
        {
            var unpackFolder = AbstractSettingsContainer.Instance.UserDocsAddonFolder.FullName;
            var embeddedResourceDictionary = new Dictionary<string, byte[]>()
            {
                { "putty.exe", Resources.putty },
                { "VNC.exe", Resources.VNC },
                { "telnet.exe", Resources.telnet }
            };

            foreach (var embededResource in embeddedResourceDictionary.Where(e => !DoesFileExist(unpackFolder, e.Key) || !AreFilesOfTheSameLength(e.Value.Length, unpackFolder, e.Key)))
            {
                try
                {
                    using (FileStream stream = File.Create(Path.Combine(unpackFolder, embededResource.Key)))
                    {
                        stream.Write(embededResource.Value, 0, embededResource.Value.Length);
                    }
                }
                catch (Exception ex) { RobotMessageBox.Show(ex.Message); }
            }
        }

        private void CreateToDiskEmbeddedManifests()
        {
            var unpackFolder = Path.GetDirectoryName(typeof(NetAddon).Assembly.Location);
            if (string.IsNullOrEmpty(unpackFolder)) return;

            var embeddedResourceDictionary = new Dictionary<string, byte[]>()
            {
                { "Microsoft.Exchange.WebServices.dll", Resources.Microsoft_Exchange_WebServices },
                { "Microsoft.Exchange.WebServices.Auth.dll", Resources.Microsoft_Exchange_WebServices_Auth }
            };

            foreach (var embededResource in embeddedResourceDictionary.Where(e => !DoesFileExist(unpackFolder, e.Key) || !AreFilesOfTheSameLength(e.Value.Length, unpackFolder, e.Key)))
            {
                try
                {
                    var dllpath = Path.Combine(unpackFolder, embededResource.Key);

                    using (FileStream stream = File.Create(dllpath))
                    {
                        stream.Write(embededResource.Value, 0, embededResource.Value.Length);
                    }

                    //Assembly.LoadFile(dllpath);
                }
                catch (Exception ex) { RobotMessageBox.Show(ex.Message); }
            }
        }

        private bool DoesFileExist(string folder, string fileName)
        {
            return File.Exists(Path.Combine(folder, fileName));
        }

        private bool AreFilesOfTheSameLength(int length, string folder, string fileName)
        {
            return length == new FileInfo(Path.Combine(folder, fileName)).Length;
        }

    }
}
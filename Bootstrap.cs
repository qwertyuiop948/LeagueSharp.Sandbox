using System;
using System.Diagnostics;
using LeagueSharp.Loader.Service;

namespace LeagueSharp.Sandbox
{
    public class Bootstrap
    {
        private static Domain _applicationDomain;

        public static void Init()
        {
            CreateApplicationDomain();
        }

        public static bool Load()
        {
            if (_applicationDomain != null)
            {
                var api = ServiceFactory.GetInterface<ILoaderService>();
                try
                {
                    var assemblies = api.GetAssemblyList(Process.GetCurrentProcess().Id);
                    foreach (var assembly in assemblies)
                    {
                        _applicationDomain.Load(assembly.PathToBinary, new string[0]);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Sandbox.Bootstrap has encountered an error:");
                    Console.WriteLine(e);
                }
                return true;
            }
            return false;
        }

        private static void CreateApplicationDomain()
        {
            if (_applicationDomain == null)
            {
                _applicationDomain = Domain.CreateProxy("SandboxDomain");
            }
        }
    }
}
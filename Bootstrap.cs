using System;
using System.Diagnostics;
using LeagueSharp.Loader.Service;

namespace LeagueSharp.Sandbox
{
    public class Bootstrap
    {
        private static Domain _applicationDomain;
        public static uint ReloadKey = 116U;
        public static uint ReloadAndRecompileKey = 119U;

        public static void Init()
        {
            Input.SubclassHWnd(Process.GetCurrentProcess().MainWindowHandle);

            CreateApplicationDomain();
            Load();
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
                        _applicationDomain.Load(assembly.PathToBinary, new string[1]);
                    }
                }
                catch (Exception e)
                {
                    ServiceFactory.GetInterface<ILoaderLogService>()
                        .ErrorFormat("[PID:%d] Sandbox.Boostrap error at Load(): %s", Process.GetCurrentProcess().Id, e);
                    Console.WriteLine("Sandbox.Bootstrap has encountered an error:");
                    Console.WriteLine(e);
                }
                return true;
            }
            return false;
        }

        public static void Reload()
        {
            Unload();
            CreateApplicationDomain();
            Load();
        }

        public static void Recompile()
        {
            Unload();

            try
            {
                ServiceFactory.GetInterface<ILoaderService>().Recompile(Process.GetCurrentProcess().Id);
            }
            catch (Exception e)
            {
                ServiceFactory.GetInterface<ILoaderLogService>()
                    .ErrorFormat(
                        "[PID:%d] Sandbox.Boostrap error at Recompile(): %s", Process.GetCurrentProcess().Id, e);
                Console.WriteLine("Sandbox.Bootstrap has encountered an error:");
                Console.WriteLine(e);
            }

            CreateApplicationDomain();
            Load();
        }

        public static void Unload()
        {
            if (_applicationDomain == null)
            {
                return;
            }

            try
            {
                Domain.UnloadProxy(_applicationDomain);
            }
            finally
            {
                _applicationDomain = null;
            }
        }

        private static void CreateApplicationDomain()
        {
            if (_applicationDomain == null)
            {
                _applicationDomain = Domain.CreateProxy("SandboxDomain");

                try
                {
                    ReloadKey = ServiceFactory.GetInterface<Configuration>().ReloadKey;
                    ReloadAndRecompileKey = ServiceFactory.GetInterface<Configuration>().ReloadAndRecompileKey;
                }
                catch (Exception e)
                {
                    ServiceFactory.GetInterface<ILoaderLogService>()
                        .ErrorFormat(
                            "[PID:%d] Sandbox.Boostrap error at CreateApplicationDomain(): %s",
                            Process.GetCurrentProcess().Id, e);
                    Console.WriteLine("Sandbox.Bootstrap has encountered an error:");
                    Console.WriteLine(e);
                }
            }
        }
    }
}
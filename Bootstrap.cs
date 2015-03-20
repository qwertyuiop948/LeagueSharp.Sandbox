using System;
using System.Diagnostics;
using LeagueSharp.Loader.Service;

namespace LeagueSharp.Sandbox
{
    public class Bootstrap
    {
        public static Configuration Config;
        private static Domain _applicationDomain;

        public static int Pid
        {
            get { return Process.GetCurrentProcess().Id; }
        }

        public static uint ReloadAndRecompileKey
        {
            get { return Config == null ? 0x76 : Config.ReloadKey; }
        }

        public static uint ReloadKey
        {
            get { return Config == null ? 0x74 : Config.ReloadKey; }
        }

        public static uint UnloadKey
        {
            get { return Config == null ? 0x75 : Config.ReloadKey; }
        }

        public static void Init()
        {
            Config = ServiceFactory.GetInterface<ILoaderService>().GetConfiguration(Pid);
            // TODO: login and configure core

            CreateApplicationDomain();
            Load();

            Input.SubclassHWnd(Process.GetCurrentProcess().MainWindowHandle);
        }

        public static bool Load()
        {
            if (_applicationDomain == null)
            {
                return false;
            }

            try
            {
                var assemblies =
                    ServiceFactory.GetInterface<ILoaderService>().GetAssemblyList(Process.GetCurrentProcess().Id);

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
            if (_applicationDomain != null)
            {
                return;
            }

            try
            {
                _applicationDomain = Domain.CreateProxy("SandboxDomain");
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
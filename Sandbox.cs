using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LeagueSharp.Sandbox.Shared;

namespace LeagueSharp.Sandbox
{
    public class Sandbox
    {
        [DllImport("kernel32")]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();

        public static Configuration Config;
        private static Domain _applicationDomain;

        public static int Pid
        {
            get { return Process.GetCurrentProcess().Id; }
        }

        public static int ReloadAndRecompileKey
        {
            get { return Config == null ? 0x76 : Config.ReloadAndRecompileKey; }
        }

        public static int ReloadKey
        {
            get { return Config == null ? 0x74 : Config.ReloadKey; }
        }

        public static int UnloadKey
        {
            get { return Config == null ? 0x75 : Config.UnloadKey; }
        }

        public static int Bootstrap(string param)
        {
            try
            {
                Logs.InfoFormat("[PID:{0}] Sandbox.Bootstrap: Started ({1})", Pid, param);
                Reload();
                Input.SubclassHWnd(Process.GetCurrentProcess().MainWindowHandle);
            }
            catch (Exception e)
            {
                Logs.ErrorFormat("[PID:{0}] Sandbox.Bootstrap: {1}", Pid, e.ToString());
                return 1;
            }

            return 0;
        }

        public static bool Load()
        {
            if (_applicationDomain == null)
            {
                return false;
            }

            try
            {
                Logs.InfoFormat("[PID:{0}] Sandbox.Load: Load Assemblies", Pid);
                var assemblies = ServiceFactory.CreateProxy<ILoaderService>().GetAssemblyList(Pid);

                foreach (var assembly in assemblies)
                {
                    _applicationDomain.Load(assembly.PathToBinary, new string[1]);
                }
            }
            catch (Exception e)
            {
                Logs.ErrorFormat("[PID:{0}] Sandbox.Load: {0}", Pid, e.ToString());
            }
            return true;
        }

        public static void Reload()
        {
            try
            {
                Config = ServiceFactory.CreateProxy<ILoaderService>().GetConfiguration(Pid);
                Hacks.AntiAFK = Config.AntiAfk;
                Hacks.TowerRanges = Config.TowerRange;
                Hacks.ZoomHack = Config.ExtendedZoom;

                if (Config.Console)
                {
                    AllocConsole();
                }
                else
                {
                    FreeConsole();
                }
            }
            catch(Exception e)
            {
                Logs.ErrorFormat("[PID:{0}] Sandbox.Reload: {1}", Pid, e.ToString());
            }

            Unload();
            CreateApplicationDomain();
            Load();
        }

        public static void Recompile()
        {
            Unload();

            try
            {
                Logs.InfoFormat("[PID:{0}] Sandbox.Recompile: Request Recompile", Pid);
                ServiceFactory.CreateProxy<ILoaderService>().Recompile(Process.GetCurrentProcess().Id);
            }
            catch (Exception e)
            {
                Logs.ErrorFormat("[PID:{0}] Sandbox.Recompile: {1}", Pid, e.ToString());
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
                Logs.InfoFormat("[PID:{0}] Sandbox.Unload: Unload", Pid);
                Domain.UnloadProxy(_applicationDomain);
            }
            catch (Exception e)
            {
                Logs.ErrorFormat("[PID:{0}] Sandbox.Unload: {1}", Pid, e.ToString());
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
                Logs.InfoFormat("[PID:{0}] Sandbox.CreateApplicationDomain: Create", Pid);
                _applicationDomain = Domain.CreateProxy("SandboxDomain");

                if (_applicationDomain == null)
                {
                    Logs.InfoFormat("[PID:{0}] Sandbox.CreateApplicationDomain: creation failed :(", Pid);
                }
                else
                {
                    Logs.ErrorFormat("[PID:{0}] Sandbox.CreateApplicationDomain: created #{1}", Pid,
                        _applicationDomain.AppDomain.Id);
                }
            }
            catch (Exception e)
            {
                Logs.ErrorFormat("[PID:{0}] Sandbox.CreateApplicationDomain: {1}", Pid, e.ToString());
            }
        }
    }
}
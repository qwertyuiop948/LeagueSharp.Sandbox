using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LeagueSharp.Sandbox
{
    public class Bootstrap
    {
        private static readonly List<string> LoadedAssemblies = new List<string>();
        private static DomainProxy _mainDomainProxy;
        private static ProcessInputDelegate _processInputDelegate;

        public static void Init(string param)
        {
            
        }

        private static void ProcessInput(ProcessAction action, string args)
        {
            if (_mainDomainProxy == null)
            {
                _mainDomainProxy = DomainProxy.Create("LeagueSharp\\Sandbox");
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate void ProcessInputDelegate(ProcessAction action, string args);
    }

    public enum ProcessAction
    {
        Load,
        Unload
    }
}
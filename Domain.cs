using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

namespace LeagueSharp.Sandbox
{
    public class Domain : MarshalByRefObject
    {
        public AppDomain AppDomain { get; private set; }

        static Domain()
        {
            AppDomain.CurrentDomain.AssemblyResolve += DomainOnAssemblyResolve;
            AppDomain.CurrentDomain.UnhandledException += DomainOnUnhandledException;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        public static Domain CreateProxy(string domainName)
        {
            try
            {
                if (string.IsNullOrEmpty(domainName))
                {
                    domainName = "Sandbox" + Guid.NewGuid().ToString("N") + "Domain";
                }

                Logs.InfoFormat("[PID:{0}] Sandbox.CreateProxy: Domain {1}", Sandbox.Pid, domainName);
                Logs.InfoFormat("[PID:{0}] Sandbox.CreateProxy: Configuration\n{1}", Sandbox.Pid,
                    Sandbox.Config.ToString());

                var info = new AppDomainSetup
                {
                    ApplicationName = domainName,
                    ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\",
                    ShadowCopyFiles = "true",
                    CachePath = Path.Combine(Sandbox.Config.DataDirectory, "SandboxCache")
                };

                Logs.InfoFormat(
                    "[PID:{0}] Sandbox.CreateProxy:\nApplicationName:{1}\nApplicationBase:{2}\nCachePath:{3}",
                    Sandbox.Pid, info.ApplicationName, info.ApplicationBase, info.CachePath);

                var grantSet = Sandbox.Config.Permissions ?? new PermissionSet(PermissionState.Unrestricted);
                //grantSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                //grantSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, Sandbox.Config.DataDirectory));
                //grantSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, Assembly.GetExecutingAssembly().Location));
                //grantSet.AddPermission(new ReflectionPermission(PermissionState.Unrestricted));

                var strongNameLeagueSharp = new StrongName(new StrongNamePublicKeyBlob(
                    new byte[]
                    {
                        0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00,
                        0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31,
                        0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x2d, 0xc3, 0x2b, 0xa0,
                        0x24, 0xf4, 0xe8, 0x1d, 0xa5, 0x51, 0x39, 0xa6, 0x35, 0x7f, 0x64, 0x1e,
                        0x9a, 0xa4, 0xee, 0x01, 0x63, 0x71, 0x1a, 0x8e, 0x78, 0xa1, 0xca, 0x38,
                        0x26, 0x3a, 0x77, 0x69, 0x7b, 0x0d, 0xa3, 0xb6, 0x33, 0x2e, 0xd3, 0xbc,
                        0x1a, 0x41, 0xd8, 0xac, 0x0f, 0xf7, 0xcb, 0x1c, 0x1d, 0x54, 0x6e, 0xe8,
                        0x85, 0x19, 0xeb, 0xe8, 0xb3, 0xf7, 0x99, 0xc9, 0xf9, 0x79, 0x81, 0x6a,
                        0xec, 0x65, 0x47, 0x78, 0x4c, 0xe4, 0x1d, 0x10, 0x19, 0x0b, 0x6f, 0x47,
                        0xf0, 0x96, 0x6b, 0x87, 0x03, 0xdf, 0xb1, 0x11, 0x95, 0xc5, 0x5e, 0xf0,
                        0x65, 0x4c, 0x5c, 0xd8, 0x5a, 0x61, 0xa6, 0xb4, 0x51, 0x49, 0x31, 0x40,
                        0x0f, 0xe1, 0xb5, 0x01, 0xf5, 0xe2, 0x65, 0x4f, 0x92, 0xf4, 0x06, 0x6b,
                        0xb4, 0xe8, 0xd3, 0x23, 0x98, 0xe7, 0x82, 0x44, 0x7e, 0xbe, 0xbe, 0x3f,
                        0xf7, 0x65, 0x36, 0xac
                    }
                    ),
                    "LeagueSharp",
                    new Version(1, 0, 0, 25));

                var strongNameSharpDX = new StrongName(new StrongNamePublicKeyBlob(
                    new byte[]
                    {
                        0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00,
                        0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
                        0x45, 0x43, 0xd7, 0x7b, 0x41, 0x22, 0x2c, 0xfd, 0x48, 0xf4, 0xe0, 0xd8, 0xdd, 0x9b, 0x2f, 0x83,
                        0xdc, 0x15, 0xfb, 0xed, 0xe3, 0x12, 0xa4, 0x22, 0xa7, 0x45, 0x4a, 0x0b, 0x72, 0x3e, 0x98, 0x87,
                        0x18, 0xeb, 0xba, 0x61, 0x97, 0x73, 0xfc, 0x8d, 0xfe, 0xd2, 0xbc, 0x69, 0xc9, 0x7a, 0xec, 0x40,
                        0x63, 0xf5, 0x1d, 0xc5, 0x82, 0x1f, 0x5e, 0xaa, 0x72, 0xf3, 0x31, 0xb2, 0x78, 0x27, 0x55, 0x75,
                        0x4d, 0xfd, 0x99, 0x8a, 0xde, 0x0d, 0xcb, 0xf9, 0x2a, 0x73, 0x4e, 0x53, 0x28, 0x70, 0xf6, 0x61,
                        0xcb, 0xe4, 0x38, 0x8f, 0x54, 0x4b, 0xef, 0xa2, 0xf3, 0x2a, 0x8e, 0x45, 0x68, 0xe0, 0xbe, 0x07,
                        0x1a, 0x90, 0xfa, 0x54, 0x6c, 0x8b, 0x4e, 0x6e, 0xfc, 0xea, 0x75, 0x57, 0x03, 0xae, 0x03, 0xf6,
                        0x47, 0x9e, 0x78, 0x76, 0x32, 0x68, 0x8b, 0xe8, 0xf6, 0xaa, 0xae, 0x80, 0x8f, 0x6f, 0x43, 0xba
                    }
                    ),
                    "SharpDX",
                    new Version(2, 6, 3, 0));

                var strongNameSharpDXDirect3D9 = new StrongName(new StrongNamePublicKeyBlob(
                    new byte[]
                    {
                        0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00,
                        0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
                        0x45, 0x43, 0xd7, 0x7b, 0x41, 0x22, 0x2c, 0xfd, 0x48, 0xf4, 0xe0, 0xd8, 0xdd, 0x9b, 0x2f, 0x83,
                        0xdc, 0x15, 0xfb, 0xed, 0xe3, 0x12, 0xa4, 0x22, 0xa7, 0x45, 0x4a, 0x0b, 0x72, 0x3e, 0x98, 0x87,
                        0x18, 0xeb, 0xba, 0x61, 0x97, 0x73, 0xfc, 0x8d, 0xfe, 0xd2, 0xbc, 0x69, 0xc9, 0x7a, 0xec, 0x40,
                        0x63, 0xf5, 0x1d, 0xc5, 0x82, 0x1f, 0x5e, 0xaa, 0x72, 0xf3, 0x31, 0xb2, 0x78, 0x27, 0x55, 0x75,
                        0x4d, 0xfd, 0x99, 0x8a, 0xde, 0x0d, 0xcb, 0xf9, 0x2a, 0x73, 0x4e, 0x53, 0x28, 0x70, 0xf6, 0x61,
                        0xcb, 0xe4, 0x38, 0x8f, 0x54, 0x4b, 0xef, 0xa2, 0xf3, 0x2a, 0x8e, 0x45, 0x68, 0xe0, 0xbe, 0x07,
                        0x1a, 0x90, 0xfa, 0x54, 0x6c, 0x8b, 0x4e, 0x6e, 0xfc, 0xea, 0x75, 0x57, 0x03, 0xae, 0x03, 0xf6,
                        0x47, 0x9e, 0x78, 0x76, 0x32, 0x68, 0x8b, 0xe8, 0xf6, 0xaa, 0xae, 0x80, 0x8f, 0x6f, 0x43, 0xba
                    }
                    ),
                    "SharpDX.Direct3D9",
                    new Version(2, 6, 3, 0));

                var domain = AppDomain.CreateDomain(domainName, null, info, grantSet);
                    //Assembly.GetExecutingAssembly().Evidence.GetHostEvidence<StrongName>(), strongNameLeagueSharp, strongNameSharpDX, strongNameSharpDXDirect3D9);

                var handle = Activator.CreateInstanceFrom(domain, Assembly.GetExecutingAssembly().Location,
                    typeof (Domain).FullName);

                var wrappedDomain = handle.Unwrap() as Domain;

                if (wrappedDomain != null)
                {
                    wrappedDomain.AppDomain = domain;
                    return wrappedDomain;
                }
            }
            catch (Exception e)
            {
                Logs.InfoFormat("[PID:{0}] Sandbox.CreateProxy: {1}", Sandbox.Pid, e.ToString());
            }

            return null;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private static Assembly DomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Logs.InfoFormat("[PID:{0}] Sandbox.DomainOnAssemblyResolve: {1}", Sandbox.Pid, args.Name);

            if (args.Name.Contains(".resources"))
            {
                return null;
            }

            var strArray = args.Name.Split(',');


            if (Assembly.GetExecutingAssembly().FullName == args.Name)
            {
                return Assembly.GetExecutingAssembly();
            }

            strArray[0] = Path.GetFullPath(strArray[0]);
            string resolvedPath;

            if (FindAssembly(Path.GetFileName(strArray[0]), out resolvedPath))
            {
                /*
                if (strArray[0] == "LeagueSharp")
                {
                    Logs.Info("Loading LeagueSharp.dll");
                    return Assembly.LoadFile(resolvedPath);
                }*/

                return Assembly.Load(File.ReadAllBytes(resolvedPath));
            }

            return null;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private static void DomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Logs.InfoFormat("[PID:{0}] Sandbox.UnhandledException: {1}", Sandbox.Pid, args.ExceptionObject);
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        public bool Load(string path, string[] args)
        {
            if (LoadAssembly(path, args))
            {
                return true;
            }

            return false;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private static bool LoadAssembly(string path, IEnumerable args)
        {
            try
            {
                if (File.Exists(path))
                {
                    Logs.InfoFormat("[PID:{0}] Sandbox.LoadAssembly: {1}", Sandbox.Pid, path);
                    var assembly = Assembly.Load(File.ReadAllBytes(path));
                    if (assembly != null)
                    {
                        if (assembly.EntryPoint != null)
                        {
                            assembly.EntryPoint.Invoke(null, new object[] {args});
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logs.ErrorFormat("[PID:{0}] Sandbox.LoadAssembly: {1}", Sandbox.Pid, e.ToString());
            }

            return false;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        public static bool FindAssembly(string name, out string resolvedPath)
        {
            resolvedPath = "";
            var extension = Path.GetExtension(name);
            var flag = extension != null &&
                       (Path.HasExtension(name) && (extension.ToLower() == ".dll" || extension.ToLower() == ".exe"));

            try
            {
                var path1 = Path.Combine(Sandbox.Config.DataDirectory, "Assemblies");
                foreach (var path2 in
                    Directory.EnumerateFiles(path1)
                        .Where(
                            path2 =>
                                string.Compare(
                                    flag ? Path.GetFileName(path2) : Path.GetFileNameWithoutExtension(path2), name,
                                    StringComparison.OrdinalIgnoreCase) == 0))
                {
                    resolvedPath = path2;
                    return true;
                }

                foreach (var path2 in
                    Directory.EnumerateFiles(Assembly.GetExecutingAssembly().Location)
                        .Where(
                            path2 =>
                                string.Compare(
                                    flag ? Path.GetFileName(path2) : Path.GetFileNameWithoutExtension(path2), name,
                                    StringComparison.OrdinalIgnoreCase) == 0))
                {
                    resolvedPath = path2;
                    return true;
                }

                foreach (var path3 in
                    Directory.EnumerateDirectories(path1)
                        .SelectMany(
                            path2 =>
                                Directory.EnumerateFiles(path2)
                                    .Where(
                                        path3 =>
                                            string.Compare(
                                                flag ? Path.GetFileName(path3) : Path.GetFileNameWithoutExtension(path3),
                                                name, StringComparison.OrdinalIgnoreCase) == 0)))
                {
                    resolvedPath = path3;
                    return true;
                }
            }
            catch (Exception e)
            {
                Logs.ErrorFormat("[PID:{0}] Sandbox.FindAssembly: {1}", Sandbox.Pid, e.ToString());
                return false;
            }
            return false;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        public override object InitializeLifetimeService()
        {
            return null;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        public static void UnloadProxy(Domain proxy)
        {
            AppDomain.Unload(proxy.AppDomain);
        }
    }
}
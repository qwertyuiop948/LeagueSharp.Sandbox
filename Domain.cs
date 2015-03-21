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

                var info = new AppDomainSetup
                {
                    ApplicationName = domainName,
                    ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\",
                    ShadowCopyFiles = "true",
                    CachePath = Sandbox.Config.DataDirectory + "\\SandboxCache\\"
                };

                Logs.InfoFormat(
                    "[PID:{0}] Sandbox.CreateProxy:\nApplicationName:{1}\nApplicationBase:{2}\nCachePath:{3}",
                    Sandbox.Pid, info.ApplicationName, info.ApplicationBase, info.CachePath);

                var grantSet = Sandbox.Config.Permissions ?? new PermissionSet(PermissionState.None);

                if (Sandbox.Config.Permissions == null)
                {
                    grantSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                    grantSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, Sandbox.Config.DataDirectory));
                    grantSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, Assembly.GetExecutingAssembly().Location));
                    grantSet.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
                }

                var fullTrustAssembly = typeof (Domain).Assembly.Evidence.GetHostEvidence<StrongName>();
                var domain = AppDomain.CreateDomain(domainName, null, info, grantSet, fullTrustAssembly);
                    // TODO: stops working here!

                Logs.InfoFormat("[PID:{0}] Sandbox.CreateProxy: {1}", Sandbox.Pid, fullTrustAssembly.ToString());

                domain.AssemblyResolve += DomainOnAssemblyResolve;
                domain.UnhandledException += DomainOnUnhandledException;

                var handle = Activator.CreateInstanceFrom(domain,
                    typeof (Domain).Assembly.ManifestModule.FullyQualifiedName, typeof (Domain).FullName);
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
            var strArray = args.Name.Split(',');
            if (Assembly.GetExecutingAssembly().FullName == args.Name)
            {
                return Assembly.GetExecutingAssembly();
            }

            strArray[0] = Path.GetFullPath(strArray[0]);
            string resolvedPath;

            return FindAssembly(Path.GetFileName(strArray[0]), out resolvedPath)
                ? Assembly.Load(File.ReadAllBytes(resolvedPath))
                : null;
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
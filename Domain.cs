using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using LeagueSharp.Loader.Service;

namespace LeagueSharp.Sandbox
{
    public class Domain : MarshalByRefObject
    {
        public AppDomain AppDomain { get; private set; }

        public static Domain CreateProxy(string domainName)
        {
            var apiConfig = ServiceFactory.GetInterface<Configuration>();

            if (string.IsNullOrEmpty(domainName))
            {
                domainName = "Sandbox" + Guid.NewGuid().ToString("N") + "Domain";
            }

            var info = new AppDomainSetup
            {
                ApplicationName = domainName,
                ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\",
                ShadowCopyFiles = "true",
                CachePath = apiConfig.DataDirectory + "\\SandboxCache\\"
            };
            var grantSet = apiConfig.Permissions ?? new PermissionSet(PermissionState.None);
            if (apiConfig.Permissions == null)
            {
                grantSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                grantSet.AddPermission(
                    new FileIOPermission(
                        FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, apiConfig.DataDirectory));
            }
            var fullTrustAssembly = typeof(Domain).Assembly.Evidence.GetHostEvidence<StrongName>();

            var domain = AppDomain.CreateDomain(domainName, null, info, grantSet, fullTrustAssembly);
            domain.AssemblyResolve += (sender, args) =>
            {
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
            };
            domain.UnhandledException += (sender, args) =>
            {
                ServiceFactory.GetInterface<ILoaderLogService>()
                    .ErrorFormat("[PID:%d] Encountered an unhandled exception.\n%s", args.ExceptionObject.ToString());
                Console.WriteLine(args.ExceptionObject.ToString());
                Console.WriteLine("Press Enter to continue");
                Console.ReadLine();
                Environment.Exit(1);
            };

            var handle = Activator.CreateInstanceFrom(
                domain, typeof(Domain).Assembly.ManifestModule.FullyQualifiedName, typeof(Domain).FullName);
            var wrappedDomain = handle.Unwrap() as Domain;

            if (wrappedDomain != null)
            {
                wrappedDomain.AppDomain = domain;
                return wrappedDomain;
            }
            return null;
        }

        public bool Load(string @string, string[] args)
        {
            if (LoadAssembly(@string, args))
            {
                return true;
            }
            ServiceFactory.GetInterface<ILoaderLogService>()
                .ErrorFormat("[PID:%d] Failed to load assembly %s", Process.GetCurrentProcess().Id, @string);
            return false;
        }

        private static bool LoadAssembly(string @string, IEnumerable args)
        {
            try
            {
                if (File.Exists(@string))
                {
                    var assembly = Assembly.Load(File.ReadAllBytes(@string));
                    if (assembly != null)
                    {
                        if (assembly.EntryPoint != null)
                        {
                            try
                            {
                                assembly.EntryPoint.Invoke(null, new object[] { args });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ServiceFactory.GetInterface<ILoaderLogService>()
                    .ErrorFormat(
                        "[PID:%d] Failed to load assembly %s.\n%s", Process.GetCurrentProcess().Id, @string,
                        e.ToString());
            }

            return false;
        }

        public static bool FindAssembly(string name, out string resolvedPath)
        {
            resolvedPath = "";
            var extension = Path.GetExtension(name);
            var flag = extension != null &&
                       (Path.HasExtension(name) && (extension.ToLower() == ".dll" || extension.ToLower() == ".exe"));
            try
            {
                var path1 = Path.Combine(ServiceFactory.GetInterface<Configuration>().DataDirectory, "Assemblies");
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
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public static void UnloadProxy(Domain proxy)
        {
            AppDomain.Unload(proxy.AppDomain);
        }
    }
}
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using LeagueSharp.Loader.Service;

namespace LeagueSharp.Sandbox
{
    public class Domain : MarshalByRefObject
    {
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
                grantSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, apiConfig.DataDirectory));
            }
            var fullTrustAssembly = typeof(Domain).Assembly.Evidence.GetHostEvidence<StrongName>();

            var domain = AppDomain.CreateDomain(domainName, null, info, grantSet, fullTrustAssembly);
            var handle = Activator.CreateInstanceFrom(
                domain, typeof(Domain).Assembly.ManifestModule.FullyQualifiedName, typeof(Domain).FullName);

            return (Domain) handle.Unwrap();
        }

        public bool Load(string @string, string[] args)
        {
            if (LoadAssembly(@string, args))
            {
                return true;
            }
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

            }

            return false;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
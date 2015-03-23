using System;
using LeagueSharp.Sandbox.Shared;

namespace LeagueSharp.Sandbox
{
    public static class Logs
    {
        public static void InfoFormat(string s, params object[] args)
        {
            try
            {
                Console.WriteLine(s, args);
                //ServiceFactory.CreateProxy<ILoaderLogService>().InfoFormat(s, args);
            }
            catch
            {
                // ignored
            }
        }

        public static void Info(string s)
        {
            try
            {
                Console.WriteLine(s);
                //ServiceFactory.CreateProxy<ILoaderLogService>().Info(s);
            }
            catch
            {
                // ignored
            }
        }

        public static void ErrorFormat(string s, params object[] args)
        {
            try
            {
                Console.WriteLine(s, args);
                //ServiceFactory.CreateProxy<ILoaderLogService>().ErrorFormat(s, args);
            }
            catch
            {
                // ignored
            }
        }

        public static void Error(string s)
        {
            try
            {
                Console.WriteLine(s);
                //ServiceFactory.CreateProxy<ILoaderLogService>().Error(s);
            }
            catch
            {
                // ignored
            }
        }
    }
}
using System;
using LeagueSharp.Loader.Service;

namespace LeagueSharp.Sandbox
{
    public static class Logs
    {
        public static void InfoFormat(string s, params object[] args)
        {
            try
            {
                ServiceFactory.CreateProxy<ILoaderLogService>().InfoFormat(s, args);
                Console.WriteLine(s, args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void Info(string s)
        {
            try
            {
                ServiceFactory.CreateProxy<ILoaderLogService>().Info(s);
                Console.WriteLine(s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void ErrorFormat(string s, params object[] args)
        {
            try
            {
                ServiceFactory.CreateProxy<ILoaderLogService>().ErrorFormat(s, args);
                Console.WriteLine(s, args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void Error(string s)
        {
            try
            {
                ServiceFactory.CreateProxy<ILoaderLogService>().Error(s);
                Console.WriteLine(s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
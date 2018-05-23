using System;
using System.IO;
using System.Reflection;

namespace Kesco.Lib.Log
{
    /// <summary>
    /// Фабрика локального логгера.
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Получить экземпляр логгера
        /// </summary>
        public static ILocalLogger GetLocalLogger(string appName = "Application", string userName = null)
        {
            // Локальный логгер распространяется вне релиза.
            // Доступен отдельным пользователям.
            // Поэтому экземпляр логгера создается рефлексией.
            var path = AppDomain.CurrentDomain.BaseDirectory + "LocalLogger.dll";

            if (!File.Exists(path))
                path = AppDomain.CurrentDomain.BaseDirectory + @"bin\LocalLogger.dll";

            if (File.Exists(path))
            {
                try
                {
                    var assembly = Assembly.LoadFile(path);
                    Type classType = assembly.GetType("Kesco.Lib.LocalLogger.Logger");

                    var logger = (ILocalLogger)Activator.CreateInstance(classType, appName, userName);

                    return logger;
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                }
            }

            return null;
        }
    }
}

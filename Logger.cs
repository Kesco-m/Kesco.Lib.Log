using System;

namespace Kesco.Lib.Log
{
    /// <summary>
    /// Статический класс, предоставляющий доступ к единственному объекту LogModule
    /// </summary>
    public class Logger
    {
        static Logger()
        {
            _localLogger = new MockLocalLogger();
        }

        private static readonly object Sync = new object();
        private static volatile LogModule _log;

        /// <summary>
        /// Экземпляр локального логгера
        /// </summary>
        private static ILocalLogger _localLogger;

        /// <summary>
        /// Проверка того, что модуль регистрации инициализирован (и инициализирован правильно)
        /// </summary>
        public static bool IsConfigured
        {
            get { return _log != null && _log.IsConfigured; }
        }

        /// <summary>
        /// Фиксация информации об исключении
        /// </summary>
        /// <param name="ex">Исключение, которое требуется зафиксировать</param>
        public static void WriteEx(Exception ex)
        {
            _log.WriteEx(ex, true);

            // Расширенное логирование(локально)
            _localLogger.Exception(null, ex);
        }

        /// <summary>
        /// Фиксация информации об исключении
        /// </summary>
        /// <param name="ex">Исключение, которое требуется зафиксировать</param>
        /// <param name="async">Выполинть запись асинхронно</param>
        public static void WriteEx(Exception ex, bool async)
        {
            _log.WriteEx(ex, async);

            // Расширенное логирование(локально)
            _localLogger.Exception(null, ex);
        }

        /// <summary>
        /// Инициализация статического модуля обработки ошибок приложения
        /// </summary>
        /// <param name="logModule">уже проинициализированный объект</param>
        public static void Init(LogModule logModule)
        {
            Init(logModule, null, "Application");
        }

        /// <summary>
        /// Инициализация статического модуля обработки ошибок приложения
        /// </summary>
        /// <param name="logModule">уже проинициализированный объект</param>
        public static void InitVB(LogModule logModule)
        {
            Init(logModule, null, "Application");
        }
        /// <summary>
        /// Инициализация статического модуля обработки ошибок приложения
        /// </summary>
        /// <param name="logModule">уже проинициализированный объект</param>
        /// <param name="appName"></param>
        /// <param name="switchOnLocalLogger">Включить локальное логирование</param>
        public static void Init(LogModule logModule, string appName = "Application", bool switchOnLocalLogger = false)
        {
            Init(logModule, null, appName, switchOnLocalLogger);
        }

        /// <summary>
        /// Инициализация статического модуля обработки ошибок приложения
        /// </summary>
        /// <param name="logModule">уже проинициализированный объект</param>
        /// <param name="userName">Для Web указать "All"</param>
        /// <param name="appName">Наименование приложения. Указать римя приложения или "Application"</param>
        /// <param name="switchOnLocalLogger">Включить локальное логирование</param>
        public static void Init(LogModule logModule, string userName = "All", string appName = "Application", bool switchOnLocalLogger = false)
        {
            lock (Sync)
            {
                LogModule oldLog = _log;
                _log = logModule;
                if (oldLog != null && oldLog is IDisposable)
                    (oldLog as IDisposable).Dispose();

                if (switchOnLocalLogger)
                {
                    var localLogger = LoggerFactory.GetLocalLogger(appName, userName);

                    if (localLogger != null)
                        _localLogger = localLogger;
                }
            }
        }

        /// <summary>
        ///  Расширенное логирование. Локальный лог. Записать в лог сообщение.
        /// </summary>
        /// <param name="message"></param>
        public static void Message(string message)
        {
            // Расширенное логирование(локально)
            _localLogger.LogMessage(message);
        }

        /// <summary>
        ///  Расширенное логирование. Локальный лог. Записать в лог ошибку.
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            // Расширенное логирование(локально)
            _localLogger.Error(message);
        }

        /// <summary>
        ///  Расширенное логирование. Локальный лог. Записать в лог исключение.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="e"></param>
        public static void Exception(string message, Exception e)
        {
            // Расширенное логирование(локально)
            _localLogger.Exception(message, e);
        }

        /// <summary>
        /// Получить экземпляр класса для логирования времени выполнения
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IDurationMetter DurationMetter(string message)
        {
            return _localLogger.GetDurationMetter(message);
        }

        /// <summary>
        /// Вход в метод.
        /// Должнен работать в паре с LeaveMethod в блоке try catch finaly
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        public static void EnterMethod(object instance, string methodName)
        {
            _localLogger.EnterMethod(instance, methodName);
        }

        /// <summary>
        /// Выход из метода.
        /// Должнен работать в паре с EnterMethod в блоке try catch finaly
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        public static void LeaveMethod(object instance, string methodName)
        {
            _localLogger.LeaveMethod(instance, methodName);
        }
    }
}
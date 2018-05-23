using System;

namespace Kesco.Lib.Log
{
    /// <summary>
    /// Методы для работы с локальным логом
    /// </summary>
    public interface ILocalLogger
    {
        /// <summary>
        /// Создать запись.
        /// </summary>
        /// <param name="message"></param>
        void LogMessage(string message);

        /// <summary>
        /// Создать запись  с уровнем предупреждение.
        /// </summary>
        /// <param name="message"></param>
        void LogWarning(string message);

        /// <summary>
        /// Записать в лог ошибку
        /// </summary>
        /// <param name="message"></param>
        void Error(string message);

        /// <summary>
        /// Записать в лог Exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        void Exception(string message, Exception ex);

        /// <summary>
        /// Вход в метод.
        /// Должнен работать в паре с LeaveMethod в блоке try catch finaly
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        void EnterMethod(object instance, string methodName);

        /// <summary>
        /// Выход из метода.
        /// Должнен работать в паре с EnterMethod в блоке try catch finaly
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        void LeaveMethod(object instance, string methodName);

        /// <summary>
        /// Трассировка.
        /// </summary>
        void StackTrace();

        /// <summary>
        /// Логирование состояние объекта. Значения свойств объекта.
        /// </summary>
        /// <param name="instance"></param>
        void ObjectProperties(object instance);

        /// <summary>
        /// Получить IDisposable экземпляр класса для точного измерения времени выполнения операции или метода
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        IDurationMetter GetDurationMetter(string message);
    }
}

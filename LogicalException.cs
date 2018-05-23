using System;
using System.Reflection;


namespace Kesco.Lib.Log
{
	/// <summary>
	/// Переопределение базового класса Exception. Используется в приложениях холдинга для формирования цепочек исключений, содержащих дополнительную отладочную информацию.
	/// </summary>
	public class LogicalException : Exception
	{
		private string LOGICAL = "LogicalException";

		private Priority priorityLevel;
		private string details;
		private string build;
		private string version;
		private string method;

		/// <summary>
		/// Приоритет исключительной ситуации
		/// </summary>
		internal Priority PriorityLevel { get { return priorityLevel; } }

		/// <summary>
		/// Какие-то детали, которые программист считает нужным добавить в отладочную информацию
		/// </summary>
		internal string Details { get { return details; } }

		/// <summary>
		/// Сборка
		/// </summary>
		internal string Build { get { return build; } }

		/// <summary>
		/// Версия сборки
		/// </summary>
		internal string Version { get { return version; } }

		/// <summary>
		/// Метод, к которому относится ошибка
		/// </summary>
		internal string Method { get { return method; } }


		/// <summary>
		/// Базовый конструктор LogicalException. В качестве метода по-умолчанию будет присвоено значение "LogicalException".
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="details">дополнительная отладочная информация</param>
		/// <param name="assembly">информация о сборке (System.Reflection.Assembly.GetExecutingAssembly().GetName)</param>
		public LogicalException( string message, string details, AssemblyName assembly ) : base( message )
		{
			priorityLevel = Priority.Error;
			this.details = details;
			this.build = assembly.Name;
			this.version = assembly.Version.ToString();
			this.method = LOGICAL;
		}


		/// <summary>
		/// конструктор LogicalException
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="details">дополнительная отладочная информация</param>
		/// <param name="assembly">информация о сборке (System.Reflection.Assembly.GetExecutingAssembly().GetName)</param>
		/// <param name="method">имя метода (System.Reflection.MethodBase.GetCurrentMethod().Name)</param>
		public LogicalException( string message, string details, AssemblyName assembly, string method ) : this( message, details, assembly )
		{
			this.method = method;
		}


		/// <summary>
		/// конструктор LogicalException
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="details">дополнительная отладочная информация</param>
		/// <param name="assembly">информация о сборке (System.Reflection.Assembly.GetExecutingAssembly().GetName)</param>
		/// <param name="priority">приоритет исключения</param>
		public LogicalException( string message, string details, AssemblyName assembly, Priority priority ) : this( message, details, assembly )
		{
			priorityLevel = priority;
		}


		/// <summary>
		/// конструктор LogicalException
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="details">дополнительная отладочная информация</param>
		/// <param name="assembly">информация о сборке (System.Reflection.Assembly.GetExecutingAssembly().GetName)</param>
		/// <param name="method">имя метода (System.Reflection.MethodBase.GetCurrentMethod().Name)</param>
		/// <param name="priority">приоритет исключения</param>
		public LogicalException( string message, string details, AssemblyName assembly, string method, Priority priority ) : this( message, details, assembly )
		{
			this.method = method;
			priorityLevel = priority;
		}

	}
}

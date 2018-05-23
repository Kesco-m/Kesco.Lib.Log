using System.Diagnostics;
using System.Threading;


namespace Kesco.Lib.Log
{
	/// <summary>
	/// Асинхронная регистрация сообщения в EventLog
	/// </summary>
	internal class TaskEventLogReg
	{
		/// <summary>
		/// Префикс источника в Event log
		/// </summary>
		private const string EventSource = "Kesco: ";

		/// <summary>
		/// Журнал регистрации
		/// </summary>
		private const string EventLogName = "Application";

		private Priority _p;
		private string _appName;
		private string _msgText;


		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="p">Приоритет сообщения</param>
		/// <param name="appName">Навание приложения</param>
		/// <param name="msgText">Тест сообщения</param>
		public TaskEventLogReg( Priority p, string appName, string msgText )
		{
			_p = p;
			_appName = appName;
			_msgText = msgText;
		}


		/// <summary>
		/// Вызов функции регистрации
		/// </summary>
		/// <param name="obj">заглушла для callback-функции</param>
		private void EventLogReg( object obj )
		{
			// Тип сообщения в EventLog
			EventLogEntryType type = EventLogEntryType.Information;

			switch( _p )
			{
				case Priority.Error:
				case Priority.Alarm:
					type = EventLogEntryType.Error;
					break;
			}

			// Регистрация сообщения в EventLog
			try
			{
				if( !System.Diagnostics.EventLog.SourceExists( EventSource + _appName ) )
					System.Diagnostics.EventLog.CreateEventSource( EventSource + _appName, EventLogName );

				System.Diagnostics.EventLog.WriteEntry( EventSource + _appName, _msgText, type );
			}
			catch{}
		}


		/// <summary>
		/// Организация асинхронного вызова
		/// </summary>
		public void Execute()
		{
			ThreadPool.QueueUserWorkItem( new WaitCallback( EventLogReg ) );
		}

	}
}
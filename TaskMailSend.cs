using System.Threading;
using System.Xml;


namespace Kesco.Lib.Log
{
	/// <summary>
	/// Асинхронная отправка сообщения через SMTP
	/// </summary>
	internal class TaskMailSend
	{
		private LogModule _log;
		/// <summary>
		/// Тема письма (для асинхронной отправки)
		/// </summary>
		private string _subject;
		/// <summary>
		/// Данные об ошибке (для асинхронной отправки)
		/// </summary>
		private XmlElement _xml;


		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="log">Текущий log-модуль</param>
		/// <param name="subject">Тема письма</param>
		/// <param name="xml">XML с информацией по ошибке</param>
		public TaskMailSend( LogModule log, string subject, XmlElement xml )
		{
			_log = log;
			_subject = subject;
			_xml = xml;
		}


		/// <summary>
		/// Вызов функции отправки
		/// </summary>
		/// <param name="obj">заглушла для callback-функции</param>
		private void MailSend( object obj )
		{
			_log.SendMail( _subject, _xml );
		}


		/// <summary>
		/// Организация асинхронного вызова
		/// </summary>
		public void Execute()
		{
			ThreadPool.QueueUserWorkItem( new WaitCallback( MailSend ) );
		}

	}
}
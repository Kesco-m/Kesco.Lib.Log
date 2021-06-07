using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;


namespace Kesco.Lib.Log
{
	/// <summary>
	/// Переопределение базового класса Exception. Используется в приложениях холдинга для формирования цепочек исключений, содержащих дополнительную отладочную информацию.
	/// </summary>
	[Serializable]
	public class DetailedException : Exception, ISerializable
	{
		private Priority priorityLevel;
		private string details;
		private bool sendMail;
	    private string customMessage;

		private IDbCommand sqlCmd;

        /// <summary>
        /// Аксессор к сообщения об ошибке, которое написал программист
        /// </summary>
	    public string CustomMessage {
	        get { return customMessage; }
	    }

	    /// <summary>
		/// Приоритет исключительной ситуации
		/// </summary>
		internal Priority PriorityLevel { get { return priorityLevel; } }

		/// <summary>
		/// Какие-то детали, которые программист считает нужным добавить в отладочную информацию
		/// </summary>
		internal string Details { get { return details; } }

		/// <summary>
		/// Признак того, что по данному исключению должна быть собрана информация и отправлена в службу поддержки.
		/// В сообщение об ошибке попадет вся цепочка Exception-ов, если хотя бы для одного из них флаг примет значение true.
		/// </summary>
		internal bool SendMail { get { return sendMail; } }

		/// <summary>
		/// Необязательное поле - указывается в случае, если исключение произошло на стороне SQL-сервера и блоку try-catch доступна SQL команда
		/// </summary>
		internal IDbCommand SqlCmd { get { return sqlCmd; } }


		/// <summary>
		/// Получение строки с расширенными деталями цепочки исключений (доп. информация, SQL данные)
		/// </summary>
		/// <returns>дополнительная отладочная информация</returns>
		public string GetExtendedDetails()
		{
			string res = "";

			Exception ex = this;
			while( ex != null )
			{
				res += ex.GetType().FullName + ": " + ex.Message + "\n";
				if( ex.StackTrace != null )
					res += "\tStack Trace:\n" + ex.StackTrace + "\n";

				if( ex is DetailedException )
				{
					if( (ex as DetailedException).Details.Length > 0 )
						res += "\tDetails:\n" + LogModule.ClearPassword( (ex as DetailedException).Details ) + "\n";

					if( (ex as DetailedException).SqlCmd != null )
					{
						res += "\tSql Command:\n" + (ex as DetailedException).SqlCmd.CommandText + "\n";
						res += "\tSql Connection:\n" + LogModule.ClearPassword( (ex as DetailedException).SqlCmd.Connection.ConnectionString ) + "\n";
                        res += "\tSql Timeout:\n" + (ex as DetailedException).SqlCmd.CommandTimeout + " сек.\n";

                        string sParams = "";
						foreach( SqlParameter p in (ex as DetailedException).SqlCmd.Parameters )
							sParams += String.Format( "{0}='{1}';\n", p.ParameterName, p.Value );

						if( sParams.Trim().Length > 0 )
							res += "\tSql Params:\n" + sParams + "\n";
					}
				}

				res += "\n";
				ex = ex.InnerException;
			}

			return res;
		}


		/// <summary>
		/// Дефолтный конструктор DetailedException
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">"пойманный" глубже Exception или null</param>
		public DetailedException( string message, Exception innerException ) : base( message, innerException )
		{
		    customMessage = message;

			if( innerException is DetailedException )
			{
				priorityLevel = ( innerException as DetailedException ).PriorityLevel;
				sendMail = ( innerException as DetailedException ).SendMail;
			}
			else
			{
				priorityLevel = Priority.Error;
				sendMail = true;
			}

			details = "";
		}


		/// <summary>
		/// Конструктор с возможностью изменять приоритет исключительной ситуации
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">"пойманный" глубже Exception или null</param>
		/// <param name="priority">приоритет исключения</param>
		public DetailedException( string message, Exception innerException, Priority priority ) : this( message, innerException )
		{
		    customMessage = message;
			this.priorityLevel = priority;
		}


		/// <summary>
		/// Конструктор, предполагающий обязательную отправку письма в службу поддержки.
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">"пойманный" глубже Exception или null</param>
		/// <param name="details">подробное описание</param>
		public DetailedException( string message, Exception innerException, string details ) : this( message, innerException )
		{
		    customMessage = message;
			this.details = details;
		}


		/// <summary>
		/// Констуктор с указанием необходимости отправки письма в службу поддержки без доп. деталей.
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">объект типа Exception</param>
		/// <param name="sendMail">Флаг-нужно ли отправлять данную ошибку в Службу поддержки</param>
		public DetailedException( string message, Exception innerException, bool sendMail ) : this( message, innerException )
		{
		    customMessage = message;
			this.sendMail = sendMail;
		}


		/// <summary>
		/// Констуктор с указанием необходимости отправки письма в службу поддержки с дополнительными отладочными данными.
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">объект типа Exception</param>
		/// <param name="details">дополнительная отладочная информация</param>
		/// <param name="sendMail">Флаг-нужно ли отправлять данную ошибку в Службу поддержки</param>
		public DetailedException( string message, Exception innerException, string details, bool sendMail ) : this( message, innerException )
		{
		    customMessage = message;
			this.details = details;
			this.sendMail = sendMail;
		}


		/// <summary>
		/// Констуктор с указанием необходимости отправки письма в службу поддержки с дополнительными отладочными данными.
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">объект типа Exception</param>
		/// <param name="priority">приоритет исключения</param>
		/// <param name="details">дополнительная отладочная информация</param>
		public DetailedException( string message, Exception innerException, Priority priority, string details ) : this( message, innerException )
		{
		    customMessage = message;
			this.priorityLevel = priority;
			this.details = details;
		}


		/// <summary>
		/// Констуктор с указанием необходимости отправки письма в службу поддержки с дополнительными отладочными данными.
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">объект типа Exception</param>
		/// <param name="priority">приоритет исключения</param>
		/// <param name="details">дополнительная отладочная информация</param>
		/// <param name="sendMail">Флаг-нужно ли отправлять данную ошибку в Службу поддержки</param>
		public DetailedException( string message, Exception innerException, Priority priority, string details, bool sendMail ) : this( message, innerException )
		{
		    customMessage = message;
			this.priorityLevel = priority;
			this.details = details;
			this.sendMail = sendMail;
		}


		/// <summary>
		/// Констуктор на основании SQL исключения. Отправка письма определяется классом SQL исключения (12 не отправляется)
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">объект типа Exception</param>
		/// <param name="sqlCmd">команда к БД - для получения строки подключения к БД и собственно текста sql-комманды</param>
		public DetailedException( string message, Exception innerException, IDbCommand sqlCmd ) : this( message, innerException )
		{
		    customMessage = message;
			this.sqlCmd = sqlCmd;

			// Отправка сообщений для ошибок на SQL Server с кодом 12 не предусмотрена - только вывод информационных сообщений пользователю
			if( innerException is SqlException && ((SqlException)innerException).Class.Equals((byte)12) )
				sendMail = false;
			else if( innerException is DetailedException )
				sendMail = ( innerException as DetailedException ).SendMail;
			else
				sendMail = true;
		}


		/// <summary>
		/// Констуктор на основании SQL исключения. Отправка письма определяется классом SQL исключения (12 не отправляется)
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">объект типа Exception</param>
		/// <param name="sqlCmd">команда к БД - для получения строки подключения к БД и собственно текста sql-комманды</param>
		/// <param name="priority">приоритет исключения</param>
		public DetailedException( string message, Exception innerException, IDbCommand sqlCmd, Priority priority ) : this( message, innerException, sqlCmd )
		{
		    customMessage = message;
			this.priorityLevel = priority;
		}


		/// <summary>
		/// Констуктор на основании SQL исключения с доп. деталями. Отправка письма определяется классом SQL исключения (12 не отправляется)
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">объект типа Exception</param>
		/// <param name="sqlCmd">команда к БД - для получения строки подключения к БД и собственно текста sql-комманды</param>
		/// <param name="details">дополнительная отладочная информация</param>
		public DetailedException( string message, Exception innerException, IDbCommand sqlCmd, string details ) : this( message, innerException, sqlCmd )
		{
		    customMessage = message;
			this.details = details;
		}


		/// <summary>
		/// Констуктор на основании SQL исключения с доп. деталями. Отправка письма определяется классом SQL исключения (12 не отправляется)
		/// </summary>
		/// <param name="message">краткое описание</param>
		/// <param name="innerException">объект типа Exception</param>
		/// <param name="sqlCmd">команда к БД - для получения строки подключения к БД и собственно текста sql-комманды</param>
		/// <param name="priority">приоритет исключения</param>
		/// <param name="details">дополнительная отладочная информация</param>
		public DetailedException( string message, Exception innerException, IDbCommand sqlCmd, Priority priority, string details ) : this( message, innerException, sqlCmd, details )
		{
		    customMessage = message;
			this.priorityLevel = priority;
		}



		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="info">Информация необходимация для сериализации</param>
		/// <param name="context">Содержимое для сериализации</param>
		protected DetailedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
            customMessage = info.GetString("message");
			details = info.GetString("details");
		}


		/// <summary>
		/// Сериализация содержимого ошибки
		/// </summary>
		/// <param name="info">Информация необходимация для сериализации</param>
		/// <param name="context">Содержимое для сериализации</param>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("details", details);
		}

	}
}

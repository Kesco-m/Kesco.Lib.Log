using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Collections.Generic;
using System.Linq;


namespace Kesco.Lib.Log
{
	/// <summary>
	/// Делегат для события реинициализации log-модуля
	/// </summary>
	public delegate void DisposeEventHandler(LogModule sender);

	/// <summary>
	/// Модуль регистрации исключительных ситуаций
	/// </summary>
	public class LogModule
	{
        private readonly int SKIP_MESS_MINUTE = 20;
		/// <summary>
		/// Событие вызова реинициализации log-модуля
		/// </summary>
		public event DisposeEventHandler OnDispose;

		private object lockSmtp = new object();
		private object lockObj = new object();

		private string _smtpServer = "";
		private string _supportEmail = "";
		private string _appName = "";

		private const int MaxSubjLen = 254;

		private const string WarningBuild = "Не удалось определить сборку авторства холдинга, в функции которой произошла ошибка!";
		private const string WarningSQL = "Требуется обрабатывать SQL исключение так, чтобы в обработчик ошибок передавался наследник IDbCommand!";

        private SynchronizedCollection<SentMessage> _sentMessages = new SynchronizedCollection<SentMessage>();

        /// <summary>
        /// Имя домена, в контесте которого выполняется сборка
        /// </summary>
        private string _domain_name = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
        
		/// <summary>
		/// Названия сборок, разрабатываемых в компаниях Атэк-групп. Используется для опрекделения места ошибки на достаточно глубоком уровне.
		/// </summary>
		private static StringCollection kescoBuilds = new StringCollection();

		private volatile int state = 0;

		/// <summary>
		/// Состояние LogModule:
		/// 0 – готов к отправке;
		/// >0 – сообщения отправляются;
		/// </summary>
		public int State
		{
			get { return state; }
		}


		/// <summary>
		/// Проверка правильности инициализации модуля регистрации
		/// </summary>
		public bool IsConfigured
		{
			get
			{
				return (_smtpServer != null && _smtpServer != ""
					&& _supportEmail != null && _supportEmail != ""
					&& _appName != null && _appName != "");
			}
		}


		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="appName">Название приложения</param>
		public LogModule( string appName )
		{
			_appName = System.Web.HttpUtility.HtmlDecode( appName );
		}

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="appName">Название приложения</param>
        /// <param name="skip_minutes">Период времени в минутах, в течении которого одинаковые сообщения не поступают в Службу Поддержки</param>
        public LogModule(string appName, int skip_minutes)
            : this(appName)
        {
            SKIP_MESS_MINUTE = (skip_minutes < 0 ? SKIP_MESS_MINUTE : skip_minutes);
        }

		/// <summary>
		/// Инициализация параметров для модуля регистрации исключительных ситуаций
		/// </summary>
		/// <param name="smtpServer">SMTP-сервер</param>
		/// <param name="supportEmail">Email-suppport</param>
		public void Init( string smtpServer, string supportEmail )
		{
			_smtpServer = smtpServer;
			_supportEmail = supportEmail;

			// Получение названий сборок, разработанных компаниями Атэк-групп
			Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
			for( int i = 0; i < asms.Length; i++ )
			{
				object[] attributes = asms[i].GetCustomAttributes( typeof(AssemblyIsKescoAttribute), false );
				if( attributes.Length > 0 && (attributes[0] as AssemblyIsKescoAttribute).IsKesco )
					kescoBuilds.Add( asms[i].GetName().Name );
			}
		}


		/// <summary>
		/// Фиксация информации об исключении
		/// </summary>
		/// <param name="ex">Исключение, которое требуется зафиксировать</param>
		/// <param name="async">Выполинть запись асинхронно</param>
		internal void WriteEx( Exception ex, bool async )
		{
            lock (_sentMessages.SyncRoot)
            {
                try
                {
                    SentMessage[] tmp = _sentMessages.Where(x => x.DtCreate <= DateTime.Now.AddMinutes(-1 * SKIP_MESS_MINUTE)).ToArray();
                    tmp.Select(x => _sentMessages.Remove(x)).ToArray();
                }
                catch { } 
            }

			if( ex == null ) return;

			XmlElement xml = GetErrorXml( ex );

			if( ((XmlElement)xml.SelectSingleNode( "Info" )).GetAttribute( "mail" ) == "1" )
			{
				// Заголовок - используется для автоматической разборки писем на сервере
				string subject = "";

				string pr = ((XmlElement)xml.SelectSingleNode( "Info/Node[@id='Priority']" )).GetAttribute( "value" );
				if (pr == Enum.GetName(typeof(Priority), Priority.Error))
				{
					subject = "AppError";

					// Доп. проверка на ExternalError
					pr = GetPriority(ex, Priority.Error).ToString();
					((XmlElement)xml.SelectSingleNode("Info/Node[@id='Priority']")).SetAttribute("value", pr);
				}
                if (pr == Enum.GetName(typeof(Priority), Priority.ExternalError))
                    subject = "Error ";
                else if (pr == Enum.GetName(typeof(Priority), Priority.Info) || pr == Enum.GetName(typeof(Priority), Priority.Alarm))
                    subject = "*" + pr + "* ";

                if (string.IsNullOrEmpty(_domain_name) && (pr == Enum.GetName(typeof(Priority), Priority.ExternalError) || pr == Enum.GetName(typeof(Priority), Priority.Alarm)))
                    return;

				XmlElement component = (XmlElement)xml.SelectSingleNode( "Info/Node[@id='Component']" );
				string build = component != null ? component.GetAttribute( "value" ) : "";

				subject += String.Format((pr == Enum.GetName(typeof(Priority), Priority.Error) ? "[{0}]{1}:{2}" : "{2}"), _appName,
					( build != "" ? "[" + build + "]" : "" ),
					((XmlElement)xml.SelectSingleNode("Info")).GetAttribute("message")).Replace("\n", " ").Replace("\r", " ");

				if( subject.Length > MaxSubjLen )
					subject = subject.Substring( 0, MaxSubjLen );

                XmlElement func = ((XmlElement)xml.SelectSingleNode("Info/Node[@id='Function']"));
                XmlElement host = ((XmlElement)xml.SelectSingleNode("Info/Node[@id='Computer']"));
                XmlElement user = ((XmlElement)xml.SelectSingleNode("Info/Node[@id='Computer']"));
                SentMessage sm = new SentMessage((host != null ? host.GetAttribute("value") : ""), (user != null ? user.GetAttribute("value") : ""), (func != null ? func.GetAttribute("value") : ""), subject);
                lock (_sentMessages.SyncRoot)
                {
                    try
                    {
                        if (_sentMessages.FirstOrDefault(x => x.Equals(sm)) != null)
                            return;
                        _sentMessages.Add(sm);
                    }
                    catch { }
                }

                // Очистка информации о внутренней структуре приложения(стэк/ информацию о сборках) для внешних ошибок. 
                if(pr == Enum.GetName(typeof(Priority), Priority.ExternalError))
                {
                    XmlElement excpt = ((XmlElement)xml.SelectSingleNode("Exceptions"));
                    if(excpt != null)
                    {
                        foreach(XmlElement exc in excpt.ChildNodes)
                        {
                            XmlElement cmpt = ((XmlElement)exc.SelectSingleNode("Node[@id='Component']"));
                            if (cmpt != null)
                                exc.RemoveChild(cmpt);
                            XmlElement mthd = ((XmlElement)exc.SelectSingleNode("Node[@id='Method']"));
                            if (mthd != null)
                                exc.RemoveChild(mthd);
                            XmlElement trce = ((XmlElement)exc.SelectSingleNode("Node[@id='Trace']"));
                            if (trce != null)
                                exc.RemoveChild(trce);
                        }
                        XmlElement info = ((XmlElement)xml.SelectSingleNode("Info"));
                        XmlElement frmw = ((XmlElement)xml.SelectSingleNode("Info/Node[@id='Framework']"));
                        if (frmw != null)
                            info.RemoveChild(frmw);
                        XmlElement memuse = ((XmlElement)xml.SelectSingleNode("Info/Node[@id='MemoryUsed']"));
                        if (memuse != null)
                            info.RemoveChild(memuse);
                    }
                }

				// Отправка письма
				IncState( 1 );

				if( async )
				{
					TaskMailSend task = new TaskMailSend( this, subject, xml );
					task.Execute();
				}
				else
					SendMail( subject, xml );
			}
		}


		/// <summary>
		/// Изменение состояния LogModule. Состояние выступает в роли счетчика и принимает только неотрицательные значения.
		/// </summary>
		/// <param name="delta">число, на которое изменяем состояние</param>
		private void IncState( int delta )
		{
			state += delta;
			if( state < 0 ) state = 0;
		}


		/// <summary>
		/// Формирование XML с описанием и цепочкой исключений, вызванных ошибкой
		/// </summary>
		/// <param name="ex">исключение, по которому надо получить отладочную информацию</param>
		/// <returns>XML с данными об ошибке</returns>
		private XmlElement GetErrorXml( Exception ex )
		{
			XmlDocument Document = new XmlDocument();
			XmlElement root = Document.CreateElement( "Error" );
			Document.AppendChild( root );
			XmlElement node;
			string type = "", message = "", priority = Enum.GetName(typeof (Priority), Priority.Info), build = "", version = "", method = "" ,SQLWarning = "";
			int hResult = 0;
			bool sendMail = false;

			#region Формирование информации об исключениях в цепочке исключений

			// Подготовка цепочки исключений в след. порядке: сперва самые "глубокие" исключения, далее - оборачивающие их
			ArrayList exList = new ArrayList();
			while( ex != null )
			{
				exList.Insert( 0, ex );
				ex = ex.InnerException;
			}

			XmlElement exceptions = Document.CreateElement( "Exceptions" );

			for( int exOrder = 0; exOrder < exList.Count; exOrder++ )
			{
				ex = (Exception) exList[exOrder];

				// Сообщение и тип оригинального исключения
				if( exOrder == 0 )
				{
					type = ex.GetType().FullName;
					message = ex.Message;
					hResult = Marshal.GetHRForException( ex );

					if( exList.Count == 1 )
					{
						if( !(ex is LogicalException || ex is DetailedException) )
							priority = Enum.GetName(typeof (Priority), Priority.Error);

						if (ex is SqlException)
						{
							sendMail = !((SqlException)ex).Class.Equals(12);
							SQLWarning = WarningSQL;
						}
						else if (ex is DetailedException)
						{
							sendMail = (ex as DetailedException).SendMail;
							if( (int)(ex as DetailedException).PriorityLevel < 1 )
								priority = Enum.GetName(typeof (Priority), Priority.Error);
						}
						else if (ex is LogicalException)
						{
							sendMail = true;
							if( (int)(ex as LogicalException).PriorityLevel < 1 )
								priority = Enum.GetName(typeof (Priority), Priority.Error);
						}
						else
							sendMail = true;
					}
					else
					{
						if( ex is SqlException )
							sendMail = !( (SqlException)ex ).Class.Equals(12);
						else if( ex is DetailedException )
							sendMail = ( ex as DetailedException ).SendMail;
						else if( ex is LogicalException )
							sendMail = true;
					}
				}
				else
				{
					// Если пришедшее исключение не было обернуто Detailed или LogicalException
					if( !(ex is LogicalException || ex is DetailedException) )
					{
						priority = Enum.GetName(typeof (Priority), Priority.Error);

						if( ex is SqlException )
							sendMail |= !( (SqlException)ex ).Class.Equals(12);
						else
							sendMail = true;
					}
				}

				#region Получение информации о месте возникновения исключительной ситуации

				if( ex is LogicalException )
				{
					LogicalException lex = ex as LogicalException;
					build = lex.Build;
					version = lex.Version;
					method = lex.Method;
				}
				else
				{
					StackTrace stack = new StackTrace( ex, true );
					for( int j = 0; j < stack.FrameCount; j++ )
					{
						StackFrame frame = stack.GetFrame( j );
						MethodBase m = frame.GetMethod();
						if( method != null )
						{
							Type declaringType = m.DeclaringType;
							if( declaringType != null )
							{
								string componentName = declaringType.Assembly.GetName( false ).Name;
								if( kescoBuilds.IndexOf( componentName ) == -1 )
								{
									//Повторная загрузка сборок авторства холдинга
									kescoBuilds.Clear();
									Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
									kescoBuilds.AddRange(asms.Where(a => a.GetCustomAttributes(typeof(AssemblyIsKescoAttribute), false).Length > 0 &&
													(a.GetCustomAttributes(typeof(AssemblyIsKescoAttribute), false)[0] as AssemblyIsKescoAttribute).IsKesco).Select(a => a.GetName().Name).ToArray());
								}
								if (kescoBuilds.IndexOf(componentName) != -1)
								{
									build = declaringType.Assembly.GetName( false ).Name;
									version = declaringType.Assembly.GetName().Version.ToString();
									method = m.Name;
									break;
								}
							}
						}
					}
				}

				#endregion

				XmlElement exception = Document.CreateElement( "Ex" );

				// Сообщение
				node = Document.CreateElement( "Node" );
				node.SetAttribute( "id", "Message" );
				node.SetAttribute( "name", "Сообщение" );
				node.SetAttribute( "value", XmlTextEncoder.Encode(ex.Message) );
				exception.AppendChild( node );

				// Тип исключения
				node = Document.CreateElement( "Node" );
				node.SetAttribute( "id", "Type" );
				node.SetAttribute( "name", "Тип исключения" );
				node.SetAttribute( "value", XmlTextEncoder.Encode(ex.GetType().FullName) );
				exception.AppendChild( node );

				if( ex is SoapHeaderException || ex is SoapException || ex is DetailedException || ex is LogicalException )
				{
					#region Приоритет ошибки

					Priority pr;
					if( ex is DetailedException )
					{
						pr = ( ex as DetailedException ).PriorityLevel;

						// Отправка письма осуществляется если хотя бы для одного письма в цепочке это предусмотрено
						sendMail |= ( ex as DetailedException ).SendMail;
					}
					else if( ex is LogicalException )
					{
						pr = ( ex as LogicalException ).PriorityLevel;
					}
					else pr = Priority.Error;

					node = Document.CreateElement( "Node" );
					node.SetAttribute( "id", "Priority" );
					node.SetAttribute( "name", "Приоритет" );
					node.SetAttribute( "value", Enum.GetName( typeof (Priority), pr ) );
					exception.AppendChild( node );

					// Приоритет всей цепочки сообщений определяется наивысшим уровнем (Alarm > ExternalError > Error > Info)
					if( priority != Enum.GetName(typeof (Priority), Priority.Alarm) )
					{
						if( pr == Priority.Alarm )
							priority = Enum.GetName(typeof (Priority), Priority.Alarm);
						else if( pr == Priority.ExternalError && priority != Enum.GetName(typeof (Priority), Priority.ExternalError) )
							priority = Enum.GetName(typeof (Priority), Priority.ExternalError);
						else if( pr == Priority.Error && priority != Enum.GetName(typeof (Priority), Priority.Error) )
							priority = Enum.GetName(typeof (Priority), Priority.Error);
					}

					#endregion

					#region Дополнительная отладочная информация

					string details;

					if( ex is SoapHeaderException )
						details = "XML Web service URL : " + ( ex as SoapHeaderException ).Actor;
					else if( ex is SoapException )
						details = "XML Web service URL : " + ( ex as SoapException ).Actor;
					else if( ex is Win32Exception )
						details = "ErrorCode (HRESULT) : " + ( ex as Win32Exception ).ErrorCode +
								"\nNativeErrorCode : " + ( ex as Win32Exception ).NativeErrorCode;
					else if( ex is DetailedException )
						details = ( ex as DetailedException ).Details;
					else
						details = ( ex as LogicalException ).Details;

					if( details != "" )
					{
						node = Document.CreateElement( "Node" );
						node.SetAttribute( "id", "Details" );
						node.SetAttribute( "code", "1" );
						node.SetAttribute( "name", "Details" );
						node.SetAttribute("value", XmlTextEncoder.Encode(ClearPassword(details)));
						exception.AppendChild( node );
					}

					#endregion
				}

				if( ex.TargetSite != null )
				{
					// Полная версия сборки
					node = Document.CreateElement( "Node" );
					node.SetAttribute( "id", "Component" );
					node.SetAttribute( "name", "Версия сборки" );
					node.SetAttribute("value", XmlTextEncoder.Encode(ex.TargetSite.DeclaringType.Assembly.FullName));
					exception.AppendChild( node );

					// Метод
					node = Document.CreateElement( "Node" );
					node.SetAttribute( "id", "Method" );
					node.SetAttribute( "name", "Метод" );
					node.SetAttribute("value", XmlTextEncoder.Encode(ex.TargetSite.DeclaringType.FullName + "." + ex.TargetSite.Name + "(...)"));
					exception.AppendChild( node );
				}

				// Stack Trace
				if( ex.StackTrace != null )
				{
					node = Document.CreateElement( "Node" );
					node.SetAttribute( "id", "Trace" );
					node.SetAttribute( "code", "1" );
					node.SetAttribute( "name", "Stack Trace" );
					node.SetAttribute( "value", XmlTextEncoder.Encode(ex.StackTrace.TrimEnd(new char[] { '\n', '\r' })) );
					exception.AppendChild( node );
				}

				#region Команда и строка подключения для SqlException

				if( ex.InnerException != null && ex.InnerException is SqlException )
				{
					if( ex is DetailedException && ( ex as DetailedException ).SqlCmd != null )
					{
						IDbCommand cmd = ( ex as DetailedException ).SqlCmd;

						node = Document.CreateElement( "Node" );
						node.SetAttribute( "id", "Connection" );
						node.SetAttribute( "name", "Sql Connection" );
						node.SetAttribute("value", cmd.Connection != null ? XmlTextEncoder.Encode(ClearPassword(cmd.Connection.ConnectionString)) : "");
						exception.AppendChild( node );

						node = Document.CreateElement( "Node" );
						node.SetAttribute( "id", "Command" );
						node.SetAttribute( "code", "1" );
						node.SetAttribute( "name", "Sql Command" );
						node.SetAttribute("value", XmlTextEncoder.Encode(cmd.CommandText) ?? "");
						exception.AppendChild( node );


						if( cmd.Parameters != null && cmd.Parameters.Count > 0 )
						{
							string sParams = "";
							foreach( SqlParameter p in cmd.Parameters )
								sParams += String.Format( "{0}='{1}';\n", p.ParameterName, p.Value );

							node = Document.CreateElement( "Node" );
							node.SetAttribute( "id", "Parameters" );
							node.SetAttribute( "code", "1" );
							node.SetAttribute( "name", "Sql Parameters" );
							node.SetAttribute("value", XmlTextEncoder.Encode(sParams.TrimEnd(new char[] { '\n', '\r' })));
							exception.AppendChild( node );
						}
					}
					else
						SQLWarning = WarningSQL;
				}

				#endregion

				// Помечаем первое DetailedException, оборачивающее оригинальное исключение
				if( ex is LogicalException || ex is DetailedException && ( ex.InnerException == null || ex.InnerException != null && !(ex.InnerException is DetailedException) ) )
					exception.SetAttribute( "first", "1" );

				exceptions.AppendChild( exception );
			}

			root.AppendChild( exceptions );

			#endregion

			#region Сбор информации о контексте выполнения программы

			XmlElement info = Document.CreateElement( "Info" );

			// Название приложения
			node = Document.CreateElement( "Node" );
			node.SetAttribute( "id", "AppName" );
			node.SetAttribute( "name", "Приложение" );
			node.SetAttribute("value", XmlTextEncoder.Encode(_appName));
			info.AppendChild( node );

			#region Задание свойств текущей ошибке на основании разорбанной цепочки исключений

			info.SetAttribute("type", XmlTextEncoder.Encode(type));
			info.SetAttribute("message", XmlTextEncoder.Encode(message));
			info.SetAttribute( "mail", sendMail ? "1" : "0" );

			if( build == "" || version == "" || method == "" )
				info.SetAttribute("Build", XmlTextEncoder.Encode(WarningBuild));

			if( SQLWarning != "" )
				info.SetAttribute("SQL", XmlTextEncoder.Encode(SQLWarning));

			// Приоритет
			node = Document.CreateElement( "Node" );
			node.SetAttribute( "id", "Priority" );
			node.SetAttribute( "name", "Приоритет" );
			node.SetAttribute("value", XmlTextEncoder.Encode(priority));
			info.AppendChild( node );

			// Сборка
			if( build != "" )
			{
				node = Document.CreateElement( "Node" );
				node.SetAttribute( "id", "Component" );
				node.SetAttribute( "name", "Сборка" );
				node.SetAttribute("value", XmlTextEncoder.Encode(build));
				info.AppendChild( node );
			}

			// Версия сборки
			if( version != "" )
			{
				node = Document.CreateElement( "Node" );
				node.SetAttribute( "id", "ComponentVersion" );
				node.SetAttribute( "name", "Версия сборки" );
				node.SetAttribute("value", XmlTextEncoder.Encode(version));
				info.AppendChild( node );
			}

			// Функция
			if( method != "" )
			{
				node = Document.CreateElement( "Node" );
				node.SetAttribute( "id", "Function" );
				node.SetAttribute( "name", "Функция" );
				node.SetAttribute("value", XmlTextEncoder.Encode(method));
				info.AppendChild( node );
			}

			// HRESULT
			if( hResult != 0 )
			{
				node = Document.CreateElement( "Node" );
				node.SetAttribute( "id", "hresult" );
				node.SetAttribute( "name", "HRESULT" );
				node.SetAttribute( "value", hResult.ToString() );
				info.AppendChild( node );
			}

			#endregion

			// Рабочая папка приложения
			node = Document.CreateElement( "Node" );
			node.SetAttribute( "id", "BaseDirectory" );
			node.SetAttribute( "name", "Base directory" );
			node.SetAttribute("value", XmlTextEncoder.Encode(AppDomain.CurrentDomain.BaseDirectory));
			info.AppendChild( node );

			// Компьютер
			node = Document.CreateElement( "Node" );
			node.SetAttribute( "id", "Computer" );
			node.SetAttribute( "name", "Компьютер" );
			node.SetAttribute("value", XmlTextEncoder.Encode(Dns.GetHostName()));
			info.AppendChild( node );

			// Пользователь
			node = Document.CreateElement( "Node" );
			node.SetAttribute( "id", "UserLogin" );
			node.SetAttribute( "name", "Пользователь" );
			node.SetAttribute( "value", XmlTextEncoder.Encode(
				Thread.CurrentPrincipal.Identity.Name != WindowsIdentity.GetCurrent().Name ?
				Thread.CurrentPrincipal.Identity.Name + "(" + WindowsIdentity.GetCurrent().Name + ")" :
				Thread.CurrentPrincipal.Identity.Name ));
			info.AppendChild( node );

			// Время ошибки
			node = Document.CreateElement( "Node" );
			node.SetAttribute( "id", "TimeError" );
			node.SetAttribute( "name", "Время ошибки" );
			node.SetAttribute( "value", DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss") );
			info.AppendChild( node );

			// Версия Framework
			node = Document.CreateElement( "Node" );
			node.SetAttribute( "id", "Framework" );
			node.SetAttribute( "name", "Framework" );
			node.SetAttribute("value", XmlTextEncoder.Encode(Environment.Version.ToString()));
			info.AppendChild( node );

			// Для веб приложений собираем заголовки запроса и путь, по которому открыли страницу
			if(System.Web.HttpContext.Current != null )
			{
				System.Web.HttpRequest req = System.Web.HttpContext.Current.Request;
				if( req != null )
				{
                    if (!string.IsNullOrEmpty(req.UserHostAddress))
                    {
                        try
                        {
                            IPHostEntry entry = Dns.GetHostEntry(req.UserHostAddress);
                            string domname = (entry != null ? entry.HostName : "");
                            domname = domname.Substring(domname.IndexOf(".") + 1);
                            if (!domname.Equals(_domain_name, StringComparison.InvariantCultureIgnoreCase))
                                _domain_name = "";
                        }
                        catch { }
                    }
					node = Document.CreateElement( "Node" );
					node.SetAttribute( "id", "UserHostAddress" );
					node.SetAttribute( "name", "UserHostAddress" );
					node.SetAttribute("value", XmlTextEncoder.Encode(req.UserHostAddress));
					info.AppendChild( node );

					node = Document.CreateElement( "Node" );
					node.SetAttribute( "id", "URI" );
					node.SetAttribute( "name", "URI" );
					node.SetAttribute("value", XmlTextEncoder.Encode(req.Url.AbsoluteUri));
					info.AppendChild( node );

					node = Document.CreateElement( "Node" );
					node.SetAttribute( "id", "RequestType" );
					node.SetAttribute( "name", "RequestType" );
					node.SetAttribute( "value", req.RequestType );
					info.AppendChild( node );

					string headers = "";
					foreach( string key in req.Headers.AllKeys )
						headers += String.Format("{0}->[{1}]\n", key, (key == "Authorization" && req.Headers[key].Length > 100 ? req.Headers[key].Substring(0, 100) : req.Headers[key]));

					node = Document.CreateElement( "Node" );
					node.SetAttribute( "id", "RequestHeaders" );
					node.SetAttribute( "code", "1" );
					node.SetAttribute( "name", "Request headers" );
					node.SetAttribute("value", XmlTextEncoder.Encode(headers.Trim()));
					info.AppendChild( node );
				}
			}

			// Используется памяти
			node = Document.CreateElement( "Node" );
			node.SetAttribute( "id", "MemoryUsed" );
			node.SetAttribute( "name", "Занято памяти" );
			node.SetAttribute( "value", GC.GetTotalMemory(true).ToString("### ### ### ### ###") );
			info.AppendChild( node );

			root.AppendChild( info );

			#endregion

			return Document.DocumentElement;
		}


		/// <summary>
		/// Очистка пароля в строке
		/// </summary>
		/// <param name="str">исходная строка</param>
		public static string ClearPassword( string str )
		{
			string connString = "";
			if( str != null && str != String.Empty )
				connString = str;
			connString += ";";

			//connString = @"Provider = SQLOLEDB;Data Source = myServerAddress;Initial Catalog = myDataBase;PWD=123456;User Id=myUsername; Password  =  myPassword;";

			// replace whitespaces around '=' symbol: 'Password  =    myPassword;' replaced with 'Password=myPassword;'
			Regex myRegex = new Regex(@"\s*=\s*", RegexOptions.IgnoreCase);
			connString = myRegex.Replace(connString, "=");

			// replace password marked with Password keyword: 'Password=myPassword;' replaced with 'Password=*****;'
			myRegex = new Regex(@"Password=([^;]*);", RegexOptions.IgnoreCase);
			connString = myRegex.Replace(connString, "Password=*****;");

			// replace password marked with PWD keyword: 'PWD=12345;' replaced with 'PWD=*****;'
			myRegex = new Regex(@"PWD=([^;]*);", RegexOptions.IgnoreCase);
			connString = myRegex.Replace(connString, "PWD=*****;");

			return connString.Substring(0, connString.Length - 1);
		}


		/// <summary>
		/// Отправка сообщения через SMTP
		/// </summary>
		/// <param name="subject">тема сообщения</param>
		/// <param name="xml">XML с информацией по ошибке</param>
		internal void SendMail( string subject, XmlElement xml )
		{
			// Формирование тела письма в формате HTML
			XslCompiledTransform xslt = new XslCompiledTransform();
			using(Stream stream = this.GetType().Assembly.GetManifestResourceStream(this.GetType().Namespace + ".Letter.xslt"))
			{
				XmlDocument Doc = new XmlDocument();
				Doc.Load( stream );
				xslt.Load( Doc, null, null );
			}

			XPathNavigator nav = xml.CreateNavigator();
			StringWriter wr = new StringWriter();

			xslt.Transform( nav, null, wr );

			// Формирование письма
		    using (MailMessage msg = new MailMessage(
		        String.Format("\"{0}\" <{1}>", _appName, _supportEmail),
		        _supportEmail,
		        subject,
		        wr.ToString()
		        ))
		    {
		        msg.IsBodyHtml = true;

		        /* вариант не работает
			msg.BodyEncoding = System.Text.Encoding.GetEncoding("koi8-r");
			msg.BodyEncoding = System.Text.Encoding.Default;
			msg.Headers["Content-type"] = "text/plain; charset=koi8-r";
			*/

		        /*вариант работает, но не подходит для эстонского
			int codepage = 1251;
			msg.BodyEncoding = System.Text.Encoding.GetEncoding(codepage);
			//msg.SubjectEncoding = System.Text.Encoding.GetEncoding(codepage);
			msg.Headers.Set("Content-Type", "text/plain; charset=windows-1251");
			*/

		        msg.BodyEncoding = System.Text.Encoding.UTF8;
		        msg.Headers.Set("Content-Type", "text/plain; charset=UTF-8");

		        lock (lockSmtp)
		        {
		            SmtpClient client = null;
		            try
		            {
                        
		                client = new SmtpClient(_smtpServer);

#if !NET_4_OR_GREATER					// Должен быть больше 2. Интервал во внутренней обработке объекта делится на 2.
						client.ServicePoint.MaxIdleTime = 4;
#endif
						client.Send(msg);
                        

		            }
		            catch (Exception ex)
		            {
		                // Формирование тела записи об ошибке в журнале Windows
		                xslt = new XslCompiledTransform();
		                using (
		                    Stream stream =
		                        this.GetType()
		                            .Assembly.GetManifestResourceStream(this.GetType().Namespace + ".PlainText.xslt"))
		                {
		                    XmlDocument Doc = new XmlDocument();
		                    Doc.Load(stream);
		                    xslt.Load(Doc, null, null);
		                }
		                nav = xml.CreateNavigator();
		                wr = new StringWriter();

		                xslt.Transform(nav, null, wr);

		                string errorMsg = string.Format(@"
Не удалось доставить сообщение об ошибке
SMTP сервер: {0}.
Email_Support: {1}.
Sender: {2}.
Заголовок: {3}.
Inner Exception: {4}.

{5}", _smtpServer, _supportEmail, _appName, subject, ex.Message, wr.ToString());

		                FailedMailSend(_appName, Priority.Error, errorMsg);
		            }
		            finally
		            {
		                IncState(-1);

#if NET_4_OR_GREATER
if (client != null) client.Dispose();
#endif
					}
		        }
		    }
		}


		/// <summary>
		/// Регистрация исключительных ситуаций, которые не удалось отправить в Email в EventLog
		/// </summary>
		/// <param name="appName">Название приложения</param>
		/// <param name="p">Приоритет сообщения</param>
		/// <param name="plainText">Зарание подоготовленный текст сообщения</param>
		private void FailedMailSend( string appName, Priority p, string plainText )
		{
			lock( lockObj )
			{
				TaskEventLogReg eventLogReg = new TaskEventLogReg( p, appName, plainText );
				eventLogReg.Execute();

				DisposeLog();
			}
		}


		/// <summary>
		/// Вызов события реинициализаиции модуля регистрации исключительных ситуаций
		/// </summary>
		private void DisposeLog()
		{
			if( OnDispose != null ) OnDispose(this);
		}


		/// <summary>
		/// Возвращает проставляемы приоритет ошибки в соответствии с регламентом
		/// </summary>
		/// <param name="ex">Пойманная ошибка</param>
		/// <param name="pr">Приоритет по-умолчанию</param>
		private static Priority GetPriority(Exception ex, Priority pr)
		{
			if (ex == null)
				return pr;

			while (ex.InnerException != null)
				ex = ex.InnerException;

			if (ex is SqlException && Enum.IsDefined(typeof(SqlErrors), ((SqlException)ex).Number))
				return Priority.ExternalError;

			if (ex.Message.Contains("provider: Named Pipes Provider, error: 40") || ex.Message.Contains("provider: Поставщик именованных каналов, error: 40"))
				return Priority.ExternalError;

			if (ex is SqlException && ex.StackTrace.Contains("System.Data.SqlClient.SqlConnection.Open()"))
				return Priority.ExternalError;

            if (ex is System.Net.WebException && ((System.Net.WebException)ex).Status == WebExceptionStatus.ProtocolError)
            {
                var response = ((System.Net.WebException)ex).Response as HttpWebResponse;
                if (response != null && (int)response.StatusCode >= 500)
                    return Priority.ExternalError;
            }

			int hr = Marshal.GetHRForException(ex);
			if (hr == HRESULTs.COR_E_NOTSUPPORTED || hr == HRESULTs.COR_E_UNAUTHORIZEDACCESS || hr == HRESULTs.COR_E_TIMEOUT)
				return Priority.ExternalError;

			if (ex is SqlException && (/*hr == HRESULTs.COR_E_LOGIN_FAILED ||*/ hr == HRESULTs.COR_E_LOGIN_FAILED1))
				return Priority.ExternalError;

			if (ex is System.Runtime.InteropServices.COMException && hr == HRESULTs.COR_E_SRV_NOT_OPERATIONAL)
				return Priority.ExternalError;

			if (ex is System.Web.Services.Protocols.SoapException && ex.StackTrace.Contains("ReportingService") && hr == HRESULTs.COR_E_SRV_REFUSED)
				return Priority.ExternalError;

			if (ex is System.Net.Sockets.SocketException && Enum.IsDefined(typeof(TcpErrors), ((System.Net.Sockets.SocketException)ex).ErrorCode))
				return Priority.ExternalError;

			if (ex is System.IO.IOException && hr == HRESULTs.COR_E_NET_NOT_REACHED)
				return Priority.ExternalError;

			if (ex is System.Net.WebException)
			{
				System.Net.WebException we = ex as System.Net.WebException;
				if (we.Status != System.Net.WebExceptionStatus.ProtocolError && we.Status != System.Net.WebExceptionStatus.UnknownError)
					return Priority.ExternalError;
			}

			return pr;
		}
	}

    internal class SentMessage
    {
        private readonly string _host;
        private readonly string _user;
        private readonly string _subj;
        private readonly string _meth;
        public DateTime DtCreate { get; private set; }

        internal SentMessage(string hst, string usr, string mth, string sbj)
        {
            _host = hst;
            _user = usr;
            _subj = sbj;
            _meth = mth;
            DtCreate = DateTime.Now;
        }

        public override bool Equals(object obj)
        {
        if(obj == null)
            return false;

        if(!(obj is SentMessage))
            return false;

        var cmpr = obj as SentMessage;
        return	_host.Equals(cmpr._host, StringComparison.InvariantCultureIgnoreCase) && 
                _user.Equals(cmpr._user, StringComparison.InvariantCultureIgnoreCase) &&
                _subj.Equals(cmpr._subj, StringComparison.InvariantCultureIgnoreCase) &&
                _meth.Equals(cmpr._meth, StringComparison.InvariantCultureIgnoreCase);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (_host != null ? _host.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_user != null ? _user.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_subj != null ? _subj.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_meth != null ? _meth.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DtCreate.GetHashCode();
                return hashCode;
            }
        }
    }
}
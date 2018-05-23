namespace Kesco.Lib.Log
{
	/// <summary>
	/// Приоритеты сообщений
	/// </summary>
	public enum Priority
	{
		/// <summary>
		/// Ошибка - приоритет по-умолчанию. Ошибки с таким приоритетом автоматчески разбираются и заводятся в Заявках IT.
		/// </summary>
		Error = 1,
		/// <summary>
		/// Высший приоритет ошибки. По ошибкам с таким приоритетом отправляется смс сообщение дежурному инжененру.
		/// </summary>
		Alarm = 2,
		/// <summary>
		/// Информационное сообщение.
		/// </summary>
		Info = 3,
		/// <summary>
		/// Ошибка окружения. Не зависит от приложения - сбой при обращении/использовании внешних ресурсов. Например, подключение к серверам, оборудованию...
		/// </summary>
		ExternalError = 4
    }

    internal enum SqlErrors
    {
        SQL_CONNECTION_FAILED = -2,
        SQL_CONNECTION_FAILED1 = -1,
        SQL_CONNECTION_FAILED3 = 2,
        SQL_CONNECTION_FAILED4 = 51,
        SQL_CONNECTION_FAILED5 = 53,
        SQL_CONNECTION_FAILED6 = 64,
        SQL_CONNECTION_FAILED7 = 233,
        SQL_CONNECTION_FAILED8 = 10054,
        SQL_CONNECTION_FAILED9 = 10060,
        SQL_CONNECTION_FAILED10 = 10061,
        SQL_CONNECTION_FAILED11 = 11001,
        SQL_CONNECTION_FAILED12 = 17806,
        SQL_INIT_SRVC_PAUSED = 17142,
        SQL_STARTUP_SERVER_KILLED = 17147,
        SQL_STARTUP_SERVER_UNINSTALL = 17148,
        SQL_SRV_NO_FREE_CONN = 17809,
        SQL_LOGON_INVALID_CONNECT1 = 18452,
        SQL_LOGON_INVALID_CONNECT2 = 18456,
        SQL_LOGON_INVALID_CONNECT3 = 1385,
        SQL_DB_UFAIL_FATAL = 4064,
        SQL_PERM_DEN = 229,
        SQL_MEMTIME_OUT = 8645,
        SQL_DEADLOCK = 1205,
        SQL_SERVER_SHUTDOWN = 6005,
        SQL_LOG_FULL = 9002
    };

    // HRESULTs:
    internal struct HRESULTs
    {
        internal const int COR_E_NOTSUPPORTED = unchecked((int)0x80131515);
        //internal const int COR_E_INVALOPERATION = unchecked((int)0x80131509);
        internal const int COR_E_UNAUTHORIZEDACCESS = unchecked((int)0x80070005);
        internal const int COR_E_TIMEOUT = unchecked((int)0x80131505);
        internal const int COR_E_LOGIN_FAILED = unchecked((int)0x80131904);
        internal const int COR_E_SRV_NOT_OPERATIONAL = unchecked((int)0x8007203A);
        internal const int COR_E_SRV_REFUSED = unchecked((int)0x80131501);
        internal const int COR_E_LOGIN_FAILED1 = unchecked((int)0x80040E4D);
        internal const int COR_E_NET_NOT_REACHED = unchecked((int)0x800704d0);
    }

    internal enum TcpErrors
    {
        REMOTE_CONNECTION_FAILED1 = 10054,
        REMOTE_CONNECTION_FAILED2 = 11001,  /* No such host is known*/
        REMOTE_CONNECTION_FAILED3 = 10061, 
        REMOTE_CONNECTION_HOST_UNAVAILABLE = 10065, 
        REMOTE_CONNECTION_TIMEOUT_FAILED = 10060
    };
}

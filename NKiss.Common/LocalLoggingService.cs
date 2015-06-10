
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Configuration;

namespace NKiss.Common
{
    public enum LogLevel
    {
        None = 99,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4
    }

    public class LocalLoggingService
    {
        private static readonly object locker = new object();
        private static StreamWriter sw;
        private static Timer changePathTimer;
        private static readonly int CHANGEPATHINTERVAL = 30 * 1000;
        private static readonly string LOGFILENAMEFORMAT = "yyyyMMdd_HHmm";
        private static readonly string LOGLINEFORMAT = "HH:mm:ss_ffff";
        private static LogLevel logLevel = LogLevel.Debug;

        static LocalLoggingService()
        {
            if (ConfigurationManager.AppSettings["LogLevel"] != null)
            {
                LogLevel l;
                if (Enum.TryParse(ConfigurationManager.AppSettings["LogLevel"], out l))
                    logLevel = l;
            }
            changePathTimer = new Timer(state =>
            {
                lock (locker)
                {
                    Close();
                    InitStreamWriter();
                }
            }, null, CHANGEPATHINTERVAL, CHANGEPATHINTERVAL);
            InitStreamWriter();
        }

        private static void Close()
        {
            try
            {
                if (sw != null)
                    sw.Close();
            }
            catch
            {
            }
        }

        private static void InitStreamWriter()
        {
            try
            {
                sw = new StreamWriter(GetLogFileName(), true, Encoding.UTF8, 1024);
                sw.AutoFlush = true;
            }
            catch
            {
            }
        }

        private static string GetLogFileName()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string file = DateTime.Now.ToString(LOGFILENAMEFORMAT) + ".txt";
            return Path.Combine(path, file);
        }

        private static string WrapWithContext(LogLevel logLevel, string s)
        {
            return string.Format("[{0}] @{1} #{2} - {3}",
                logLevel,
                DateTime.Now.ToString(LOGLINEFORMAT),
                Thread.CurrentThread.ManagedThreadId, s);
        }

        public static void Debug(Func<string> s)
        {
            Log(LogLevel.Debug, s);
        }

        public static void Debug(string s)
        {
            Log(LogLevel.Debug, s);
        }

        public static void Debug(string format, params object[] args)
        {
            Log(LogLevel.Debug, format, args);
        }

        public static void Info(Func<string> s)
        {
            Log(LogLevel.Info, s);
        }

        public static void Info(string s)
        {
            Log(LogLevel.Info, s);
        }

        public static void Info(string format, params object[] args)
        {
            Log(LogLevel.Info, format, args);
        }

        public static void Warning(Func<string> s)
        {
            Log(LogLevel.Warning, s);
        }

        public static void Warning(string s)
        {
            Log(LogLevel.Warning, s);
        }

        public static void Warning(string format, params object[] args)
        {
            Log(LogLevel.Warning, format, args);
        }

        public static void Error(Func<string> s)
        {
            Log(LogLevel.Error, s);
        }

        public static void Error(string s)
        {
            Log(LogLevel.Error, s);
        }

        public static void Error(string format, params object[] args)
        {
            Log(LogLevel.Error, format, args);
        }

        public static void Log(LogLevel level, string format, params object[] args)
        {
            if ((int)logLevel <= (int)level)
                InternalLog(level, string.Format(format, args));
        }

        public static void Log(LogLevel level, string s)
        {
            if ((int)logLevel <= (int)level)
                InternalLog(level, s);
        }

        public static void Log(LogLevel level, Func<string> s)
        {
            if ((int)logLevel <= (int)level)
                InternalLog(level, s());
        }

        private static void InternalLog(LogLevel logLevel, string s)
        {
            try
            {

                lock (locker)
                {
                    var message = WrapWithContext(logLevel, s);
#if DEBUG
                    switch (logLevel)
                    {
                        case LogLevel.Debug:
                            Console.ForegroundColor = ConsoleColor.Gray;
                            break;
                        case LogLevel.Info:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case LogLevel.Warning:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case LogLevel.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                    }
                    Console.WriteLine(message);
                    Console.ResetColor();
#endif
                    sw.WriteLine(message);
                }
            }
            catch
            {
            }
        }
    }
}

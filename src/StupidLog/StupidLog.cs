namespace StupidLog
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public enum LogLevel
    {
        Verbose = 0,
        Info,
        Warning,
        Error,
        Min = Verbose,
        Max = Error,
    }

    internal static class LogLevelHelper
    {
        public static bool TryParseLogLevel(string levelString, out LogLevel level)
        {
            level = LogLevel.Verbose;
            levelString = levelString.ToUpper();
            if (levelString.StartsWith("[V]"))
            {
                level = LogLevel.Verbose;
                return true;
            }

            if (levelString.StartsWith("[I]"))
            {
                level = LogLevel.Info;
                return true;
            }

            if (levelString.StartsWith("[W]"))
            {
                level = LogLevel.Warning;
                return true;
            }

            if (levelString.StartsWith("[E]"))
            {
                level = LogLevel.Error;
                return true;
            }

            return false;
        }

        public static string ToLogString(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose:
                    return "[V]";
                case LogLevel.Info:
                    return "[I]";
                case LogLevel.Warning:
                    return "[W]";
                case LogLevel.Error:
                    return "[E]";
                default:
                    throw new ArgumentException("Not a valid level: " + level.ToString());
            }
        }
    }

    public class StupidLog
    {
        public static readonly TextWriter OriginalConsoleOut;

        static StupidLog()
        {
            OriginalConsoleOut = Console.Out;
        }

        public static void Stupidify(IEnumerable<ILogger> loggers = null)
        {
            loggers = loggers ?? new[] { new ModifiedConsoleOut() };
            Console.SetOut(new LogDispatcher(loggers, GetMinLogLevel()));
        }

        private static LogLevel GetMinLogLevel()
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("----"))
                {
                    // It's unfortunate that I can't remove this fucking arg from the args list
                    string level = arg[4].ToString().ToUpper();
                    switch (level)
                    {
                        case "V": return LogLevel.Verbose;
                        case "I": return LogLevel.Info;
                        case "W": return LogLevel.Warning;
                        case "E": return LogLevel.Error;
                        default: break;
                    }

                    break;
                }
            }

            return LogLevel.Info;
        }

        public interface ILogger : IDisposable
        {
            void Log(LogLevel level, string content);
        }

        private class LogDispatcher : TextWriter
        {
            private readonly LogLevel minLogLevel;
            private List<ILogger> loggers;
            public LogDispatcher(IEnumerable<ILogger> loggers, LogLevel minLogLevel)
            {
                this.loggers = loggers.ToList();
                this.minLogLevel = minLogLevel;
            }

            public override Encoding Encoding
            {
                get
                {
                    // This is not redirected to log targets. If ppl call this, we fallback to the true stdout.
                    return OriginalConsoleOut.Encoding;
                }
            }

            public override void Write(char value)
            {
                // This is not redirected to log targets. If ppl call this, we fallback to the true stdout.
                OriginalConsoleOut.Write(value);
            }

            private static LogLevel GetLogLevel(ref string line)
            {
                if (line == null || line.Length < 3)
                {
                    return LogLevel.Verbose;
                }

                LogLevel level = LogLevel.Verbose;
                if (LogLevelHelper.TryParseLogLevel(line, out level))
                {
                    line = line.Substring(3);
                }

                return level;
            }

            public override void WriteLine(string format, params object[] arg)
            {
                string line = string.Format(format, arg);
                this.WriteLine(line);
            }

            public override void WriteLine(string value)
            {
                var logLevel = GetLogLevel(ref value);
                if (logLevel < this.minLogLevel) return;
                this.loggers.ForEach(logger => logger.Log(logLevel, value));
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (this.loggers != null)
                    {
                        this.loggers.ForEach(target => target.Dispose());
                    }

                    this.loggers = null;
                }

                base.Dispose(disposing);
            }
        }

        public class ModifiedConsoleOut : ILogger
        {
            public void Dispose()
            {
            }

            public ModifiedConsoleOut()
            {
                Console.CancelKeyPress += (sender, e) =>
                {
                    Console.ResetColor();
                };
            }

            private static readonly Dictionary<LogLevel, ConsoleColor> colorMap = new Dictionary<LogLevel, ConsoleColor>
            {
                { LogLevel.Verbose, ConsoleColor.Gray },
                { LogLevel.Info, ConsoleColor.Green },
                { LogLevel.Warning, ConsoleColor.Yellow },
                { LogLevel.Error, ConsoleColor.Red },
            };

            public void Log(LogLevel level, string content)
            {
                try
                {
                    ConsoleColor fg;
                    if (colorMap.TryGetValue(level, out fg))
                    {
                        Console.ForegroundColor = fg;
                    }

                    OriginalConsoleOut.WriteLine(string.Format("{0} {1}", level.ToLogString(), content));
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
    }
}

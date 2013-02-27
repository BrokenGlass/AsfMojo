using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using AsfMojo.Media;

namespace AsfMojo.Utils
{
    public static class GuidHelper
    {
        public static Guid ToGuid(this byte[] guidBytes)
        {
            return new Guid(guidBytes);
        }
    }

    public enum LogLevel { logDebug = 0, logDetail, logInfo, logWarning, logError, logNone }
    public static class LogHelper
    {
        public static void Log(this string message, LogLevel level)
        {
            //TODO: decide on logging code
            //Console.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss}: {1}", DateTime.Now, message));
        }
    }
}

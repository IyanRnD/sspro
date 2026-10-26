using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServer {
    public class EventLogger {
        public static void Log(string eventMessage) {
            string logDirectory = "Logs";
            string fileName = $"{DateTime.Now:yyyy-MM-dd}.txt";
            string logFilePath = Path.Combine(logDirectory, fileName);

            // Create the log directory if it doesn't exist
            Directory.CreateDirectory(logDirectory);

            // Append the event message with timestamp to the log file
            using (StreamWriter sw = File.AppendText(logFilePath)) {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {eventMessage}";
                sw.WriteLine(logMessage);
            }
        }
    }
}

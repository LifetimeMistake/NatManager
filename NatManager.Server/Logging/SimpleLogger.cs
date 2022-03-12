using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server.Logging
{
    public class SimpleLogger : ILogger, IErrorHandler
    {
        private string outputPath;
        private SemaphoreSlim fileLock = new SemaphoreSlim(1, 1);
        public SimpleLogger(string outputDir)
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            outputPath = Path.Combine(outputDir, dateTime + ".log");
        }

        public async Task DebugAsync(string message)
        {
            await LogAsync(LogLevel.Debug, message);
        }

        public async Task ErrorAsync(string message)
        {
            await LogAsync(LogLevel.Error, message);
        }

        public async Task FatalAsync(string message)
        {
            await LogAsync(LogLevel.Fatal, message);
        }

        public async Task InfoAsync(string message)
        {
            await LogAsync(LogLevel.Info, message);
        }

        public async Task WarnAsync(string message)
        {
            await LogAsync(LogLevel.Warn, message);
        }

        public async Task LogAsync(LogLevel level, string message)
        {
            try
            {
                await fileLock.WaitAsync();
                string dateNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logMessage = $"[{dateNow}] [{level.ToString()}] {message}";
                Console.WriteLine(logMessage);
                await File.AppendAllTextAsync(outputPath, logMessage + "\n");
            }
            finally
            {
                fileLock.Release();
            }
        }

        public async void HandleException(Exception ex)
        {
            await ErrorAsync($"Unhandled internal server error: {ex}");
        }
    }
}

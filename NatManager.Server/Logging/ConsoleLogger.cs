using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.Logging
{
    public class ConsoleLogger : ILogger, IErrorHandler
    {
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

        public Task LogAsync(LogLevel level, string message)
        {
            string dateNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{dateNow}] [{level.ToString()}] {message}";
            Console.WriteLine(logMessage);
            return Task.CompletedTask;
        }

        public async void HandleException(Exception ex)
        {
            await ErrorAsync($"Unhandled internal server error: {ex}");
        }
    }
}

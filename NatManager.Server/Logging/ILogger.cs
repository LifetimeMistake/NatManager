using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.Logging
{
    public interface ILogger
    {
        public Task LogAsync(LogLevel level, string message);
        public Task DebugAsync(string message);
        public Task InfoAsync(string message);
        public Task WarnAsync(string message);
        public Task ErrorAsync(string message);
        public Task FatalAsync(string message);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.Processors
{
    public interface ICommandLineProcessor
    {
        string Verb { get; }
        string Description { get; }
        Task<bool> ProcessAsync(IEnumerable<string> args);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.Tables
{
    // Credit: https://stackoverflow.com/a/14698822
    public interface ITextRow
    {
        string Output();
        void Output(StringBuilder sb);
        object Tag { get; set; }
    }
}

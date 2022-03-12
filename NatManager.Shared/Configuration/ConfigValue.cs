using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.Configuration
{
    public class ConfigValue
    {
        public ConfigValueType Type;
        public byte[] Value;

        public ConfigValue(ConfigValueType type, byte[] value)
        {
            Type = type;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }


    }
}

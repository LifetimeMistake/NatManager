using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.Configuration
{
    public static class ConfigManagerRpcMethods
    {
        public const string ConfigEntryExists = "ConfigEntryExists";
        public const string GetAllConfigEntries = "GetAllConfigEntries";
        public const string GetConfigValueBytes = "GetConfigValueBytes";
        public const string GetConfigValueInt = "GetConfigValueInt";
        public const string GetConfigValueUInt = "GetConfigValueUInt";
        public const string GetConfigValueString = "GetConfigValueString";
        public const string SetConfigValueBytes = "SetConfigValueBytes";
        public const string SetConfigValueInt = "SetConfigValueInt";
        public const string SetConfigValueUInt = "SetConfigValueUInt";
        public const string SetConfigValueString = "SetConfigValueString";
    }
}

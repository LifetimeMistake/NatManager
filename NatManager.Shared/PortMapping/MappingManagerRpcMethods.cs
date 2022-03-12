using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.PortMapping
{
    public static class MappingManagerRpcMethods
    {
        public const string CreateMapping = "CreateMapping";
        public const string DeleteMapping = "DeleteMapping";
        public const string SetMappingOwner = "SetMappingOwner";
        public const string UpdateMapping = "UpdateMapping";
        public const string GetMappingInfo = "GetMappingInfo";
        public const string GetAllMappings = "GetAllMappings";
        public const string GetUsersMappings = "GetUsersMappings";
    }
}

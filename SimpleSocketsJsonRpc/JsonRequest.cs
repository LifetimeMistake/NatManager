// Based on the following implementation
// https://github.com/Astn/JSON-RPC.NET/blob/master/Json-Rpc/JsonRequest.cs

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSocketsJsonRpc
{
    public class JsonRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get => JsonRpcVersion.Version; }
        [JsonProperty("method")]
        public string? MethodName { get; set; }
        [JsonProperty("params")]
        public object? Params { get; set; }
        [JsonProperty("id")]
        public object? Id { get; set; }

        public JsonRequest()
        {
        }

        public JsonRequest(string methodName, object? parameters, object id)
        {
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            Params = parameters;
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }
    }
}

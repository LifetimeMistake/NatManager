// Based on the following implementation
// https://github.com/Astn/JSON-RPC.NET/blob/master/Json-Rpc/JsonResponseErrorObject.cs

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSocketsJsonRpc
{
    public class JsonRpcException : Exception
    {
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("message")]
        public new string Message { get; set; }
        [JsonProperty("data")]
        public new object Data { get; set; }

        public JsonRpcException(string code, string message, object data)
        {
            Code = code;
            Message = message;
            Data = data;
        }
    }
}

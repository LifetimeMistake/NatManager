// Based on the following implementation
// https://github.com/Astn/JSON-RPC.NET/blob/master/Json-Rpc/JsonResponse.cs

using Newtonsoft.Json;

namespace SimpleSocketsJsonRpc
{
    public class JsonResponse
    {
        [JsonProperty("jsonrpc", NullValueHandling = NullValueHandling.Ignore)]
        public string JsonRpc { get; set; } = JsonRpcVersion.Version;
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object? Result { get; set; }
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonRpcException? Error { get; set; }
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public object? Id { get; set; }
    }

    public class JsonResponse<T>
    {
        [JsonProperty("jsonrpc", NullValueHandling = NullValueHandling.Ignore)]
        public string JsonRpc { get; set; } = JsonRpcVersion.Version;
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public T Result { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonRpcException? Error { get; set; }
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public object? Id { get; set; }
    }
}

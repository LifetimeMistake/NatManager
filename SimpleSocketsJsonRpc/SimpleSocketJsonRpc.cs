using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleSockets.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSocketsJsonRpc
{
    public class SimpleSocketJsonRpc : IJsonRpc, IDisposable
    {
        private SimpleSocketClient transport;
        private List<int> reservedIds;

        public SimpleSocketJsonRpc(SimpleSocketClient simpleSocketClient)
        {
            this.transport = simpleSocketClient ?? throw new ArgumentNullException(nameof(simpleSocketClient));
            this.reservedIds = new List<int>();
        }

        public async Task InvokeAsync(string method, int timeoutMs, params object?[]? parameters)
        {
            await InvokeAsync<object>(method, timeoutMs, parameters);
        }

        public async Task InvokeAsync(string method, CancellationToken cancellationToken, params object?[]? parameters)
        {
            await InvokeAsync<object>(method, cancellationToken, parameters);
        }

        public async Task<T> InvokeAsync<T>(string method, int timeoutMs, params object?[]? parameters)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeoutMs);
            return await InvokeAsync<T>(method, cancellationTokenSource.Token, parameters);
        }

        public async Task<T> InvokeAsync<T>(string methodName, CancellationToken cancellationToken, params object?[]? parameters)
        {
            int requestId = ReserveRequestId();
            JsonRequest request = new JsonRequest(methodName, parameters, requestId);
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();

            var lostConnectionHandler = new DisconnectedFromServerDelegate((SimpleSocketClient client) =>
            {
                ApplicationException exception = new ApplicationException("Lost connection with the remote server before the request could complete.");
                taskCompletionSource.TrySetException(exception);
            });
            var cancelledHandler = new Action(() =>
            {
                taskCompletionSource.TrySetCanceled();
            });
            var messageReceivedHandler = new MessageReceivedDelegate((SimpleSocketClient client, string msg) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                JsonResponse<T>? rpcResponse;
                try
                {
                    rpcResponse = JsonConvert.DeserializeObject<JsonResponse<T>>(msg);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                    return;
                }

                if (rpcResponse == null)
                    return;

                if (rpcResponse.Id == null)
                    return;

                if (requestId.GetHashCode() != rpcResponse.Id.GetHashCode())
                    return; // wrong response

                if (rpcResponse.Error != null)
                {
                    JsonRpcException rpcException = rpcResponse.Error;
                    if (rpcException.Data != null)
                    {
                        JToken innerException = (JToken)rpcException.Data;
                        string? message = (string?)innerException["Message"];
                        string? source = (string?)innerException["Source"];
                        string? helpLink = (string?)innerException["HelpLink"];
                        int? hResult = (int?)innerException["HResult"];

                        Exception exception = new Exception(message);
                        exception.Source = source;
                        exception.HelpLink = helpLink;

                        if(hResult != null)
                            exception.HResult = hResult.Value;

                        taskCompletionSource.TrySetException(exception);
                    }
                    else
                        taskCompletionSource.TrySetException(rpcException);
                }
                else
                {
                    taskCompletionSource.TrySetResult(rpcResponse.Result);
                }
            });

            try
            {
                transport.DisconnectedFromServer += lostConnectionHandler;
                transport.MessageReceived += messageReceivedHandler;
                cancellationToken.Register(cancelledHandler);
                string requestString = JsonConvert.SerializeObject(request);
                transport.SendMessage(requestString);
                return await taskCompletionSource.Task;
            }
            finally
            {
                transport.DisconnectedFromServer -= lostConnectionHandler;
                transport.MessageReceived -= messageReceivedHandler;
                ReleaseRequestId(requestId);
            }
        }

        private int ReserveRequestId()
        {
            lock(reservedIds)
            {
                int lastId = reservedIds.OrderByDescending(id => id).FirstOrDefault();
                int reservedId = lastId + 1;
                reservedIds.Add(reservedId);
                return reservedId;
            }
        }

        private bool ReleaseRequestId(int id)
        {
            lock(reservedIds)
            {
                return reservedIds.Remove(id);
            }
        }

        public void Dispose()
        {
            lock(reservedIds)
            {
                reservedIds.Clear();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSocketsJsonRpc
{
    public interface IJsonRpc : IDisposable
    {
        Task InvokeAsync(string method, int timeoutMs, params object?[]? parameters);
        Task InvokeAsync(string method, CancellationToken cancellationToken, params object?[]? parameters);
        Task<T> InvokeAsync<T>(string method, int timeoutMs, params object?[]? parameters);
        Task<T> InvokeAsync<T>(string method, CancellationToken cancellationToken, params object?[]? parameters);
    }
}

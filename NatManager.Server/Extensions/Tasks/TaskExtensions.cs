using NatManager.Server.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server.Extensions
{
    public static class TaskExtensions
    {
        public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler? errorHandler)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                errorHandler?.HandleException(ex);
            }
        }

        public static async Task ExecuteWithinLock(this Task task, SemaphoreSlim semaphore)
        {
            try
            {
                await semaphore.WaitAsync();
                await task;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static async Task<T> ExecuteWithinLock<T>(this Task<T> task, SemaphoreSlim semaphore)
        {
            try
            {
                await semaphore.WaitAsync();
                return await task;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}

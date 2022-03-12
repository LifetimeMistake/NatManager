using AustinHarris.JsonRpc;
using NatManager.Server.Configuration;
using NatManager.Server.Extensions;
using NatManager.Server.RPC.EventArgs;
using NatManager.Server.RPC.Services;
using NatManager.Server.Users;
using NatManager.Shared;
using NatManager.Shared.Users;
using SimpleSockets.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server.RPC
{
    public class TcpRpcServer : IDaemonService
    {
        private const uint DEFAULT_LISTENER_PORT = 30000;
        private const string CONFIG_KEY_DEFAULT_LISTENER_PORT = "natmanagerd.tcprpcserver.listenerPort";
        private const uint DEFAULT_MAX_CONNECTIONS_COUNT = 50;
        private const string CONFIG_KEY_MAX_CONNECTIONS_COUNT = "natmanagerd.tcprpcserver.connectionLimit";
        private uint listenerPort;
        private uint maxConnectionsCount;

        private IDaemon? daemon;
        private ServiceState serviceState;

        protected readonly Dictionary<int, RpcSessionContext> sessionsList = new Dictionary<int, RpcSessionContext>();
        protected SemaphoreSlim sessionsListLock = new SemaphoreSlim(1, 1);
        protected List<JsonRpcService> registeredServices = new List<JsonRpcService>();
        private SimpleSocketListener? tcpListener;

         public IDaemon? Daemon { get { return daemon; } set { daemon = value; } }
        public ServiceState State { get { return serviceState; } }

        public event EventHandler<SessionEventArgs>? SessionCreated;
        public event EventHandler<SessionEventArgs>? SessionDestroyed;
        public event EventHandler<SessionEventArgs>? SessionIdentityChanged;

        public TcpRpcServer(IDaemon daemon)
        {
            this.daemon = daemon;
            registeredServices.Add(new SessionManagementRpcService(this));
            serviceState = ServiceState.Stopped;
        }

        public TcpRpcServer(IDaemon daemon, IEnumerable<JsonRpcService> services) : this(daemon)
        {
            foreach (JsonRpcService service in services)
            {
                if (registeredServices.Any(s => s.GetType() == service.GetType()))
                    throw new InvalidOperationException("Attempted to register a duplicate service: " + service.GetType());
                else
                    registeredServices.Add(service);
            }
        }

        public async Task StartAsync()
        {
            await StopAsync();
            ThrowIfNotReady(false);
            await RetrieveConfig();

            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            UserManager userManager = await daemon.GetServiceAsync<UserManager>();

            configManager.ConfigEntryUpdated += ConfigManager_ConfigEntryUpdated;
            userManager.UserUpdated += UserManager_UserUpdated;
            userManager.UserDeleted += UserManager_UserDeleted;

            AustinHarris.JsonRpc.Config.SetErrorHandler(OnRpcException);
            AustinHarris.JsonRpc.Config.SetPostProcessHandler(OnRpcPostProcess);

            tcpListener = new SimpleSocketTcpListener();
            tcpListener.AllowReceivingFiles = false;
            tcpListener.ClientConnected += TcpListener_ClientConnected;
            tcpListener.ClientDisconnected += TcpListener_ClientDisconnected;
            tcpListener.MessageReceived += TcpListener_MessageReceived;
            tcpListener.StartListening((int)listenerPort, (int)maxConnectionsCount);

            serviceState = ServiceState.Running;
        }

        public async Task StopAsync()
        {
            if (tcpListener != null)
            {
                tcpListener.Dispose();
                tcpListener.ClientConnected -= TcpListener_ClientConnected;
                tcpListener.ClientDisconnected -= TcpListener_ClientDisconnected;
                tcpListener.MessageReceived -= TcpListener_MessageReceived;
            }

            if (daemon != null)
            {
                ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
                UserManager userManager = await daemon.GetServiceAsync<UserManager>();

                configManager.ConfigEntryUpdated -= ConfigManager_ConfigEntryUpdated;
                userManager.UserUpdated -= UserManager_UserUpdated;
                userManager.UserDeleted -= UserManager_UserDeleted;
            }

            AustinHarris.JsonRpc.Config.SetErrorHandler(null);
            AustinHarris.JsonRpc.Config.SetPostProcessHandler(null);

            try
            {
                await sessionsListLock.WaitAsync();
                sessionsList.Clear();
            }
            finally
            {
                sessionsListLock.Release();
            }

            tcpListener = null;

            serviceState = ServiceState.Stopped;
        }

        [MemberNotNull(nameof(daemon))]
        public void ThrowIfNotReady(bool checkState = true)
        {
            if (daemon == null)
                throw new InvalidOperationException("Requested service is not attached to a daemon instance");

            if (serviceState != ServiceState.Running && checkState)
                throw new InvalidOperationException("Requested service or it's dependency is not available");
        }

        private async Task RetrieveConfig()
        {
            ThrowIfNotReady(false);
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            if (!await configManager.ConfigEntryExistsAsync(CONFIG_KEY_DEFAULT_LISTENER_PORT))
            {
                await configManager.SetConfigValueAsync(CONFIG_KEY_DEFAULT_LISTENER_PORT, DEFAULT_LISTENER_PORT);
                listenerPort = DEFAULT_LISTENER_PORT;
            }
            else
            {
                listenerPort = await configManager.GetConfigValueUIntAsync(CONFIG_KEY_DEFAULT_LISTENER_PORT);
            }

            if (!await configManager.ConfigEntryExistsAsync(CONFIG_KEY_MAX_CONNECTIONS_COUNT))
            {
                await configManager.SetConfigValueAsync(CONFIG_KEY_MAX_CONNECTIONS_COUNT, DEFAULT_MAX_CONNECTIONS_COUNT);
                maxConnectionsCount = DEFAULT_MAX_CONNECTIONS_COUNT;
            }
            else
            {
                maxConnectionsCount = await configManager.GetConfigValueUIntAsync(CONFIG_KEY_MAX_CONNECTIONS_COUNT);
            }
        }

        private void ConfigManager_ConfigEntryUpdated(object? sender, System.EventArgs e)
        {
            RetrieveConfig().FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void UserManager_UserUpdated(object? sender, Users.EventArgs.UserUpdatedEventArgs e)
        {
            Task.Run(() =>
            {
                foreach (RpcSessionContext context in sessionsList.Values.Where(c => c.Identity != null && c.Identity.Id == e.UpdatedUser.Id))
                {
                    context.Identity = e.UpdatedUser;
                    SessionIdentityChanged?.Invoke(this, new SessionEventArgs(context));
                }
            }).ExecuteWithinLock(sessionsListLock).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void UserManager_UserDeleted(object? sender, Users.EventArgs.UserDeletedEventArgs e)
        {
            Task.Run(() =>
            {
                foreach (RpcSessionContext context in sessionsList.Values.Where(c => c.Identity != null && c.Identity.Id == e.DeletedUser.Id))
                {
                    context.Identity = null;
                    SessionIdentityChanged?.Invoke(this, new SessionEventArgs(context));
                }
            }).ExecuteWithinLock(sessionsListLock).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void TcpListener_ClientConnected(SimpleSockets.Messaging.Metadata.IClientInfo clientInfo)
        {
            Task.Run(() =>
            {
                RpcSessionContext rpcSessionContext = new RpcSessionContext(transportConnectionId: clientInfo.Id);
                sessionsList.Add(clientInfo.Id, rpcSessionContext);
                SessionCreated?.Invoke(this, new SessionEventArgs(rpcSessionContext));
            }).ExecuteWithinLock(sessionsListLock).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void TcpListener_ClientDisconnected(SimpleSockets.Messaging.Metadata.IClientInfo client, SimpleSockets.DisconnectReason reason)
        {
            Task.Run(() =>
            {
                RpcSessionContext? rpcSessionContext;
                rpcSessionContext = sessionsList.GetValueOrDefault(client.Id);
                sessionsList.Remove(client.Id);

                if (rpcSessionContext != null)
                    SessionDestroyed?.Invoke(this, new SessionEventArgs(rpcSessionContext));
            }).ExecuteWithinLock(sessionsListLock).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void TcpListener_MessageReceived(SimpleSockets.Messaging.Metadata.IClientInfo client, string message)
        {
            Task.Run(async () =>
            {
                RpcSessionContext? rpcSessionContext = sessionsList.GetValueOrDefault(client.Id);
                if (rpcSessionContext == null)
                    throw new ArgumentNullException(nameof(rpcSessionContext));

                string response = await JsonRpcProcessor.Process(message, rpcSessionContext);;
                tcpListener?.SendMessage(client.Id, response);
            }).ExecuteWithinLock(sessionsListLock).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private static JsonRpcException OnRpcException(JsonRequest jsonRequest, JsonRpcException jsonRpcException)
        {
            Exception? exception = (jsonRpcException.data as Exception);
            if (exception == null)
                return new JsonRpcException(-32100, "Internal server error", null);

            return new JsonRpcException(jsonRpcException.code, exception.Message, exception.InnerException);
        }

        private static JsonRpcException? OnRpcPostProcess(JsonRequest jsonRequest, JsonResponse jsonResponse, object context)
        {
            // Unwrap tasks if necessary
            if (!(jsonResponse.Result is Task))
                return null;

            Task task = (Task)jsonResponse.Result;
            // Perform task checks
            if (task.IsFaulted)
                throw task.Exception ?? throw new Exception("Task ended prematurely.");
            if (task.IsCanceled)
                throw task.Exception ?? throw new TaskCanceledException();

            if (!task.IsCompleted)
                task.Wait(); // possible deadlock

            Type taskType = task.GetType();
            MethodInfo getResultInfo = taskType.GetMethod("get_Result") ?? throw new Exception("Failed to retrieve task result");
            object? result = getResultInfo.Invoke(task, null);
            if (result == null || result.GetType().FullName == "VoidTaskResult")
            {
                jsonResponse.Result = null;
                return null;
            }
            else
            {
                jsonResponse.Result = result;
                return null;
            }
        }

        public async Task<User?> AuthenticateSessionContextAsync(Guid contextId, string username, string password)
        {
            ThrowIfNotReady();

            RpcSessionContext? context;
            context = sessionsList.Values.FirstOrDefault(context => context.Id == contextId);
            if (context == null)
                return null;

            if (context.Identity != null)
                return context.Identity;

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User user = await userManager.AuthenticateUserAsync(username, password);
            context.Identity = user;
            return user;
        }

        public Task<bool> DisconnectSessionAsync(Guid contextId)
        {
            ThrowIfNotReady();

            RpcSessionContext? context;
            context = sessionsList.Values.FirstOrDefault(context => context.Id == contextId);
            if (context == null)
                return Task.FromResult(false);

            sessionsList.Remove(context.TransportConnectionId);
            SessionDestroyed?.Invoke(this, new SessionEventArgs(context));

            return Task.FromResult(true);
        }
    }
}

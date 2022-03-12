using NatManager.Server.Configuration;
using NatManager.Server.Database;
using NatManager.Server.Extensions;
using NatManager.Server.Logging;
using NatManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server
{
    public class Daemon : IDaemon
    {
        public const string CONFIG_KEY_BEHAVIOUR_MODE = "natmanagerd.daemon.behaviourMode";
        private const BehaviourMode DEFAULT_BEHAVIOUR_MODE = BehaviourMode.Enforcing;
        private BehaviourMode behaviourMode;

        private IDatabaseProvider databaseProvider;
        private ILogger logger;
        private IErrorHandler errorHandler;
        private ServiceState serviceState;
        protected List<IDaemonService> services = new List<IDaemonService>();
        protected SemaphoreSlim servicesLock = new SemaphoreSlim(1, 1);

        public ServiceState State { get { return serviceState; } }
        public BehaviourMode BehaviourMode { get { return behaviourMode; } }

        public Daemon(IDatabaseProvider databaseProvider, ILogger logger, IErrorHandler errorHandler)
        {
            this.databaseProvider = databaseProvider;
            this.logger = logger;
            this.errorHandler = errorHandler;
            serviceState = ServiceState.Stopped;
            ConfigManager configManager = new ConfigManager(this);
            services.Add(configManager);
        }

        public async Task StartAsync()
        {
            await StopAsync();

            try
            {
                try
                {
                    foreach (IDaemonService service in services.Where(service => service.State == ServiceState.Stopped))
                        await StartServiceAsync(service, true);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to start one of the services", ex);
                }

                await RetrieveConfig();
            }
            catch
            {
                await StopAsync();
                throw;
            }

            serviceState = ServiceState.Running;
            await logger.InfoAsync("Started daemon.");
        }

        public async Task StopAsync()
        {
            foreach (IDaemonService service in services.Where(service => service.State == ServiceState.Running))
            {
                try
                {
                    await StopServiceAsync(service, true);
                }
                catch (Exception ex)
                {
                    errorHandler.HandleException(new Exception("Failed to stop service of type " + service.GetType().Name, ex));
                }
            }

            serviceState = ServiceState.Stopped;
        }

        private async Task RetrieveConfig()
        {
            ConfigManager configManager = await GetServiceAsync<ConfigManager>();
            if (!await configManager.ConfigEntryExistsAsync(CONFIG_KEY_BEHAVIOUR_MODE))
            {
                await configManager.SetConfigValueAsync(CONFIG_KEY_BEHAVIOUR_MODE, (int)DEFAULT_BEHAVIOUR_MODE);
                behaviourMode = DEFAULT_BEHAVIOUR_MODE;
            }
            else
            {
                behaviourMode = (BehaviourMode)await configManager.GetConfigValueIntAsync(CONFIG_KEY_BEHAVIOUR_MODE);
            }
        }

        public IDatabaseProvider GetDatabaseProvider()
        {
            return databaseProvider;
        }

        public ILogger GetLogger()
        {
            return logger;
        }

        public IErrorHandler GetErrorHandler()
        {
            return errorHandler;
        }

        public void SetErrorHandler(IErrorHandler exceptionHandler)
        {
            this.errorHandler = exceptionHandler;
        }

        public void SetBehaviourMode(BehaviourMode newBehaviourMode, bool permanent)
        {
            behaviourMode = newBehaviourMode;

            if (permanent)
            {
                Task.Run(async () =>
                {
                    ConfigManager configManager = await GetServiceAsync<ConfigManager>();
                    await configManager.SetConfigValueAsync(CONFIG_KEY_BEHAVIOUR_MODE, (int)newBehaviourMode);
                }).FireAndForgetSafeAsync(errorHandler);
            }
        }

        public async Task AddServiceAsync(IDaemonService daemonService, bool startService = false)
        {
            await logger.InfoAsync($"Adding service {daemonService.GetType().Name}...");
            if (daemonService.GetType() == typeof(ConfigManager))
                throw new InvalidOperationException("Cannot add service of type " + typeof(ConfigManager).Name);

            try
            {
                await servicesLock.WaitAsync();
                if (services.Any(service => service.GetType() == daemonService.GetType()))
                    throw new InvalidOperationException("Service of this type has already been registered");

                services.Add(daemonService);
                daemonService.Daemon = this;
                await logger.InfoAsync("Service added.");
            }
            finally
            {
                servicesLock.Release();
            }
        }

        public async Task RemoveServiceAsync<T>() where T : IDaemonService
        {
            await logger.InfoAsync($"Removing service {typeof(T).Name}...");
            if (typeof(T) == typeof(ConfigManager))
                throw new InvalidOperationException("Cannot remove service of type " + typeof(ConfigManager).Name);

            try
            {
                await servicesLock.WaitAsync();
                IDaemonService? daemonService = services.FirstOrDefault(service => service.GetType() == typeof(T));
                if (daemonService == null)
                    throw new InvalidOperationException("Service of this type has not been registered");

                services.Remove(daemonService);
                daemonService.Daemon = null;
                await daemonService.StopAsync();
                await logger.InfoAsync("Service removed.");
            }
            finally
            {
                servicesLock.Release();
            }
        }

        public async Task<T> GetServiceAsync<T>() where T : IDaemonService
        {
            try
            {
                await servicesLock.WaitAsync();
                IDaemonService? daemonService = services.FirstOrDefault(service => service.GetType() == typeof(T));
                if (daemonService == null)
                    throw new InvalidOperationException("Requested service was not available");

                return (T)daemonService;
            }
            finally
            {
                servicesLock.Release();
            }
        }

        public async Task<bool> HasServiceAsync<T>() where T : IDaemonService
        {
            try
            {
                await servicesLock.WaitAsync();
                IDaemonService? daemonService = services.FirstOrDefault(service => service.GetType() == typeof(T));
                return daemonService != null;
            }
            finally
            {
                servicesLock.Release();
            }
        }

        public async Task StartServiceAsync<T>(bool force = false) where T : IDaemonService
        {
            IDaemonService? daemonService;
            try
            {
                await servicesLock.WaitAsync();
                daemonService = services.FirstOrDefault(service => service.GetType() == typeof(T));
                if (daemonService == null)
                    throw new InvalidOperationException("Service of this type has not been registered");
            }
            finally
            {
                servicesLock.Release();
            }

            await StartServiceAsync(daemonService, force);
        }

        public async Task StopServiceAsync<T>(bool force = false) where T : IDaemonService
        {
            IDaemonService? daemonService;
            try
            {
                await servicesLock.WaitAsync();
                daemonService = services.FirstOrDefault(service => service.GetType() == typeof(T));
                if (daemonService == null)
                    throw new InvalidOperationException("Service of this type has not been registered");
            }
            finally
            {
                servicesLock.Release();
            }

            await StopServiceAsync(daemonService, force);
        }

        public async Task StartServiceAsync(IDaemonService daemonService, bool force = false)
        {
            await logger.InfoAsync($"Starting service {daemonService.GetType().Name}...");

            if (daemonService.State == ServiceState.Running && !force)
                throw new InvalidOperationException("The service is already running.");

            await daemonService.StartAsync();
            if (daemonService.State != ServiceState.Running)
                throw new Exception($"Service {daemonService.GetType().Name} did not mark its status as \"{ServiceState.Running}\" yet it completed the init procedure");
            await logger.InfoAsync("Started service.");
        }

        public async Task StopServiceAsync(IDaemonService daemonService, bool force = false)
        {
            await logger.InfoAsync($"Stopping service {daemonService.GetType().Name}...");

            if (daemonService.State == ServiceState.Stopped && !force)
                throw new InvalidOperationException("The service is already stopped.");

            await daemonService.StopAsync();
            if (daemonService.State != ServiceState.Stopped)
                throw new Exception($"Service {daemonService.GetType().Name} did not mark its status as \"{ServiceState.Stopped}\" yet it completed the stopping procedure");
            await logger.InfoAsync($"Service stopped.");
        }
    }
}

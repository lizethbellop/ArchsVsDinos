using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public class WcfConnectionGuardian
    {
        private readonly Action<string, string> onError;
        private readonly ILogger logger;

        public bool IsServerAvailable { get; private set; } = true;
        public event EventHandler<ServerStateChangedEventArgs> ServerStateChanged;

        public WcfConnectionGuardian(Action<string, string> onError, ILogger logger = null)
        {
            this.onError = onError ?? throw new ArgumentNullException(nameof(onError));
            this.logger = logger ?? new Logger(); 
        }

        public async Task<bool> ExecuteAsync(Func<Task> operation, string operationName = "Operación")
        {
            try
            {
                await operation();
                UpdateServerState(true);
                return true;
            }
            catch (FaultException ex)
            {
                HandleWcfError(
                    Lang.WcfErrorService,
                    ex.Message,
                    ex,
                    operationName
                );
                return false;
            }
            catch (CommunicationException ex)
            {
                HandleWcfError(
                    Lang.WcfNoConnection,
                    string.Format(Lang.WcfErrorOperationFailed, operationName),
                    ex,
                    operationName
                );
                return false;
            }
            catch (TimeoutException ex)
            {
                HandleWcfError(
                    Lang.WcfErrorTimeout,
                    string.Format(Lang.WcfErrorOperationTimeout, operationName),
                    ex,
                    operationName
                );
                return false;
            }
            catch (Exception ex)
            {
                HandleWcfError(
                    Lang.WcfErrorUnexpected,
                    ex.Message,
                    ex,
                    operationName
                );
                return false;
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, T defaultValue = default(T), string operationName = "Operación")
        {
            try
            {
                var result = await operation();
                UpdateServerState(true);
                return result;
            }
            catch (FaultException ex)
            {
                HandleWcfError(
                    Lang.WcfErrorService,
                    ex.Message,
                    ex,
                    operationName
                );
                return defaultValue;
            }
            catch (CommunicationException ex)
            {
                HandleWcfError(
                    Lang.WcfNoConnection,
                    string.Format(Lang.WcfErrorOperationFailed, operationName),
                    ex,
                    operationName
                );
                return defaultValue;
            }
            catch (TimeoutException ex)
            {
                HandleWcfError(
                    Lang.WcfErrorTimeout,
                    string.Format(Lang.WcfErrorOperationTimeout, operationName),
                    ex,
                    operationName
                );
                return defaultValue;
            }
            catch (Exception ex)
            {
                HandleWcfError(
                    Lang.WcfErrorUnexpected,
                    ex.Message,
                    ex,
                    operationName
                );
                return defaultValue;
            }
        }

        public void MonitorClientState(ICommunicationObject client)
        {
            if (client == null) return;

            client.Faulted += (s, e) =>
            {
                UpdateServerState(false);
                logger.LogError($"WCF client faulted. State: {client.State}");
                onError(Lang.WcfErrorConnectionLost, Lang.WcfErrorConnectionLost);
            };

            client.Closed += (s, e) =>
            {
                UpdateServerState(false);
                logger.LogInfo($"WCF client closed. State: {client.State}");
            };
        }

        private void HandleWcfError(string title, string message, Exception ex, string operationName)
        {
            UpdateServerState(false);

            logger.LogError($"WCF Error in operation '{operationName}': {title} - {message}", ex);

            onError(title, message);
        }

        private void UpdateServerState(bool isAvailable)
        {
            if (IsServerAvailable != isAvailable)
            {
                IsServerAvailable = isAvailable;

                if (isAvailable)
                {
                    logger.LogInfo("Server connection restored");
                }
                else
                {
                    logger.LogWarning("Server connection lost");
                }

                ServerStateChanged?.Invoke(this, new ServerStateChangedEventArgs(isAvailable));
            }
        }

        public void RestoreNormalMode()
        {
            UpdateServerState(true);
        }
    }

    public class ServerStateChangedEventArgs : EventArgs
    {
        public bool IsAvailable { get; }
        public DateTime Timestamp { get; }

        public ServerStateChangedEventArgs(bool isAvailable)
        {
            IsAvailable = isAvailable;
            Timestamp = DateTime.Now;
        }
    }
}

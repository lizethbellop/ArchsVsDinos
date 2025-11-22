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
            catch (ObjectDisposedException ex)
            {
                logger.LogError($"[{operationName}] Cliente desechado: {ex.ObjectName}", ex);
                HandleWcfError(
                    Lang.WcfErrorConnectionLost,
                    Lang.WcfNoConnection,
                    ex,
                    operationName
                );
                return false;
            }
            catch (EndpointNotFoundException ex)
            {
                logger.LogError($"[{operationName}] Servidor no encontrado: {ex.Message}", ex);
                HandleWcfError(
                    Lang.WcfNoConnection,
                    "No se pudo conectar al servidor. Verifique que esté en ejecución.",
                    ex,
                    operationName
                );
                return false;
            }
            catch (FaultException ex)
            {
                logger.LogError($"[{operationName}] Error del servicio: {ex.Message}", ex);
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
                logger.LogError($"[{operationName}] Error de comunicación: {ex.Message}", ex);
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
                logger.LogError($"[{operationName}] Tiempo de espera agotado", ex);
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
                logger.LogError($"[{operationName}] Error inesperado ({ex.GetType().Name}): {ex.Message}", ex);
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
            catch (ObjectDisposedException ex)
            {
                logger.LogError($"[{operationName}] Cliente desechado: {ex.ObjectName}", ex);
                HandleWcfError(
                    Lang.WcfErrorConnectionLost,
                    Lang.WcfNoConnection,
                    ex,
                    operationName
                );
                return defaultValue;
            }
            catch (EndpointNotFoundException ex)
            {
                logger.LogError($"[{operationName}] Servidor no encontrado: {ex.Message}", ex);
                HandleWcfError(
                    Lang.WcfNoConnection,
                    "No se pudo conectar al servidor. Verifique que esté en ejecución.",
                    ex,
                    operationName
                );
                return defaultValue;
            }
            catch (FaultException ex)
            {
                logger.LogError($"[{operationName}] Error del servicio: {ex.Message}", ex);
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
                logger.LogError($"[{operationName}] Error de comunicación: {ex.Message}", ex);
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
                logger.LogError($"[{operationName}] Tiempo de espera agotado", ex);
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
                logger.LogError($"[{operationName}] Error inesperado ({ex.GetType().Name}): {ex.Message}", ex);
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

            string logMessage = $"WCF Error en '{operationName}': {title} - {message}";

            if (ex.InnerException != null)
            {
                logMessage += $" | Inner: {ex.InnerException.Message}";
            }

            logger.LogError(logMessage, ex);

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

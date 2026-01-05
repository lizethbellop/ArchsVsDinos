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

        private string lastErrorTitle;
        private string lastErrorMessage;

        private bool errorAlreadyReported = false;
        private bool operationInProgress = false;

        public bool IsServerAvailable { get; private set; } = true;

        public string LastErrorTitle => lastErrorTitle;
        public string LastErrorMessage => lastErrorMessage;

        public event EventHandler<ServerStateChangedEventArgs> ServerStateChanged;

        public WcfConnectionGuardian(Action<string, string> onError, ILogger logger = null)
        {
            this.onError = onError ?? throw new ArgumentNullException(nameof(onError));
            this.logger = logger ?? new Logger();
        }

        public async Task<bool> ExecuteAsync(Func<Task> operation, string operationName = "Operación")
        {
            operationInProgress = true;

            try
            {
                logger.LogDebug($"Executing operation: {operationName}");
                await operation();
                UpdateServerState(true);
                errorAlreadyReported = false;
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return false;
            }
            finally
            {
                operationInProgress = false;
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, T defaultValue = default(T), string operationName = "Operación")
        {
            operationInProgress = true;

            try
            {
                logger.LogDebug($"Executing operation: {operationName}");
                var result = await operation();
                UpdateServerState(true);
                errorAlreadyReported = false;
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return defaultValue;
            }
            finally
            {
                operationInProgress = false;
            }
        }

        public async Task<T> ExecuteWithThrowAsync<T>(Func<Task<T>> operation, string operationName = "Operación")
        {
            operationInProgress = true;

            try
            {
                logger.LogDebug($"Executing operation with trow: {operationName}");
                var result = await operation();
                UpdateServerState(true);
                errorAlreadyReported = false;
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                throw; 
            }
            finally
            {
                operationInProgress = false;
            }
        }

        public void MonitorClientState(ICommunicationObject client)
        {
            if (client == null) return;

            client.Faulted += (s, e) =>
            {
                UpdateServerState(false);
                logger.LogError($"⚠️ WCF client faulted. State: {client.State}");

                if (!operationInProgress && !errorAlreadyReported)
                {
                    errorAlreadyReported = true;
                    logger.LogWarning("🔶 [MONITOR] Disparando evento de error porque no hay operación activa");
                    onError(lastErrorTitle ?? "Error de conexión", lastErrorMessage ?? "El cliente WCF falló inesperadamente");
                }
                else
                {
                    logger.LogDebug("🔷 [MONITOR] No disparando evento - hay operación activa que lo manejará");
                }
            };

            client.Closed += (s, e) =>
            {
                UpdateServerState(false);
                logger.LogInfo($"WCF client closed. State: {client.State}");
            };
        }

        private void HandleException(Exception ex, string operationName, bool suppressErrors = false)
        {
            switch (ex)
            {
                case ObjectDisposedException ode:
                    logger.LogError($"[{operationName}] Cliente desechado: {ode.ObjectName}", ode);
                    HandleWcfError("Conexión perdida", "No hay conexión con el servidor", ode, operationName, suppressErrors);
                    break;

                case EndpointNotFoundException enfe:
                    logger.LogError($"[{operationName}] Servidor no encontrado: {enfe.Message}", enfe);
                    HandleWcfError("Conexión perdida", "No se pudo conectar al servidor. Verifique que esté en ejecución", enfe, operationName, suppressErrors);
                    break;

                case FaultException fe:
                    logger.LogError($"[{operationName}] Error del servicio: {fe.Message}", fe);
                    HandleWcfError("Error de servicio", fe.Message, fe, operationName, suppressErrors);
                    break;

                case CommunicationException ce:
                    logger.LogError($"[{operationName}] Error de comunicación: {ce.Message}", ce);
                    HandleWcfError("Conexión perdida", $"La operación '{operationName}' falló", ce, operationName, suppressErrors);
                    break;

                case TimeoutException te:
                    logger.LogError($"[{operationName}] Tiempo de espera agotado", te);
                    HandleWcfError("Timeout", $"Tiempo de espera agotado para '{operationName}'", te, operationName, suppressErrors);
                    break;

                default:
                    logger.LogError($"[{operationName}] Error inesperado ({ex.GetType().Name}): {ex.Message}", ex);
                    HandleWcfError("Error inesperado", ex.Message, ex, operationName, suppressErrors);
                    break;
            }
        }

        private void HandleWcfError(string title, string message, Exception ex, string operationName, bool suppressErrors = false)
        {
            UpdateServerState(false);

            lastErrorTitle = title;
            lastErrorMessage = message;

            string logMessage = $"WCF Error en '{operationName}': {title} - {message}";
            if (ex.InnerException != null)
                logMessage += $" | Inner: {ex.InnerException.Message}";

            logger.LogError(logMessage, ex);

            if (!suppressErrors && !errorAlreadyReported)
            {
                errorAlreadyReported = true;
                logger.LogDebug($"Disparando onError para '{operationName}'");
                onError?.Invoke(title, message);
            }
            else if (suppressErrors)
            {
                logger.LogDebug($"Error supprimed in '{operationName}'");
            }
        }

        private void UpdateServerState(bool isAvailable)
        {
            if (IsServerAvailable != isAvailable)
            {
                IsServerAvailable = isAvailable;

                if (isAvailable)
                    logger.LogInfo("Server connection restored");
                else
                    logger.LogWarning("Server connection lost");

                ServerStateChanged?.Invoke(this, new ServerStateChangedEventArgs(isAvailable));
            }
        }

        public void RestoreNormalMode()
        {
            UpdateServerState(true);
            errorAlreadyReported = false;
            operationInProgress = false;
        }

        public async Task<bool> ExecuteAsync(
            Func<Task> operation,
            string operationName,
            bool suppressErrors)
        {
            operationInProgress = true;

            try
            {
                logger.LogDebug($"Executing operation: {operationName}");
                await operation();
                UpdateServerState(true);
                errorAlreadyReported = false;
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, suppressErrors);
                return false;
            }
            finally
            {
                operationInProgress = false;
            }
        }

        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            T defaultValue,
            bool suppressErrors)
        {
            operationInProgress = true;

            try
            {
                logger.LogDebug($"Executing operation: {operationName}");
                var result = await operation();
                UpdateServerState(true);
                errorAlreadyReported = false;
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, suppressErrors);
                return defaultValue;
            }
            finally
            {
                operationInProgress = false;
            }
        }

    }

}

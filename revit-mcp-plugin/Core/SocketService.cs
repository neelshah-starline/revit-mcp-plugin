using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Models.JsonRPC;
using RevitMCPSDK.API.Interfaces;
using revit_mcp_plugin.Configuration;
using revit_mcp_plugin.Utils;

namespace revit_mcp_plugin.Core
{
    public class SocketService
    {
        private static SocketService _instance;
        private TcpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private int _port = 8080;
        private UIApplication _uiApp;
        private ICommandRegistry _commandRegistry;
        private ILogger _logger;
        private CommandExecutor _commandExecutor;

        public static SocketService Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new SocketService();
                return _instance;
            }
        }

        private SocketService()
        {
            _commandRegistry = new RevitCommandRegistry();
            _logger = new Logger();
        }

        public bool IsRunning => _isRunning;

        public int Port
        {
            get => _port;
            set => _port = value;
        }

        // Initialize the socket service
        public void Initialize(UIApplication uiApp)
        {
            _logger.Info("SocketService: Starting initialization...");

            try
            {
                _uiApp = uiApp;
                _logger.Info("SocketService: UIApplication assigned");

                // Initialize ExternalEventManager
                _logger.Info("SocketService: Initializing ExternalEventManager...");
                ExternalEventManager.Instance.Initialize(uiApp, _logger);
                _logger.Info("SocketService: ExternalEventManager initialized");

                // Get the current Revit version
                _logger.Info("SocketService: Getting Revit version...");
                var versionAdapter = new RevitMCPSDK.API.Utils.RevitVersionAdapter(_uiApp.Application);
                string currentVersion = versionAdapter.GetRevitVersion();
                _logger.Info($"Current Revit version: {currentVersion}");

                // Create CommandExecutor
                _logger.Info("SocketService: Creating CommandExecutor...");
                _commandExecutor = new CommandExecutor(_commandRegistry, _logger);
                _logger.Info("SocketService: CommandExecutor created");

                // Load configuration and register commands
                _logger.Info("SocketService: Loading configuration...");
                ConfigurationManager configManager = new ConfigurationManager(_logger);
                configManager.LoadConfiguration();
                _logger.Info("SocketService: Configuration loaded");

                //// Read the service port from the configuration
                //if (configManager.Config.Settings.Port > 0)
                //{
                //    _port = configManager.Config.Settings.Port;
                //}
                _port = 8080; // Hard-coded port number
                _logger.Info($"SocketService: Using port {_port}");

                // Load commands
                _logger.Info("SocketService: Loading commands...");
                CommandManager commandManager = new CommandManager(
                    _commandRegistry, _logger, configManager, _uiApp);
                commandManager.LoadCommands();
                _logger.Info("SocketService: Commands loaded");

                _logger.Info($"Socket service initialized successfully on port {_port}");
            }
            catch (Exception ex)
            {
                _logger.Error($"SocketService initialization failed: {ex.Message}");
                _logger.Error($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to let caller handle it
            }
        }

        public void Start()
        {
            if (_isRunning)
            {
                _logger.Info("SocketService: Service is already running");
                return;
            }

            // Try to start on the configured port, with fallback ports if needed
            int[] portsToTry = { _port, 8081, 8082, 8083, 8084, 8085 };

            foreach (int portToTry in portsToTry)
            {
                try
                {
                    _logger.Info($"SocketService: Attempting to start TCP listener on port {portToTry}...");

                    _isRunning = true;
                    _listener = new TcpListener(IPAddress.Any, portToTry);
                    _listener.Start();

                    _port = portToTry; // Update the actual port being used
                    _logger.Info($"SocketService: TCP listener started successfully on port {_port}");

                    _listenerThread = new Thread(ListenForClients)
                    {
                        IsBackground = true,
                        Name = "RevitMCP-SocketListener"
                    };
                    _listenerThread.Start();

                    _logger.Info("SocketService: Listener thread started");
                    return; // Success, exit the loop
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    _logger.Warning($"SocketService: Port {portToTry} is already in use, trying next port...");
                    _isRunning = false;
                    continue; // Try next port
                }
                catch (Exception ex)
                {
                    _logger.Error($"SocketService: Failed to start socket service on port {portToTry}: {ex.Message}");
                    _logger.Error($"Stack trace: {ex.StackTrace}");
                    _isRunning = false;
                    throw; // Re-throw for critical errors (not port conflicts)
                }
            }

            // If we get here, all ports failed
            _logger.Error("SocketService: Failed to start on any available port (8080-8085)");
            _isRunning = false;
            throw new Exception("Could not start socket service - all ports (8080-8085) are in use");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _isRunning = false;

                _listener?.Stop();
                _listener = null;

                if(_listenerThread!=null && _listenerThread.IsAlive)
                {
                    _listenerThread.Join(1000);
                }
            }
            catch (Exception)
            {
                // log error
            }
        }

        private void ListenForClients()
        {
            try
            {
                while (_isRunning)
                {
                    TcpClient client = _listener.AcceptTcpClient();

                    Thread clientThread = new Thread(HandleClientCommunication)
                    {
                        IsBackground = true
                    };
                    clientThread.Start(client);
                }
            }
            catch (SocketException)
            {
                
            }
            catch(Exception)
            {
                // log
            }
        }

        private void HandleClientCommunication(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream stream = tcpClient.GetStream();

            try
            {
                byte[] buffer = new byte[8192];

                while (_isRunning && tcpClient.Connected)
                {
                    // Read client messages
                    int bytesRead = 0;

                    try
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                    catch (IOException)
                    {
                        // Client disconnected
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    System.Diagnostics.Trace.WriteLine($"Received message: {message}");

                    string response = ProcessJsonRPCRequest(message);

                    // Send response
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch(Exception)
            {
                // log
            }
            finally
            {
                tcpClient.Close();
            }
        }

        private string ProcessJsonRPCRequest(string requestJson)
        {
            JsonRPCRequest request;

            try
            {
                // Parse JSON-RPC requests
                request = JsonConvert.DeserializeObject<JsonRPCRequest>(requestJson);

                // Verify that the request format is valid
                if (request == null || !request.IsValid())
                {
                    return CreateErrorResponse(
                        null,
                        JsonRPCErrorCodes.InvalidRequest,
                        "Invalid JSON-RPC request"
                    );
                }

                // Search for the command in the registry
                if (!_commandRegistry.TryGetCommand(request.Method, out var command))
                {
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.MethodNotFound,
                        $"Method '{request.Method}' not found");
                }

                // Execute command
                try
                {
                    object result = command.Execute(request.GetParamsObject(), request.Id);

                    return CreateSuccessResponse(request.Id, result);
                }
                catch (Exception ex)
                {
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.InternalError, ex.Message);
                }
            }
            catch (JsonException)
            {
                // JSON parsing error
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.ParseError,
                    "Invalid JSON"
                );
            }
            catch (Exception ex)
            {
                // Catch other errors produced when processing requests
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.InternalError,
                    $"Internal error: {ex.Message}"
                );
            }
        }

        private string CreateSuccessResponse(string id, object result)
        {
            var response = new JsonRPCSuccessResponse
            {
                Id = id,
                Result = result is JToken jToken ? jToken : JToken.FromObject(result)
            };

            return response.ToJson();
        }

        private string CreateErrorResponse(string id, int code, string message, object data = null)
        {
            var response = new JsonRPCErrorResponse
            {
                Id = id,
                Error = new JsonRPCError
                {
                    Code = code,
                    Message = message,
                    Data = data != null ? JToken.FromObject(data) : null
                }
            };

            return response.ToJson();
        }
    }
}

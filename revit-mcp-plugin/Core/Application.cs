using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.ApplicationServices;
using System.Reflection;
using System.Windows.Media.Imaging;



namespace revit_mcp_plugin.Core
{
    public class Application : IExternalApplication
    {
        // Keep references for adaptive UI
        private static PushButton _mcpToggleButton;
        private static UIApplication _uiApp;

        public Result OnStartup(UIControlledApplication application)
    {
        // Create ribbon panel for MCP controls
        RibbonPanel mcpPanel = application.CreateRibbonPanel("Revit MCP Plugin");

        // Toggle button to start/stop MCP service
        PushButtonData pushButtonData = new PushButtonData("ID_EXCMD_TOGGLE_REVIT_MCP", "Revit MCP\r\n Switch",
            Assembly.GetExecutingAssembly().Location, "revit_mcp_plugin.Core.MCPServiceConnection");
        pushButtonData.ToolTip = "Start / Stop MCP server";
        // Initial images: red (stopped) before ApplicationInitialized auto-start kicks in
        pushButtonData.Image = new BitmapImage(new Uri(@"U:\SW Python 3 Automations\RevitMCP\revit-mcp-plugin\revit-mcp-plugin\Core\Ressources\server_unconnected-16.png", UriKind.Absolute));
        pushButtonData.LargeImage = new BitmapImage(new Uri(@"U:\SW Python 3 Automations\RevitMCP\revit-mcp-plugin\revit-mcp-plugin\Core\Ressources\server_unconnected-32.png", UriKind.Absolute));
        pushButtonData.AvailabilityClassName = "revit_mcp_plugin.Core.MCPCommandAvailability";
        _mcpToggleButton = mcpPanel.AddItem(pushButtonData) as PushButton;

        // Settings button
        PushButtonData mcp_settings_pushButtonData = new PushButtonData("ID_EXCMD_MCP_SETTINGS", "Settings",
            Assembly.GetExecutingAssembly().Location, "revit_mcp_plugin.Core.Settings");
        mcp_settings_pushButtonData.ToolTip = "MCP Settings";
        mcp_settings_pushButtonData.Image = new BitmapImage(new Uri("/revit-mcp-plugin;component/Core/Ressources/settings-16.png", UriKind.RelativeOrAbsolute));
        mcp_settings_pushButtonData.LargeImage = new BitmapImage(new Uri("/revit-mcp-plugin;component/Core/Ressources/settings-32.png", UriKind.RelativeOrAbsolute));
        mcp_settings_pushButtonData.AvailabilityClassName = "revit_mcp_plugin.Core.MCPCommandAvailability";
        mcpPanel.AddItem(mcp_settings_pushButtonData);

        // Register for ApplicationInitialized event to auto-start MCP service
        // This fires after Revit initialization but before any document opens
        application.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;

        return Result.Succeeded;
    }

        private void OnApplicationInitialized(object sender, EventArgs e)
        {
            try
            {
                var app = (Autodesk.Revit.ApplicationServices.Application)sender;
                _uiApp = new UIApplication(app);

                // Initialize and AUTO-START your socket service
                SocketService.Instance.Initialize(_uiApp);
                SocketService.Instance.RunningStateChanged += OnRunningStateChanged;
                SocketService.Instance.Start();  // auto-start

                // Immediately reflect "Running" (green) in the button
                UpdateToggleButtonVisual(SocketService.Instance.IsRunning);

                // Optional: a non-intrusive message (comment out if you prefer silent)
                // TaskDialog.Show("MCP Plugin", "MCP service started successfully on port 8080!");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("MCP Plugin Error",
                    $"Failed to start MCP service: {ex.Message}\n\nCheck Revit journal for details.");
            }
        }

        // Event handler fired by the service whenever run/stop state changes
        private void OnRunningStateChanged(object sender, bool isRunning)
        {
            // Ensure UI changes occur on Revit's UI thread via Idling
            if (_uiApp != null)
            {
                EventHandler<IdlingEventArgs> idleOnce = null;
                idleOnce = (s, e) =>
                {
                    _uiApp.Idling -= idleOnce;
                    UpdateToggleButtonVisual(isRunning);
                };
                _uiApp.Idling += idleOnce;
            }
        }

        // Swap images/text based on service state
        private void UpdateToggleButtonVisual(bool isRunning)
        {
            if (_mcpToggleButton == null) return;

            string small = isRunning
                ? @"U:\SW Python 3 Automations\RevitMCP\revit-mcp-plugin\revit-mcp-plugin\Core\Ressources\server_connected-16.png"
                : @"U:\SW Python 3 Automations\RevitMCP\revit-mcp-plugin\revit-mcp-plugin\Core\Ressources\server_unconnected-16.png";

            string large = isRunning
                ? @"U:\SW Python 3 Automations\RevitMCP\revit-mcp-plugin\revit-mcp-plugin\Core\Ressources\server_connected-32.png"
                : @"U:\SW Python 3 Automations\RevitMCP\revit-mcp-plugin\revit-mcp-plugin\Core\Ressources\server_unconnected-32.png";

            _mcpToggleButton.Image = new BitmapImage(new Uri(small, UriKind.Absolute));
            _mcpToggleButton.LargeImage = new BitmapImage(new Uri(large, UriKind.Absolute));

            // Optional: adjust text & tooltip
            _mcpToggleButton.ItemText = isRunning ? "MCP (Running)" : "MCP (Stopped)";
            _mcpToggleButton.ToolTip = isRunning
                ? "MCP server is running. Click to stop."
                : "MCP server is stopped. Click to start.";
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;

            try
            {
                SocketService.Instance.RunningStateChanged -= OnRunningStateChanged;
                if (SocketService.Instance.IsRunning)
                    SocketService.Instance.Stop();
            }
            catch { /* swallow at shutdown to avoid blocking Revit */ }

            return Result.Succeeded;
        }
    }
}

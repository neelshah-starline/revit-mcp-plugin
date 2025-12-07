using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using System.Reflection;
using System.Windows.Media.Imaging;



namespace revit_mcp_plugin.Core
{
    public class Application : IExternalApplication
    {
    public Result OnStartup(UIControlledApplication application)
    {
        // Create ribbon panel for MCP controls
        RibbonPanel mcpPanel = application.CreateRibbonPanel("Revit MCP Plugin");

        // Toggle button to start/stop MCP service
        PushButtonData pushButtonData = new PushButtonData("ID_EXCMD_TOGGLE_REVIT_MCP", "Revit MCP\r\n Switch",
            Assembly.GetExecutingAssembly().Location, "revit_mcp_plugin.Core.MCPServiceConnection");
        pushButtonData.ToolTip = "Start / Stop MCP server";
        pushButtonData.Image = new BitmapImage(new Uri("/revit-mcp-plugin;component/Core/Ressources/icon-16.png", UriKind.RelativeOrAbsolute));
        pushButtonData.LargeImage = new BitmapImage(new Uri("/revit-mcp-plugin;component/Core/Ressources/icon-32.png", UriKind.RelativeOrAbsolute));
        pushButtonData.AvailabilityClassName = "revit_mcp_plugin.Core.MCPCommandAvailability";
        mcpPanel.AddItem(pushButtonData);

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
            System.Diagnostics.Trace.WriteLine("MCP Plugin: ApplicationInitialized event fired");

            try
            {
                // sender is DB-level Autodesk.Revit.ApplicationServices.Application
                var app = (Autodesk.Revit.ApplicationServices.Application)sender;
                var uiApp = new UIApplication(app);   // construct UIApplication in this context

                System.Diagnostics.Trace.WriteLine("MCP Plugin: UIApplication created, initializing socket service...");

                // Initialize any session services now (pre-document).
                SocketService.Instance.Initialize(uiApp);

                System.Diagnostics.Trace.WriteLine("MCP Plugin: Socket service initialized, starting service...");

                SocketService.Instance.Start();

                System.Diagnostics.Trace.WriteLine("MCP Plugin: Socket service started successfully");

                // Show success message
                TaskDialog.Show("MCP Plugin", "MCP service started successfully on port 8080!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"MCP Plugin: CRITICAL ERROR - Failed to start MCP service: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"MCP Plugin: Stack trace: {ex.StackTrace}");

                TaskDialog.Show("MCP Plugin Error",
                    $"Failed to start MCP service: {ex.Message}\n\nCheck Revit journal for details.");
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
            try
            {
                if (SocketService.Instance.IsRunning)
                    SocketService.Instance.Stop();
            }
            catch { /* swallow at shutdown to avoid blocking Revit */ }

            return Result.Succeeded;
        }
    }
}

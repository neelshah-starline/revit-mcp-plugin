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
            // Diagnostic: Confirm plugin loads
            //TaskDialog.Show("MCP Plugin", "Plugin loaded successfully!");

            RibbonPanel mcpPanel = application.CreateRibbonPanel("Revit MCP Plugin");

            PushButtonData pushButtonData = new PushButtonData("ID_EXCMD_TOGGLE_REVIT_MCP", "Revit MCP\r\n Switch",
                Assembly.GetExecutingAssembly().Location, "revit_mcp_plugin.Core.MCPServiceConnection");
            pushButtonData.ToolTip = "Open / Close mcp server";
            pushButtonData.Image = new BitmapImage(new Uri("/revit-mcp-plugin;component/Core/Ressources/icon-16.png", UriKind.RelativeOrAbsolute));
            pushButtonData.LargeImage = new BitmapImage(new Uri("/revit-mcp-plugin;component/Core/Ressources/icon-32.png", UriKind.RelativeOrAbsolute));
            pushButtonData.AvailabilityClassName = "revit_mcp_plugin.Core.MCPCommandAvailability";
            mcpPanel.AddItem(pushButtonData);

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
                // Diagnostic: Confirm ApplicationInitialized event fires
                //TaskDialog.Show("MCP Plugin", "ApplicationInitialized event fired - starting MCP service...");

                // sender is DB-level Autodesk.Revit.ApplicationServices.Application
                var app = (Autodesk.Revit.ApplicationServices.Application)sender;
                var uiApp = new UIApplication(app);   // construct UIApplication in this context

                // Initialize any session services now (pre-document).
                SocketService.Instance.Initialize(uiApp);
                SocketService.Instance.Start();

                // Confirm service started
                //TaskDialog.Show("MCP Plugin", "MCP service started successfully on port 8080!");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("MCP Plugin Error", $"Failed to start MCP service: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Failed to auto-start MCP service: {ex.Message}");
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

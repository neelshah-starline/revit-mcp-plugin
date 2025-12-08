using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace revit_mcp_plugin.Core
{
    /// <summary>
    /// Command availability class that allows MCP control even without open documents
    /// </summary>
    public class MCPCommandAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            // Always available - doesn't require document or selection
            return true;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class MCPServiceConnection : IExternalCommand
    {
        // Make command always available, even without open documents
        public string AvailabilityClassName => "revit_mcp_plugin.Core.MCPCommandAvailability";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Simple toggle: start if stopped, stop if running
                if (SocketService.Instance.IsRunning)
                    SocketService.Instance.Stop();
                else
                    SocketService.Instance.Start();

                // UI updates automatically via RunningStateChanged event
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

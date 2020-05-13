#region Namespaces
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace FamilyEditorInterface
{
    /// <summary>
    /// Implements the Revit add-in interface IExternalCommand
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        public static String global_message;

        public virtual Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (Application.Started) return Result.Succeeded;

            try
            {
                Application.App = commandData.Application;
                Application.Control.ShowForm();
                Application.Started = true;
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message += global_message;
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
    /// <summary>
    /// Implements the Revit add-in interface IExternalCommand
    /// Associates parameter between two families
    /// </summary>
    public class CommandAssociateParameters : IExternalCommand
    {
        public virtual Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Result.Succeeded;
        }
    }
    /// <summary>
    /// Implements the Revit add-in interface IExternalCommand
    /// Push parameters from the current family into a nested family
    /// </summary>
    public class CommandPushParameters : IExternalCommand
    {
        public virtual Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Result.Succeeded;
        }
    }
    /// <summary>
    /// Implements the Revit add-in interface IExternalCommand
    /// Pulls Parameters from a nested family into the current family
    /// </summary>
    public class CommandPullParameters : IExternalCommand
    {
        public virtual Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Result.Succeeded;
        }
    }
}

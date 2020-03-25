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

        public virtual Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            if (Application.thisApp.Started) return Result.Succeeded;

            try
            {
                Application.App = commandData.Application;
                Application.thisApp.ShowForm();
                Application.thisApp.Started = true;
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
}

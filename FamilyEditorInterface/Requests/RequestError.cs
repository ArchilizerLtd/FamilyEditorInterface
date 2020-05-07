using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.Requests
{
    public static class RequestError
    {
        public static string ErrorLog;

        public static void ReprotError()
        {
            TaskDialog.Show("Error", ErrorLog);
        }
    }
}

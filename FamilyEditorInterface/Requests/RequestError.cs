using Autodesk.Revit.UI;
using Dialog.Alerts;
using Dialog.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FamilyEditorInterface.WPF;

namespace FamilyEditorInterface.Requests
{
    public static class RequestError
    {
        public static List<Message> ErrorLog = new List<Message>();

        public static void ReprotError()
        {
            FamilyParameterViewModel.Alert("Alert", ErrorLog);
        }

        internal static void Reset()
        {
            ErrorLog = new List<Message>();
        }
    }
}

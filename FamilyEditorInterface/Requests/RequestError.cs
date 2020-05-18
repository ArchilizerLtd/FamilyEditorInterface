using Autodesk.Revit.UI;
using Dialog.Alerts;
using Dialog.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FamilyEditorInterface.WPF;
using FamilyEditorInterface.Dialog.Alerts;

namespace FamilyEditorInterface.Requests
{
    //A static class that handles user notifications
    public static class RequestError
    {
        /// <summary>
        /// Set the ErrorLog for this session
        /// The ErrorLog is a list of Messages (bold, body)
        /// </summary>
        public static List<Message> ErrorLog = new List<Message>();
        /// <summary>
        /// Set the NotifyLog for this session
        /// The NotifyLog is a string that can be added to
        /// </summary>
        public static string NotifyLog { get; internal set; }
        /// <summary>
        /// Will notify the user through the notification dialog box
        /// </summary>
        public static void Notify()
        {
            DialogUtils.Notify("Notify", NotifyLog);
        }
        /// <summary>
        /// Will alert the user through the alert dialog box
        /// </summary>
        public static void ReprotError()
        {
            DialogUtils.Alert("Alert", ErrorLog);
        }
        /// <summary>
        /// Resets all message logs
        /// </summary>
        internal static void Reset()
        {
            ErrorLog = new List<Message>();
            NotifyLog = "";
        }
    }
}

using Dialog.Alerts;
using Dialog.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.Dialog.Alerts
{
    public static class DialogUtils
    {
        #region Dialogs
        /// <summary>
        /// A Custom Alert DialogBox. Displays a Title and a list of Messages
        /// A single Message has a (text in) Bold and a (regular) Body
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="note">List of Messages List<Message></param>
        public static void Alert(string title, List<Message> note)
        {
            var dialog = new AlertDialogViewModel(title, note);
            var result = new DialogService().OpenDialog(dialog);
        }
        /// <summary>
        /// A Custom Alert DialogBox that displays a Title and a single Message string
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="message">Message string</param>
        public static void Alert(string title, string message)
        {
            var dialog = new AlertDialogViewModel(title, message);
            var result = new DialogService().OpenDialog(dialog);
        }
        /// <summary>
        /// A notification DialogBox. Displays a Title and a single Message string
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="message">Message string</param>
        public static void Notify(string title, string message)
        {
            var dialog = new NotifyDialogViewModel(title, message);
            var result = new DialogService().OpenDialog(dialog);
        }
        /// <summary>
        /// A confirmation DialogBox. Displays a Title and a single Message string
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="message">Message string</param>
        public static void OK(string title, string message)
        {
            var dialog = new OKDialogViewModel(title, message);
            var result = new DialogService().OpenDialog(dialog);
        }
        /// <summary>
        /// A failure DialogBox. Displays a Title and a single Message string
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="message">Message string</param>
        public static void Failure(string title, string message)
        {
            var dialog = new FailureDialogViewModel(title, message);
            var result = new DialogService().OpenDialog(dialog);
        }
        #endregion
    }
}

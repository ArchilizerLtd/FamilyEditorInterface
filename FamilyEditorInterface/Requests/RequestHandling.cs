using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.Requests
{
    public static class RequestHandling
    {
        #region Request Handling
        /// <summary>
        /// Toggle Type to Instance and vica verse 
        /// </summary>
        public static void MakeRequest(RequestId request, string name, string type)
        {
            Application.Control.handler.Request.TypeToInstance(new List<string>() { name });
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        /// <summary>
        /// Rename Parameter
        /// </summary>
        public static void MakeRequest(RequestId request, Tuple<string, string> renameValue)
        {
            Application.Control.handler.Request.RenameValue(new List<Tuple<string, string>>() { renameValue });
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        /// <summary>
        /// Change Parameter Value of Multiple
        /// </summary>
        public static void MakeRequest(RequestId request, List<Tuple<string, double>> values)
        {
            Application.Control.handler.Request.Value(values);
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        /// <summary>
        /// Delete Parameter
        /// </summary>
        public static void MakeRequest(RequestId request, string deleteValue)
        {
            Application.Control.handler.Request.DeleteValue(new List<string>() { deleteValue });
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        /// <summary>
        /// Change Parameter Value
        /// </summary>
        public static void MakeRequest(RequestId request, Tuple<string, double> value)
        {
            Application.Control.handler.Request.Value(new List<Tuple<string, double>>() { value });
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        /// <summary>
        /// Change all values
        /// </summary>
        public static void MakeRequest(RequestId request, List<Tuple<string, string, double>> value)
        {
            Application.Control.handler.Request.AllValues(value);
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        #endregion
    }
}

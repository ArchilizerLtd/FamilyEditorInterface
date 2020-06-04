using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dialog.Alerts;
using Dialog.Service;
using FamilyEditorInterface.Requests;

namespace FamilyEditorInterface
{
    /// <summary>
    /// A class with methods to execute requests made by the dialog user.
    /// </summary>
    /// 
    public class RequestHandler : IExternalEventHandler
    {
        // delegate that will expect a family parameter to act upon
        private delegate void FamilyOperation(FamilyManager fm, FamilyParameter fp);
        // The value of the latest request made by the modeless form 
        private Request m_request = new Request();
        // Encountered Error, notify the View Model to roll back (update)
        public event EventHandler EncounteredError;                
        /// <summary>
        /// A public property to access the current request value
        /// </summary>
        public Request Request
        {
            get { return m_request; }
        }


        /// <summary>
        ///   A method to identify this External Event Handler
        /// </summary>
        public String GetName()
        {
            return "Family Interface";
        }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                switch (Request.Take())
                {
                    case RequestId.None:
                        {
                            return;
                        }
                    case RequestId.ChangeParam:
                        {
                            RequestExecute.ExecuteParameterChange(uiapp, "Change Parameter Value", Request.GetValue());
                            break;
                        }
                    case RequestId.DeleteId:
                        {
                            RequestExecute.ExecuteParameterChange(uiapp, "Delete Parameter", Request.GetDeleteValue());
                            break;
                        }
                    case RequestId.ChangeParamName:
                        {
                            RequestExecute.ExecuteParameterChange(uiapp, "Change Parameter Name", Request.GetRenameValue());
                            break;
                        }
                    case RequestId.RestoreAll:
                        {
                            RequestExecute.ExecuteParameterChange(uiapp, "Restore All", Request.GetAllValues());
                            break;
                        }
                    case RequestId.TypeToInstance:
                        {
                            RequestExecute.ExecuteParameterChange(uiapp, "Type To Instance", Request.GetTypeToInstanceValue(), "type or instance");
                            break;
                        }
                    case RequestId.ShuffleParam:
                        {
                            RequestExecute.ExecuteParameterChange(uiapp, "Shuffle Parameters", Request.GetShuffleValue());
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch(Exception ex)
            {
                if (EncounteredError != null)
                {
                    EncounteredError(this, null);
                    RequestError.ErrorLog.Add(new Message("Operation failed.", ex.Message));
                }
            }
            finally
            {
                if (RequestError.ErrorLog.Count > 0)
                {
                    RequestError.ReprotError();
                }
                if(!String.IsNullOrEmpty(RequestError.NotifyLog))
                {
                    RequestError.Notify();
                }
                RequestError.Reset();
            }

            return;
        }

    }

}

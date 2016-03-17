using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

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
                    case RequestId.SlideFirstParam:
                        {
                            ExecuteParameterChange(uiapp, "Slide First Parameter", Request.GetValue());
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            finally
            {
                Application.thisApp.WakeFormUp();
            }

            return;
        }

        /// <summary>
        ///   The main door-modification subroutine - called from every request 
        /// </summary>
        /// <remarks>
        ///   It searches the current selection for all doors
        ///   and if it finds any it applies the requested operation to them
        /// </remarks>
        /// <param name="uiapp">The Revit application object</param>
        /// <param name="text">Caption of the transaction for the operation.</param>
        /// <param name="operation">A delegate to perform the operation on an instance of a door.</param>
        /// 
        private void ExecuteParameterChange(UIApplication uiapp, String text, double value)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (!doc.IsFamilyDocument)
            {
                Command.global_message =
                  "Please run this command in a family document.";

                TaskDialog.Show("Message", Command.global_message);
            }

            if ((uidoc != null))
            {
                FamilyManager mgr = doc.FamilyManager;
                FamilyParameter fp = mgr.get_Parameter("Height");
                // Since we'll modify the document, we need a transaction
                // It's best if a transaction is scoped by a 'using' block
                using (Transaction trans = new Transaction(uidoc.Document))
                {
                    // The name of the transaction was given as an argument

                    if (trans.Start(text) == TransactionStatus.Started)
                    {
                        mgr.Set(fp, value);
                        //operation(mgr, fp);

                        trans.Commit();
                    }
                }
            }
        }
        /// <summary>
        /// Delegate - not used (partly because I don't know how :D)
        /// </summary>
        /// <param name="fm"></param>
        /// <param name="fp"></param>
        private static void Slide(FamilyManager fm, FamilyParameter fp)
        {
            TaskDialog.Show("Message", String.Format("Executing .. {0} for value {1}", fp.Definition.Name, "empty"));
            if(fp.UserModifiable)
            {
                fm.Set(fp, "200");
            }
        }

    }
}

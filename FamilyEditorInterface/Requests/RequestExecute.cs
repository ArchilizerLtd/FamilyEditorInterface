using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dialog.Service;
using FamilyEditorInterface.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface
{
    public class RequestExecute
    {

        #region Execute
        /// <summary>
        /// Restore all values
        /// </summary>
        /// <param name="uiapp"></param>
        /// <param name="text"></param>
        /// <param name="values"></param>
        public static void ExecuteParameterChange(UIApplication uiapp, String text, List<Tuple<string, string, double>> values)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (!doc.IsFamilyDocument)
            {
                Command.global_message = "Please run this command in a family document.";
                TaskDialog.Show("Message", Command.global_message);
            }

            if ((uidoc != null))
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "Parameter Change"))
                {
                    tg.Start();
                    foreach (var value in values)
                    {
                        using (Transaction trans = new Transaction(uidoc.Document))
                        {
                            FailureHandlingOptions failureHandlingOptions = trans.GetFailureHandlingOptions();
                            FailureHandler failureHandler = new FailureHandler();
                            failureHandlingOptions.SetFailuresPreprocessor(failureHandler);
                            failureHandlingOptions.SetClearAfterRollback(true);
                            trans.SetFailureHandlingOptions(failureHandlingOptions);

                            FamilyManager mgr = doc.FamilyManager;
                            FamilyParameter fp = mgr.get_Parameter(value.Item1);
                            // Since we'll modify the document, we need a transaction
                            // It's best if a transaction is scoped by a 'using' block
                            // The name of the transaction was given as an argument
                            if (trans.Start(text) == TransactionStatus.Started)
                            {
                                mgr.Set(fp, value.Item3);
                                //operation(mgr, fp);
                                doc.Regenerate();
                                if (!value.Item1.Equals(value.Item2)) mgr.RenameParameter(fp, value.Item2);
                                trans.Commit();
                                uidoc.RefreshActiveView();
                            }
                            if (failureHandler.ErrorMessage != "")
                            {
                                RequestError.ErrorLog.Add(new Message(fp.Definition.Name, failureHandler.ErrorMessage));
                            }
                        }
                    }
                    tg.Assimilate();
                }
            }
        }
        /// <summary>
        /// Change Parameter
        /// </summary>
        /// <param name="uiapp"></param>
        /// <param name="text"></param>
        /// <param name="values"></param>
        public static void ExecuteParameterChange(UIApplication uiapp, String text, Tuple<string, double> value)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (!doc.IsFamilyDocument)
            {
                Command.global_message = "Please run this command in a family document.";
                TaskDialog.Show("Message", Command.global_message);
            }

            if ((uidoc != null))
            {
                using (Transaction trans = new Transaction(uidoc.Document, "Change Parameter Value"))
                {
                    FailureHandlingOptions failureHandlingOptions = trans.GetFailureHandlingOptions();
                    FailureHandler failureHandler = new FailureHandler();
                    failureHandlingOptions.SetFailuresPreprocessor(failureHandler);
                    failureHandlingOptions.SetClearAfterRollback(true);
                    trans.SetFailureHandlingOptions(failureHandlingOptions);

                    FamilyManager mgr = doc.FamilyManager;
                    FamilyParameter fp = mgr.get_Parameter(value.Item1);
                    // Since we'll modify the document, we need a transaction
                    // It's best if a transaction is scoped by a 'using' block
                    // The name of the transaction was given as an argument
                    if (trans.Start(text) == TransactionStatus.Started)
                    {
                        if (fp.IsDeterminedByFormula || fp.IsReporting)
                        {
                            trans.RollBack();  //Cannot change parameters driven by formulas, cannot change reporting parameters
                            return;
                        }

                        mgr.Set(fp, value.Item2);
                        doc.Regenerate();
                        trans.Commit();
                        uidoc.RefreshActiveView();
                        if (failureHandler.ErrorMessage != "")
                        {
                            RequestError.ErrorLog.Add(new Message(fp.Definition.Name, failureHandler.ErrorMessage));
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Shuffles all parameter values 
        /// </summary>
        /// <param name="uiapp">The Revit application object</param>
        /// <param name="text">Caption of the transaction for the operation.</param>
        /// <param name="operation">A delegate to perform the operation on an instance of a door.</param>
        /// 
        public static void ExecuteParameterChange(UIApplication uiapp, String text, List<Tuple<string, double>> values)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (!doc.IsFamilyDocument)
            {
                Command.global_message = "Please run this command in a family document.";
                TaskDialog.Show("Message", Command.global_message);
            }

            if ((uidoc != null))
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "Parameter Change"))
                {
                    tg.Start();
                    foreach (var value in values)
                    {
                        using (Transaction trans = new Transaction(uidoc.Document))
                        {
                            FailureHandlingOptions failureHandlingOptions = trans.GetFailureHandlingOptions();
                            FailureHandler failureHandler = new FailureHandler();
                            failureHandlingOptions.SetFailuresPreprocessor(failureHandler);
                            failureHandlingOptions.SetClearAfterRollback(true);
                            trans.SetFailureHandlingOptions(failureHandlingOptions);

                            FamilyManager mgr = doc.FamilyManager;
                            FamilyParameter fp = mgr.get_Parameter(value.Item1);
                            // Since we'll modify the document, we need a transaction
                            // It's best if a transaction is scoped by a 'using' block
                            // The name of the transaction was given as an argument
                            if (trans.Start(text) == TransactionStatus.Started)
                            {
                                if (fp.IsDeterminedByFormula) continue;  //Cannot change parameters driven by formulas
                                if (fp.IsReporting) continue;    //Cannot change reporting parameters
                                mgr.Set(fp, value.Item2);
                                doc.Regenerate();
                                trans.Commit();
                                uidoc.RefreshActiveView();
                                if (failureHandler.ErrorMessage != "")
                                {
                                    RequestError.ErrorLog.Add(new Message(fp.Definition.Name, failureHandler.ErrorMessage));                                 
                                }
                                else
                                {
                                    RequestError.NotifyLog += $"{fp.Definition.Name} was shuffled.{Environment.NewLine}";
                                }
                            }
                        }
                    }
                    tg.Assimilate();
                    if (!String.IsNullOrEmpty(RequestError.NotifyLog))
                    {
                        var lines = RequestError.NotifyLog.Split('\n').Length - 1;
                        RequestError.NotifyLog += $"{Environment.NewLine}{lines.ToString()} parameters have been processed sucessfully.";
                    }
                }
            }
        }
        /// <summary>
        /// Type to instance change
        /// </summary>
        /// <param name="uiapp"></param>
        /// <param name="text"></param>
        /// <param name="values"></param>
        public static void ExecuteParameterChange(UIApplication uiapp, String text, List<string> values, string type)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (!doc.IsFamilyDocument)
            {
                Command.global_message = "Please run this command in a family document.";
                TaskDialog.Show("Message", Command.global_message);
            }

            if ((uidoc != null))
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "Parameter Type To Instance"))
                {
                    tg.Start();
                    using (Transaction trans = new Transaction(uidoc.Document))
                    {
                        FailureHandlingOptions failureHandlingOptions = trans.GetFailureHandlingOptions();
                        FailureHandler failureHandler = new FailureHandler();
                        failureHandlingOptions.SetFailuresPreprocessor(failureHandler);
                        failureHandlingOptions.SetClearAfterRollback(true);
                        trans.SetFailureHandlingOptions(failureHandlingOptions);
                        // Since we'll modify the document, we need a transaction
                        // It's best if a transaction is scoped by a 'using' block
                        // The name of the transaction was given as an argument
                        if (trans.Start(text) == TransactionStatus.Started)
                        {
                            FamilyManager mgr = doc.FamilyManager;
                            foreach (var value in values)
                            {
                                FamilyParameter fp = mgr.get_Parameter(value);
                                if (fp.IsInstance)
                                {
                                    mgr.MakeType(fp);
                                }
                                else
                                {
                                    mgr.MakeInstance(fp);
                                };
                            }
                        }
                        doc.Regenerate();
                        trans.Commit();
                        uidoc.RefreshActiveView();
                        if (failureHandler.ErrorMessage != "")
                        {
                            RequestError.ErrorLog.Add(new Message("", failureHandler.ErrorMessage));
                        }
                    }
                    tg.Assimilate();
                }
            }
        }
        /// <summary>
        /// Delete parameter
        /// </summary>
        /// <param name="uiapp"></param>
        /// <param name="text"></param>
        /// <param name="values"></param>
        public static void ExecuteParameterChange(UIApplication uiapp, String text, List<string> values)
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
                using (TransactionGroup tg = new TransactionGroup(doc, "Parameter Delete"))
                {
                    tg.Start();
                    using (Transaction trans = new Transaction(uidoc.Document))
                    {
                        FailureHandlingOptions failureHandlingOptions = trans.GetFailureHandlingOptions();
                        FailureHandler failureHandler = new FailureHandler();
                        failureHandlingOptions.SetFailuresPreprocessor(failureHandler);
                        failureHandlingOptions.SetClearAfterRollback(true);
                        trans.SetFailureHandlingOptions(failureHandlingOptions);
                        // Since we'll modify the document, we need a transaction
                        // It's best if a transaction is scoped by a 'using' block
                        // The name of the transaction was given as an argument
                        if (trans.Start(text) == TransactionStatus.Started)
                        {
                            FamilyManager mgr = doc.FamilyManager;
                            foreach (var value in values)
                            {
                                FamilyParameter fp = mgr.get_Parameter(value);
                                mgr.RemoveParameter(fp);
                            }
                        }
                        doc.Regenerate();
                        trans.Commit();
                        uidoc.RefreshActiveView();
                        if (failureHandler.ErrorMessage != "")
                        {
                            RequestError.ErrorLog.Add(new Message("", failureHandler.ErrorMessage));
                        }
                        else
                        {
                            RequestError.NotifyLog += $"Successfully purged {values.Count.ToString()} unused parameters.";
                        }
                    }
                    tg.Assimilate();
                }
            }
        }
        /// <summary>
        /// Rename parameter
        /// </summary>
        /// <param name="uiapp"></param>
        /// <param name="text"></param>
        /// <param name="values"></param>
        public static void ExecuteParameterChange(UIApplication uiapp, String text, List<Tuple<string, string>> values)
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
                using (Transaction trans = new Transaction(uidoc.Document, "Parameter Name Changed"))
                {
                    FailureHandlingOptions failureHandlingOptions = trans.GetFailureHandlingOptions();
                    FailureHandler failureHandler = new FailureHandler();
                    failureHandlingOptions.SetFailuresPreprocessor(failureHandler);
                    failureHandlingOptions.SetClearAfterRollback(true);
                    trans.SetFailureHandlingOptions(failureHandlingOptions);

                    // Since we'll modify the document, we need a transaction
                    // It's best if a transaction is scoped by a 'using' block
                    // The name of the transaction was given as an argument
                    if (trans.Start(text) == TransactionStatus.Started)
                    {
                        FamilyManager mgr = doc.FamilyManager;
                        foreach (var value in values)
                        {
                            if (value.Item1.Equals(value.Item2)) continue;
                            FamilyParameter fp = mgr.get_Parameter(value.Item1);
                            mgr.RenameParameter(fp, value.Item2);
                        }
                    }
                    doc.Regenerate();
                    trans.Commit();
                    uidoc.RefreshActiveView();
                    if (failureHandler.ErrorMessage != "")
                    {
                        RequestError.ErrorLog.Add(new Message("", failureHandler.ErrorMessage));
                    }
                }
            }
        }
        #endregion

    }

    #region Failure Handler
    public class FailureHandler : IFailuresPreprocessor
    {
        public string ErrorMessage { set; get; }
        public string ErrorSeverity { set; get; }

        public FailureHandler()
        {
            ErrorMessage = "";
            ErrorSeverity = "";
        }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failureMessages = failuresAccessor.GetFailureMessages();

            foreach (FailureMessageAccessor failureMessageAccessor in failureMessages)
            {
                // We're just deleting all of the warning level 
                // failures and rolling back any others
                FailureDefinitionId id = failureMessageAccessor.GetFailureDefinitionId();

                try
                {
                    ErrorMessage = failureMessageAccessor.GetDescriptionText();
                }
                catch
                {
                    ErrorMessage = "Unknown Error";
                }

                try
                {
                    FailureSeverity failureSeverity = failureMessageAccessor.GetSeverity();

                    ErrorSeverity = failureSeverity.ToString();

                    if (failureSeverity == FailureSeverity.Warning)
                    {
                        failuresAccessor.DeleteWarning(failureMessageAccessor);
                    }
                    else
                    {
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                }
                catch
                {
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
    #endregion


}

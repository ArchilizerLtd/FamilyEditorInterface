using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using FamilyEditorInterface.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamilyEditorInterface.Resources.WPF.ViewModel
{
    public class AppControl
    {
        private FamilyParameterViewModel _presenter;
        private Document _document;
        private bool _disabled;

        // windows Revit handle
        static WindowHandle _hWndRevit = null;
        public RequestHandler handler;
        public ExternalEvent exEvent;


        #region Form
        //Show the UI
        public void Show(ViewActivatedEventArgs e)
        {
            if (_presenter == null)
            {
                ShowForm();
                return;
            }
            CheckDoc(e);
        }
        //De-facto the command is here. Will either be started on start (duh) or activated when switched back to a Family Document
        public void ShowForm()
        {
            GetRevitHandle();

            _presenter = null;
            if (_presenter == null || _presenter._isClosed)
            {
                try
                {
                    if (!Application.App.ActiveUIDocument.Document.IsFamilyDocument)
                    {
                        TaskDialog.Show("Error", "Usable only in Family Documents");
                        return;
                    }
                    
                    handler = new RequestHandler(); //new handler                    
                    exEvent = ExternalEvent.Create(handler);    //new event

                    _document = Application.App.ActiveUIDocument.Document;  //set current document
                    _presenter = new FamilyParameterViewModel(_document);
                    
                    _presenter.Show(_hWndRevit);    //pass parent (Revit) thread here
                    _presenter.PresenterClosed += Stop;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", ex.Message);
                    _presenter.Dispose();
                    _presenter = null;
                }
            }
        }
        //Stop FamilyEditorInterface execution
        private void Stop(object sender, EventArgs e)
        {
            Application.Started = false;
        }
        //On ShutDown
        internal void Close()
        {
            if (_presenter != null)
            {
                _presenter.Close();
            }
        }
        //Check if the Document is a FamilyDocument
        private void CheckDoc(ViewActivatedEventArgs e)
        {
            Document doc = e.CurrentActiveView.Document;

            // If the document is a Family Document, disable the UI
            if (!doc.IsFamilyDocument)
            {
                if (!_disabled)
                {
                    _presenter.Disable();
                    _disabled = true;
                }
                return;
            }
            else
            {
                if (_disabled)
                {
                    _presenter.Enable();
                    _disabled = false;
                }
            }
            if (_document.Title != doc.Title)
            {
                _document = doc;
                _presenter.Document = doc;
                _presenter.DocumentSwitched();
            }
        }
        //On DocumentChanged
        internal void DocumentChanged(Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
        {
            List<string> commands = new List<string>()
            {
                "param",
                "Modify element attributes",
                "Family Types",
                "Add Parameter",
                "Delete Selection",
                "Delete Parameter",
                "Parameter Delete",
                "Parameter Change",
                "Change Parameter Name",
                "Project Units"
            };
            if (_presenter != null && _presenter._enabled)
            {
                IList<String> operations = e.GetTransactionNames();
                if (commands.Any(operations.Contains))
                {
                    _presenter.ThisDocumentChanged();
                }
            }
        }
        //On DocumentClosed
        internal void DocumentClosed()
        {
            if (Application.App.ActiveUIDocument == null && Application.Started && _presenter != null)
            {
                _presenter.Close();
                _presenter = null;
            }
        }
        //Get Revit Handle
        private void GetRevitHandle()
        {
            // get the isntance of Revit Thread
            // to pass it to the Windows Form later
            if (null == _hWndRevit)
            {
                Process process = Process.GetCurrentProcess();

                IntPtr h = process.MainWindowHandle;
                _hWndRevit = new WindowHandle(h);
            }
        }
        #endregion
    }
}

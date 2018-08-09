// Copyright notice

// Archilizer Family Interface
// to be used in conjunction with Revit Family Editor
// Femily Editor Interface is ease of use type of plug-in -
// it main purpose is to make the process of creating and editting
// of Revit families smoother and more pleasent (less time consuming too)

#region Namespaces
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System.Windows.Forms;
using System.Diagnostics;
using FamilyEditorInterface.WPF;
using Autodesk.Revit.DB.Events;
#endregion

namespace FamilyEditorInterface
{   
    /// <summary>
    /// Implements the Revit add-in interface IExternalApplication
    /// </summary>
    class Application : IExternalApplication
    {
        // windows Revit handle
        static WindowHandle _hWndRevit = null;

        public static RequestHandler handler;
        public static ExternalEvent exEvent;
        
        public static UIApplication App;

        //public static bool Update;

        // class instance
        internal static Application thisApp = null;

        /// <summary>
        /// Tooltip
        /// </summary>
        public const string Message = "Interactive interface for Family Editor.";

        private FamilyParameterViewModel _presenter;
        private Document _document;
        private bool _started;

        /// <summary>
        /// Get absolute path to this assembly
        /// </summary>
        static string path = Assembly.GetExecutingAssembly().Location;
        static string contentPath = Path.GetDirectoryName(Path.GetDirectoryName(path)) + "/";
        static string helpFile = "file:///C:/ProgramData/Autodesk/ApplicationPlugins/FamilyEditorInterface.bundle/Content/Family%20Editor%20Interface%20_%20Revit%20_%20Autodesk%20App%20Store.html";
        static string largeIcon = contentPath + "family_editor_interface.png";
        static string smallIcon = contentPath + "familyeditorinterface16.png";
        private bool _disabled;

        // Marks if the Plugin has been started
        public bool Started 
        {
            get { return _started; }
            set
            {
                _started = value;
            }
        }
        
        #region Ribbon
        /// <summary>
        /// Use embedded image to load as an icon for the ribbon
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static private BitmapSource GetEmbeddedImage(string name)
        {
            try
            {
                Assembly a = Assembly.GetExecutingAssembly();
                Stream s = a.GetManifestResourceStream(name);
                return BitmapFrame.Create(s);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Add ribbon panel 
        /// </summary>
        /// <param name="application"></param>
        private void AddRibbonPanel(UIControlledApplication application)
        {
            // Create a custom ribbon panel or use the existing one
            String tabName = "Archilizer";
            String panelName = "Family Document";
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { }

            List<RibbonPanel> panels = application.GetRibbonPanels();
            RibbonPanel rvtRibbonPanel = null;
            // Pick the correct panel
            if (panels.FirstOrDefault(x => x.Name.Equals(panelName, StringComparison.OrdinalIgnoreCase)) == null)
            {
                rvtRibbonPanel = application.CreateRibbonPanel(tabName, panelName); 
            }
            else
            {
                rvtRibbonPanel = panels.FirstOrDefault(x => x.Name.Equals(panelName, StringComparison.OrdinalIgnoreCase)) as RibbonPanel;
            }
            //PulldownButtonData data = new PulldownButtonData("Options", "Family Editor" + Environment.NewLine + "Interface");


            // Get dll assembly path
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            //BitmapSource img32 = new BitmapImage (new Uri (@largeIcon));
            //BitmapSource img16 = new BitmapImage (new Uri (@smallIcon));

            //RibbonItem item = rvtRibbonPanel.AddItem(data);
            //PushButton optionsBtn = item as PushButton;
            //ContextualHelp ch = new ContextualHelp(ContextualHelpType.Url, "file:///C:/Users/adicted/AppData/Roaming/Autodesk/Revit/Addins/2015/Family Editor Interface _ AutoCAD _ Autodesk App Store.html");
            ContextualHelp ch = new ContextualHelp(ContextualHelpType.Url, @helpFile);
            
            //PushButton familyEI = rvtRibbonPanel.AddItem(new PushButtonData("Family Editor", "Family Editor" + Environment.NewLine +  "Interface", path,
            //    "FamilyEditorInterface.Command")) as PushButton;


            CreatePushButton(rvtRibbonPanel, String.Format("Family Editor" + Environment.NewLine + "Interface"), thisAssemblyPath, "FamilyEditorInterface.Command",
                Message, "family_editor_interface.png", ch);

            //familyEI.Image = img16;
            //familyEI.LargeImage = img32;
            //familyEI.ToolTip = Message;
            //familyEI.SetContextualHelp(ch);
            //optionsBtn.AddPushButton(new PushButtonData("Automatic Dimensions", "AutoDim", path,
            //    "AutomaticDimensions.AutoDim"));
            //optionsBtn.AddPushButton(new PushButtonData("CAD|BIM", "CAD|BIM", path,
            //    "BimpowAddIn.BimToCad"));
        }

        private static void CreatePushButton(RibbonPanel ribbonPanel, string name, string path, string command, string tooltip, string icon, ContextualHelp ch)
        {
            PushButtonData pbData = new PushButtonData(
                name,
                name,
                path,
                command);

            PushButton pb = ribbonPanel.AddItem(pbData) as PushButton;
            pb.ToolTip = tooltip;
            pb.SetContextualHelp(ch);
            BitmapImage pb2Image = new BitmapImage(new Uri(String.Format("pack://application:,,,/FamilyEditorInterface;component/Resources/{0}", icon)));
            pb.LargeImage = pb2Image;
        }
        /// <summary>
        /// event handler that auto-rejects renaming of views
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void a_DialogBoxShowing(
            object sender,
            DialogBoxShowingEventArgs e)
        {
            TaskDialogShowingEventArgs e2
                = e as TaskDialogShowingEventArgs;

            string s = string.Empty;

            if (null != e2)
            {
                s = string.Format(", dialog id {0}, message '{1}'",
                    e2.DialogId, e2.Message);

                bool isConfirm = e2.Message.Contains("Would you like to rename corresponding level and views?");

                if (isConfirm)
                {
                    e2.OverrideResult(
                        (int)System.Windows.Forms.DialogResult.No);

                    s += ", auto-confirmed.";
                }
            }
        }
        #endregion

        #region Revit Hooks
        public Result OnStartup(UIControlledApplication a)
        {
            ControlledApplication c_app = a.ControlledApplication;
            AddRibbonPanel(a);

            thisApp = this;  // static access to this application instance

            c_app.DocumentChanged += new EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs>(c_app_DocumentChanged);
            c_app.DocumentClosed += new EventHandler<Autodesk.Revit.DB.Events.DocumentClosedEventArgs>(c_app_DocumentClosed);
            a.ViewActivated += new EventHandler<Autodesk.Revit.UI.Events.ViewActivatedEventArgs>(OnViewActivated);

            return Result.Succeeded;
        }
        /// <summary>
        /// On Document Switched
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            if (!Started) return;  // only do if the plugin is active

            if (_presenter == null)
            {
                ShowForm();
                return;
            }
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
                _presenter._Document = doc;
                _presenter.DocumentSwitched();
            }
        }
        /// <summary>
        /// On document change, update Family Parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void c_app_DocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
        {
            if (!Started) return;  // only do if the plugin is active

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
        private void c_app_DocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            if (!Started) return;  // only do if the plugin is active

            if (App.ActiveUIDocument == null && Started && _presenter != null)
            {
                _presenter.Close();
                _presenter = null;
            }
        }
        public Result OnShutdown(UIControlledApplication a)
        {
            ControlledApplication c_app = a.ControlledApplication;

            c_app.DocumentChanged -= new EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs>(c_app_DocumentChanged);
            c_app.DocumentClosed -= new EventHandler<Autodesk.Revit.DB.Events.DocumentClosedEventArgs>(c_app_DocumentClosed);
            a.ViewActivated -= new EventHandler<Autodesk.Revit.UI.Events.ViewActivatedEventArgs>(OnViewActivated);

            if(Started) Started = false;

            if (_presenter != null)
            {
                _presenter.Close();
            }

            return Result.Succeeded;
        }
        #endregion

        #region Show Form
        /// <summary>
        /// De-facto the command is here.
        /// </summary>
        /// <param name="uiapp"></param>
        public void ShowForm()
        {
            _presenter = null;
            
            // get the isntance of Revit Thread
            // to pass it to the Windows Form later
            if (null == _hWndRevit)
            {
                Process process
                  = Process.GetCurrentProcess();

                IntPtr h = process.MainWindowHandle;
                _hWndRevit = new WindowHandle(h);
            }

            if (_presenter == null || _presenter._isClosed)
            {
                if(!App.ActiveUIDocument.Document.IsFamilyDocument)
                {
                    TaskDialog.Show("Error", "Usable only in Family Documents");
                    return;
                }
                //new handler
                handler = new RequestHandler();
                //new event
                exEvent = ExternalEvent.Create(handler);
                // set current document

                _document = App.ActiveUIDocument.Document;
                _presenter = new FamilyParameterViewModel(_document);
                
                try
                {
                    //pass parent (Revit) thread here
                    _presenter.Show(_hWndRevit);
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
        private void Stop(object sender, EventArgs e)
        {
            Started = false;
        }
        public void WakeFormUp()
        {
            //if (m_MyForm != null)
            //{
            //    m_MyForm.WakeUp();
            //}
        }
        #endregion
    }
    /// <summary>
    /// Retrieve Revit Windows thread in order to pass it to the form as it's owner
    /// </summary>
    public class WindowHandle : IWin32Window
    {
      IntPtr _hwnd;
 
      public WindowHandle( IntPtr h )
      {
        Debug.Assert( IntPtr.Zero != h,
          "expected non-null window handle" );
 
        _hwnd = h;
      }
 
      public IntPtr Handle
      {
        get
        {
          return _hwnd;
        }
      }
    }
}

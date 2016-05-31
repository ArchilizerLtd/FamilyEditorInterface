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

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System.Windows.Forms;
using System.Diagnostics;
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
        // class instance
        internal static Application thisApp = null;
        // ModelessForm instance
        private Interface m_MyForm;
        /// <summary>
        /// Tooltip
        /// </summary>
        public const string Message = "Interactive interface for Family Editor.";
        /// <summary>
        /// Get absolute path to this assembly
        /// </summary>
        static string path = Assembly.GetExecutingAssembly().Location;
        static string contentPath = Path.GetDirectoryName(Path.GetDirectoryName(path)) + "/";
        static string helpFile = "file:///C:/ProgramData/Autodesk/ApplicationPlugins/FamilyEditorInterface.bundle/Content/Family%20Editor%20Interface%20_%20Revit%20_%20Autodesk%20App%20Store.html";
        static string largeIcon = contentPath + "familyeditorinterface32.png";
        static string smallIcon = contentPath + "familyeditorinterface16.png";
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
        /// <param name="a"></param>
        private void AddRibbonPanel(UIControlledApplication a)
        {
            Autodesk.Revit.UI.RibbonPanel rvtRibbonPanel = a.CreateRibbonPanel("Archilizer FEI");
            PulldownButtonData data = new PulldownButtonData("Options", "Family Editor" + Environment.NewLine + "Interface");

            BitmapSource img32 = new BitmapImage (new Uri (@largeIcon));
            BitmapSource img16 = new BitmapImage (new Uri (@smallIcon));

            //RibbonItem item = rvtRibbonPanel.AddItem(data);
            //PushButton optionsBtn = item as PushButton;
            //ContextualHelp ch = new ContextualHelp(ContextualHelpType.Url, "file:///C:/Users/adicted/AppData/Roaming/Autodesk/Revit/Addins/2015/Family Editor Interface _ AutoCAD _ Autodesk App Store.html");
            ContextualHelp ch = new ContextualHelp(ContextualHelpType.Url, @helpFile);
            
            PushButton familyEI = rvtRibbonPanel.AddItem(new PushButtonData("Family Editor", "Family Editor" + Environment.NewLine +  "Interface", path,
                "FamilyEditorInterface.Command")) as PushButton;

            familyEI.Image = img16;
            familyEI.LargeImage = img32;
            familyEI.ToolTip = Message;
            familyEI.SetContextualHelp(ch);
            //optionsBtn.AddPushButton(new PushButtonData("Automatic Dimensions", "AutoDim", path,
            //    "AutomaticDimensions.AutoDim"));
            //optionsBtn.AddPushButton(new PushButtonData("CAD|BIM", "CAD|BIM", path,
            //    "BimpowAddIn.BimToCad"));
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
        public Result OnStartup(UIControlledApplication a)
        {
            ControlledApplication c_app = a.ControlledApplication;
            AddRibbonPanel(a);
            //a.DialogBoxShowing
            //+= new EventHandler<DialogBoxShowingEventArgs>(
            //    a_DialogBoxShowing);
            m_MyForm = null;  // no dialog needed yet; ThermalAsset command will bring it
            thisApp = this;  // static access to this application instance
            c_app.DocumentChanged
                += new EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs>(
                    c_app_DocumentChanged);
            return Result.Succeeded;
        }
        /// <summary>
        /// On document change, update Family Parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void c_app_DocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
        {
            if (m_MyForm != null && m_MyForm.Visible)
            {
                IList<String> operations = e.GetTransactionNames();
                if (operations.Contains("Delete param") || operations.Contains("New param"))
                {
                    m_MyForm.DocumentChanged();
                } 
                else if (operations.Contains("Family Types"))
                {
                    //not sure how to filter that
                    //ICollection<ElementId> changedId = e.GetModifiedElementIds();
                    //m_MyForm.ElementChanged(changedId);
                    m_MyForm.DocumentChanged();
                }
            }
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            ControlledApplication c_app = a.ControlledApplication;
            c_app.DocumentChanged
                -= new EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs>(
                    c_app_DocumentChanged);

            if (m_MyForm != null && m_MyForm.Visible)
            {
                m_MyForm.Close();
            }

            return Result.Succeeded;
        }
        /// <summary>
        /// De-facto the command is here.
        /// </summary>
        /// <param name="uiapp"></param>
        public void ShowForm(UIApplication uiapp)
        {
            //get the isntance of Revit Thread
            //to pass it to the Windows Form later
            if (null == _hWndRevit)
            {
                Process process
                  = Process.GetCurrentProcess();

                IntPtr h = process.MainWindowHandle;
                _hWndRevit = new WindowHandle(h);
            }

            if (m_MyForm == null || m_MyForm.IsDisposed)
            {
                //new handler
                RequestHandler handler = new RequestHandler();
                //new event
                ExternalEvent exEvent = ExternalEvent.Create(handler);

                m_MyForm = new Interface(uiapp, exEvent, handler);
                //pass parent (Revit) thread here
                m_MyForm.Show(_hWndRevit);
            }
        }

        public void WakeFormUp()
        {
            if (m_MyForm != null)
            {
                m_MyForm.WakeUp();
            }
        }
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

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
using System.Linq;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using AW = Autodesk.Windows;
using System.Diagnostics;
using FamilyEditorInterface.WPF;
using Autodesk.Revit.DB.Events;
using Archilizer_Purge.Helpers;
using FamilyEditorInterface.Resources.WPF.ViewModel;
#endregion

namespace FamilyEditorInterface
{
    /// <summary>
    /// Implements the Revit add-in interface IExternalApplication
    /// </summary>
    class Application : IExternalApplication
    {
        #region Prpoerties & Fields
        // class instance
        internal static Application thisApp = null;

        private static UIControlledApplication MyApplication { get; set; }
        private static Assembly assembly;    
        public static UIApplication App;

        // Tooltip
        public const string Message = "Interactive interface for Family Editor.";

        public static AppControl Control;

        // Get absolute path to this assembly
        static string path = Assembly.GetExecutingAssembly().Location;
        static string contentPath = Path.GetDirectoryName(Path.GetDirectoryName(path)) + "/";
        static string helpFile = "file:///C:/ProgramData/Autodesk/ApplicationPlugins/FamilyEditorInterface.bundle/Content/Family%20Editor%20Interface%20_%20Revit%20_%20Autodesk%20App%20Store.html"; 
        private static string assemblyVersion;

        // Marks if the Plugin has been started
        private static bool _started;
        public static bool Started 
        {
            get { return _started; }
            set
            {
                _started = value;
            }
        }
        #endregion
        
        #region Ribbon
        //Add ribbon panel 
        private void AddRibbonPanel(UIControlledApplication application)
        {
            assembly = Assembly.GetExecutingAssembly();
            assemblyVersion = $"v{assembly.GetName().Version.ToString()}";
            assemblyVersion = assemblyVersion.Substring(0, assemblyVersion.Length - 2);
            string thisAssemblyPath = assembly.Location;   // Get dll assembly path

            // Create a custom ribbon panel or use the existing one
            String tabName = "Archilizer";
            String panelName = "Family Document";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { }

            RibbonPanel rvtRibbonPanel = PickPanel(application, panelName, tabName);
            ContextualHelp ch = new ContextualHelp(ContextualHelpType.Url, @helpFile);
                        
            var buttons = GetStackButtons(ch); //Create the stack buttons

            CreatePushButton(rvtRibbonPanel, String.Format("Family Editor" + Environment.NewLine + "Interface"), thisAssemblyPath, "FamilyEditorInterface.Command", Message, assemblyVersion, "FamilyEditorInterface.Resources.icon_Families.png", ch);
            CreateStackButtons(tabName, panelName, rvtRibbonPanel, buttons, thisAssemblyPath);
        }
        //Creates the local stack buttons for this application
        private List<Button> GetStackButtons(ContextualHelp ch)
        {
            var buttons = new List<Button>()
            {
                new Button(){Name = "Associate Parameters",
                    Image = "FamilyEditorInterface.Resources.icon_Associate Parameters.png",
                    ToolTip = "Associate Parameters between two Families",
                    ContextualHelp = ch,
                    Text = "Associate Parameters",
                    Command = "FamilyEditorInterface.CommandAssociateParameters"},

                new Button(){Name = "Push Parameters",
                    Image = "FamilyEditorInterface.Resources.icon_Push Parameters.png",
                    ToolTip = "Push parameters from the current family into a nested family",
                    ContextualHelp = ch,
                    Text = "Push Parameters",
                    Command = "FamilyEditorInterface.CommandPushParameters"},

                new Button(){Name = "Pull Parameters",
                    Image = "FamilyEditorInterface.Resources.icon_Pull Parameters.png",
                    ToolTip = "Pulls Parameters from a nested family into the current family",
                    ContextualHelp = ch,
                    Text = "Pull Parameters",
                    Command = "FamilyEditorInterface.CommandPullParameters"}
            };

            return buttons;
        }
        #endregion

        #region Ribbon Helper Methods
        //Pick existing or new panel
        private RibbonPanel PickPanel(UIControlledApplication application, string panelName, string tabName)
        {
            List<RibbonPanel> panels = application.GetRibbonPanels();
            RibbonPanel panel = null;

            // Pick the correct panel
            if (panels.FirstOrDefault(x => x.Name.Equals(panelName, StringComparison.OrdinalIgnoreCase)) == null)
            {
                panel = application.CreateRibbonPanel(tabName, panelName);
            }
            else
            {
                panel = panels.FirstOrDefault(x => x.Name.Equals(panelName, StringComparison.OrdinalIgnoreCase)) as RibbonPanel;
            }
            return panel;
        }
        //Create a pushbutton
        private static void CreatePushButton(RibbonPanel ribbonPanel, string name, string path, string command, string tooltip, string assemblyVersion, string icon, ContextualHelp ch)
        {
            PushButtonData pbData = new PushButtonData(
                name,
                name,
                path,
                command);

            PushButton pb = ribbonPanel.AddItem(pbData) as PushButton;
            pb.ToolTip = $"{tooltip}{Environment.NewLine}{Environment.NewLine}{assemblyVersion}";
            pb.SetContextualHelp(ch);

            BitmapIcons bitmapIcons = new BitmapIcons(assembly, icon, MyApplication);
            var largeImage = bitmapIcons.LargeBitmap();
            var smallImage = bitmapIcons.SmallBitmap();
            pb.LargeImage = largeImage;
            pb.Image = smallImage;
        }
        //Create stack column
        private IList<RibbonItem> CreateStackButtons(string tabName, string panelName, RibbonPanel ribbonPanel, List<Button> buttons, string path)
        {
            PushButtonData[] buttonData = new PushButtonData[3];
            string [] buttonNames = new string[3];

            int index = 0;

            foreach (var button in buttons)
            {
                PushButtonData data = new PushButtonData(button.Name, button.Text, path, button.Command);
                data.ToolTip = button.ToolTip;
                data.SetContextualHelp(button.ContextualHelp);

                BitmapIcons bitmapIcons = new BitmapIcons(assembly, button.Image, MyApplication);
                var largeImage = bitmapIcons.LargeBitmap();
                var smallImage = bitmapIcons.SmallBitmap();
                data.LargeImage = largeImage;
                data.Image = smallImage;

                buttonData[index] = data;
                buttonNames[index] = button.Name;

                index++;
            }

            IList<RibbonItem> ribbonItem = ribbonPanel.AddStackedItems(buttonData[0], buttonData[1], buttonData[2]);
            SupressStackButtonText(tabName, panelName, buttonNames);

            return ribbonItem;
        }
        //This is hacky and experimental method to suppors the Text of stack buttons
        private void SupressStackButtonText(string tab, string panel, string [] buttonNames)
        {
            // Find Autodes.Windows.RibbonItems
            var btnTest1 = GetButton(tab, panel, buttonNames[0]);
            var btnTest2 = GetButton(tab, panel, buttonNames[1]);
            var btnTest3 = GetButton(tab, panel, buttonNames[2]);

            // Set Size and Text Visibility
            btnTest1.Size = AW.RibbonItemSize.Standard;
            btnTest1.ShowText = false;
            btnTest2.Size = AW.RibbonItemSize.Standard;
            btnTest2.ShowText = false;
            btnTest3.Size = AW.RibbonItemSize.Standard;
            btnTest3.ShowText = false;
        }
        //Hacky way to get a ribbon item, not supported officially
        public AW.RibbonItem GetButton(string tabName, string panelName, string itemName)
        {
            AW.RibbonControl ribbon = AW.ComponentManager.Ribbon;
            foreach (AW.RibbonTab tab in ribbon.Tabs)
            {
                if (tab.Name == tabName)
                {
                    foreach (AW.RibbonPanel panel in tab.Panels)
                    {
                        if (panel.Source.Title == panelName)
                        {
                            return panel.FindItem("CustomCtrl_%CustomCtrl_%"
                              + tabName + "%" + panelName + "%" + itemName,
                              true) as AW.RibbonItem;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region Revit Hooks
        //On Revit Started, attach all handle hooks and init the needed elements
        public Result OnStartup(UIControlledApplication a)
        {
            ControlledApplication c_app = a.ControlledApplication;
            MyApplication = a;
            AddRibbonPanel(a);

            thisApp = this;  //static access to this application instance
            Control = new AppControl(); //Create new app control 

            c_app.DocumentChanged += new EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs>(c_app_DocumentChanged);
            c_app.DocumentClosed += new EventHandler<Autodesk.Revit.DB.Events.DocumentClosedEventArgs>(c_app_DocumentClosed);
            a.ViewActivated += new EventHandler<Autodesk.Revit.UI.Events.ViewActivatedEventArgs>(OnViewActivated);

            return Result.Succeeded;
        }
        //On Revit Shutdown, detach all handle hooks
        public Result OnShutdown(UIControlledApplication a)
        {
            ControlledApplication c_app = a.ControlledApplication;

            c_app.DocumentChanged -= new EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs>(c_app_DocumentChanged);
            c_app.DocumentClosed -= new EventHandler<Autodesk.Revit.DB.Events.DocumentClosedEventArgs>(c_app_DocumentClosed);
            a.ViewActivated -= new EventHandler<Autodesk.Revit.UI.Events.ViewActivatedEventArgs>(OnViewActivated);

            if (Started) Started = false;
            Control.Close();

            return Result.Succeeded;
        }
        //On Document Switched
        private void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            if (!Started) return;  // only do if the plugin is active
            Control.Show(e);
        }
        //On document change, update Family Parameters
        private void c_app_DocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
        {
            if (!Started) return;  // only do if the plugin is active
            Control.DocumentChanged(e);

        }
        //On document closed, close the ViewModel
        private void c_app_DocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            if (!Started) return;  // only do if the plugin is active
            Control.DocumentClosed();
        }
        #endregion
    }

    public class Button
    {
        public string Name { get; set; }
        public string Image { get; set; }
        public string Text { get; internal set; }
        public string Command { get; internal set; }
        public string ToolTip { get; internal set; }
        public ContextualHelp ContextualHelp { get; internal set; }
    }
}

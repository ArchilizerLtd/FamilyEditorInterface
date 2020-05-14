using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using FamilyEditorInterface.Associate.WPFSelectParameters.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.Associate
{
	/// <summary>
	/// ***** Push parameters from the current family into a nested family *****
	/// 1. Read all existing user-defined parameters (skip built-in)
	/// 2. UI allows the user to select parameters
	/// 3. Open the nested family
	/// 4. Creates the parameters
	/// 5. Optional (SHIFT) - associates the newly created parameters
	/// </summary>
	public class PushParameters
	{
		#region Properties & Fields
		private Autodesk.Revit.ApplicationServices.Application app;
		private UIDocument uidoc;
		private Document doc;
		private FamilyManager familyManager;
		private List<FamilyParameter> famParameters;
		private List<FamilyParameter> pushParameters = null;
		private ExternalDefinition sharedParam = null;
		#endregion

		#region Constructors & Initializers
		//Constructor
		public PushParameters(Autodesk.Revit.ApplicationServices.Application app, UIDocument uidoc, Document doc)
		{
			this.app = app;
			this.uidoc = uidoc;
			this.doc = doc;

			Initialize();
		}

		private void Initialize()
		{
			familyManager = doc.FamilyManager;
			famParameters = new List<FamilyParameter>();

			var parameters = familyManager.Parameters;

			foreach (FamilyParameter famParam in parameters)
			{
				famParameters.Add(famParam);
			}

			GetPushParamters(); //User Select all parameters to be transfered
		}
		//UI Call
		private void GetPushParamters()
		{
			ParameterSelectorViewModel vm = new ParameterSelectorViewModel(famParameters);
			vm.Show();
			//UI - Get Push Parameters
			//if (famParam.Definition.Name.Equals(paramName)) paramToPush = famParam;
		}
		#endregion

		#region Methods
		/// <summary>
		/// The main method. Pushes selected parameters into a nested family
		/// </summary>
		public void Push()
		{
			if (!doc.IsFamilyDocument) return;  //Only execute in FamilyDocument
			if (pushParameters == null) return; //Couldn't find it

			var selection = uidoc.Selection.PickObjects(ObjectType.Element, "Pick family to push to");  //Select a family
			if (selection == null) return;

			foreach (Reference sel in selection)
			{
				foreach (FamilyParameter paramToPush in pushParameters)
				{
					sharedParam = GetSharedParameter(app, paramToPush.Definition.Name); //This only returns if a paramter is shared though ... messy messy
					ExecuteSharedPushParamters(sel, sharedParam, paramToPush);
				}
			}
		}
		//TO DO: Clean the mess
		//TO DO: Replace with SubTransactions if possible?
		//We will be pushing paramters from the current family into the Nested family
		//The parameter will be a shared parameter
		//Therefore, if the parameter already exists, we will remove it  first
		private void ExecuteSharedPushParamters(Reference selection, ExternalDefinition sharedParam, FamilyParameter paramToPush)
		{
			var pushFamInstance = doc.GetElement(selection.ElementId) as FamilyInstance;
			var pushFamily = pushFamInstance.Symbol.Family; //Get the family from the selection
			var pushFamDoc = doc.EditFamily(pushFamily);    //Open the family

			RemoveExistingParameter(pushFamDoc, paramToPush);   //Remove the existing project parameter 
			AddSharedParameter(pushFamDoc, paramToPush);    //To replace it with a Shared Parameter
			LoadBackFamily(pushFamDoc, pushFamily);
			AssociateParameters(pushFamInstance, paramToPush);
		}
		//Associate the parameters
		private void AssociateParameters(FamilyInstance pushFamInstance, FamilyParameter paramToPush)
		{
			using (Transaction tw = new Transaction(doc, "Wire parameters"))
			{
				tw.Start();
				try
				{
					var famParameter = pushFamInstance.LookupParameter(paramToPush.Definition.Name);    //Finds the newly created parameter in the nested family
					doc.FamilyManager.AssociateElementParameterToFamilyParameter(famParameter, paramToPush);    //And associates it to the existing family parameter
				}
				catch (Exception ex) { }
				tw.Commit();
			}
		}
		//Loads back a nested family into the main family
		private void LoadBackFamily(Document pushFamDoc, Family pushFamily)
		{
			// Load back the family	    
			using (Transaction t = new Transaction(pushFamDoc, "Push parameter"))
			{
				t.Start();
				pushFamily = pushFamDoc.LoadFamily(doc, new FamilyOption());
				t.Commit();
			}
		}
		//Adds a parameter as a SharedParemter
		private void AddSharedParameter(Document pushFamDoc, FamilyParameter paramToPush)
		{
			// Add the parameter to the family
			try
			{
				using (Transaction ft = new Transaction(pushFamDoc, "Push parameter"))
				{
					ft.Start();
					pushFamDoc.FamilyManager.AddParameter(sharedParam, paramToPush.Definition.ParameterGroup, paramToPush.IsInstance);
					ft.Commit();
				}
			}
			catch (Exception) { }
		}
		//Remove existing parameter
		private void RemoveExistingParameter(Document pushFamDoc, FamilyParameter paramToPush)
		{
			//If the parameter already exists, remove the parameter before adding it again
			//Issue a warning maybe? Or skip this?
			try
			{
				using (Transaction ft = new Transaction(pushFamDoc, "Push parameter"))
				{
					ft.Start();
					foreach (FamilyParameter famparam in pushFamDoc.FamilyManager.Parameters)   //Go through each parameter in the push document
					{
						if (famparam.Definition.Name.Equals(paramToPush.Definition.Name))
						{
							pushFamDoc.FamilyManager.RemoveParameter(famparam); //If the parameter already exists, remove it. Or should we skip it?
							break;
						}
					}

					ft.Commit();
				}
			}
			catch (Exception) { }
		}
		//Gets a SharedParameter
		private ExternalDefinition GetSharedParameter(Autodesk.Revit.ApplicationServices.Application app, string name)
		{
			DefinitionFile defFile = app.OpenSharedParameterFile();
			if (defFile == null) throw new Exception("No SharedParameter File!");

			var v = (from DefinitionGroup dg in defFile.Groups
					 from ExternalDefinition d in dg.Definitions
					 where d.Name == name
					 select d);

			if (v == null || v.Count() < 1) throw new Exception("Invalid Name Input!");

			ExternalDefinition def = v.First();
			return def;
		}
		#endregion
	}

	/// <summary>
	/// FamilyOptions class. Inherits from IFamilyLoadOptions
	/// </summary>
	public class FamilyOption : IFamilyLoadOptions
	{
		public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
		{
			overwriteParameterValues = true;
			return true;
		}

		public bool OnSharedFamilyFound(Family sharedFamily,
			bool familyInUse,
			out FamilySource source,
			out bool overwriteParameterValues)
		{
			source = FamilySource.Family;
			overwriteParameterValues = true;
			return true;
		}
	}
}

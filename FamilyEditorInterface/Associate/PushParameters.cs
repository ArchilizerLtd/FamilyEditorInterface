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
		private Autodesk.Revit.ApplicationServices.Application app;	//Revit Application
		private UIDocument uidoc;	//Current Revit UIDocument
		private Document doc;	//Active (Family) Document
		private FamilyManager familyManager;	//The FamilyManager of this Document. Contains everything about Parameters
		private List<FamilyParameter> familyParameters;	//Contains all FamilyParameters in the current Document
		private List<FamilyParameter> pushParameters = null;	//User selected list of parameters to be pushed in the selected Nested family
		#endregion

		#region Constructors & Initializers
		/// <summary>
		/// The public constructor. Initializes starting parameters
		/// </summary>
		/// <param name="app">Revit Application</param>
		/// <param name="uidoc">Current Revit UIDocument</param>
		/// <param name="doc">Active (Family) Document</param>
		public PushParameters(Autodesk.Revit.ApplicationServices.Application app, UIDocument uidoc, Document doc)
		{
			this.app = app;
			this.uidoc = uidoc;
			this.doc = doc;

			Initialize();
		}
		//Initializes starting parameters
		private void Initialize()
		{
			familyManager = doc.FamilyManager;
			familyParameters = new List<FamilyParameter>();

			foreach (FamilyParameter famParam in familyManager.Parameters)
			{
				familyParameters.Add(famParam);	//Fills in all FamilyParameters in the current document
			}

			GetPushParamters(); //User Select all parameters to be transfered
		}
		//Envokes the UI to select FamilyParameters to be pushed
		private void GetPushParamters()
		{
			var vm = new ParameterSelectorViewModel(familyParameters);	//Initializes a new view and passes in this ViewModel as a DataContext
			vm.Show();	//Show the UI
			pushParameters = vm.selectedParameters;	//Collect all user-selected parameters
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// The Main method. Pushes selected parameters into a nested family
		/// </summary>
		public void Push()
		{
			if (!doc.IsFamilyDocument) return;  //Only execute in FamilyDocument
			if (pushParameters == null) return; //No parameter to be pushed, return

			var selection = uidoc.Selection.PickObjects(ObjectType.Element, "Pick family to push to");  //Select a family, Revit user interface
			if (selection == null) return;  //If the user did not select (escaped), return

			foreach (Reference sel in selection)
			{
				var familyInstance = doc.GetElement(sel.ElementId) as FamilyInstance;	//Cast the current selection as a FamilyInstance
				if (familyInstance == null) continue;	//If the selection is not a family instance, skip

				foreach (FamilyParameter paramToPush in pushParameters)
				{
					ExecutePushParamters(familyInstance, paramToPush);	//Execute Push Parameter for each Selection and each Selected Paramter
				}
			}
		}
        #endregion

        #region Internal Methods
        //Push parameter into selected FamilyInstance
        private void ExecutePushParamters(FamilyInstance familyInstance, FamilyParameter parameterToPush)
		{
			var nestedFamily = familyInstance.Symbol.Family; //Get the family from the selection
			var nestedFamilyDocument = doc.EditFamily(nestedFamily);    //Open the family

			RemoveExistingParameter(nestedFamilyDocument, parameterToPush);   //Remove the existing project parameter 
			AddParameter(nestedFamilyDocument, parameterToPush);	//Add the Parameter to the Nested Family
			LoadBackFamily(nestedFamilyDocument, nestedFamily);		//Load back the Nested Family
			AssociateParameters(familyInstance, parameterToPush);	//Associate the newly created nested family parameters to the current family parameters
		}
		//Remove existing parameter
		private void RemoveExistingParameter(Document nestedFamilyDocument, FamilyParameter paramToPush)
		{
			//If the parameter already exists, remove the parameter before adding it again
			//Issue a warning maybe? Or skip this?
			try
			{
				using (Transaction ft = new Transaction(nestedFamilyDocument, "Push parameter"))
				{
					ft.Start();
					foreach (FamilyParameter famparam in nestedFamilyDocument.FamilyManager.Parameters)   //Go through each parameter in the push document
					{
						if (famparam.Definition.Name.Equals(paramToPush.Definition.Name))
						{
							nestedFamilyDocument.FamilyManager.RemoveParameter(famparam); //If the parameter already exists, remove it. Or should we skip it?
							break;
						}
					}

					ft.Commit();
				}
			}
			catch (Exception) { }
		}
		//Adds a Project or Shared Parameter depending on the original parameter to be pushed
		private void AddParameter(Document nestedFamilyDocument, FamilyParameter parameterToPush)
		{
			if (parameterToPush.IsShared)
			{
				ExternalDefinition sharedParameter = GetSharedParameter(app, parameterToPush.Definition.Name); //Get the ExternalDefinition, in case it's a Shared Parameter
				AddSharedParameter(nestedFamilyDocument, sharedParameter, parameterToPush);    //Adds a Shared Parameter
			}
			else
			{
				AddProjectParameter(nestedFamilyDocument, parameterToPush);   //Adds a (normal) Project Parameter
			}
		}
		//Creates a regular parameter
		private void AddProjectParameter(Document nestedFamilyDocument, FamilyParameter parameterToPush)
		{
			try
			{
				using (Transaction ft = new Transaction(nestedFamilyDocument, "Push parameter"))
				{
					ft.Start();
					//Adds a Parameter to the Family
					nestedFamilyDocument.FamilyManager.AddParameter(parameterToPush.Definition.Name, parameterToPush.Definition.ParameterGroup, parameterToPush.Definition.ParameterType, parameterToPush.IsInstance);
					ft.Commit();
				}
			}
			catch (Exception) { }
		}
		//Adds a parameter as a SharedParemter
		private void AddSharedParameter(Document nestedFamilyDocument, ExternalDefinition sharedParameter, FamilyParameter paramToPush)
		{
			// Add the parameter to the family
			try
			{
				using (Transaction ft = new Transaction(nestedFamilyDocument, "Push shared parameter"))
				{
					ft.Start();
					//Adds a Shared Parameter to the Family
					nestedFamilyDocument.FamilyManager.AddParameter(sharedParameter, paramToPush.Definition.ParameterGroup, paramToPush.IsInstance);
					ft.Commit();
				}
			}
			catch (Exception) { }
		}
		//Loads back a nested family into the main family
		private void LoadBackFamily(Document nestedFamilyDocument, Family nestedFamily)
		{ 
			using (Transaction t = new Transaction(nestedFamilyDocument, "Push parameter"))
			{
				t.Start();
				//Loads back the nested family into the main family document
				nestedFamily = nestedFamilyDocument.LoadFamily(doc, new FamilyOption());
				t.Commit();
			}
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
				catch (Exception) { }
				tw.Commit();
			}
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

			if (v == null || v.Count() < 1) return null;	//There is no Shared parameter with this name

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

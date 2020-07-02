using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using FamilyEditorInterface.Associate.WPFSelectParameters.ViewModel;
using FamilyEditorInterface.Dialog.Alerts;
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
		private Document doc;   //Active (Family) Document
		private FamilyInstance familyInstance;
		private FamilyManager familyManager;	//The FamilyManager of this Document. Contains everything about Parameters
		private Dictionary<FamilyParameter, bool> familyParameters;	//Contains all FamilyParameters in the current Document
		private List<FamilyParameter> pushParameters = null;    //User selected list of parameters to be pushed in the selected Nested family
		private string AlertMessage;    //Contains all warnings to be displayed at the end of the run
		private string SuccessMessage;    //Contains all the successfully pulled parameters to be displayed at the end of the run
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
			familyInstance = GetFamilyInstance();
			if (familyInstance == null) return;	//We failed to collect a FamilyInstance

			familyManager = doc.FamilyManager;
			familyParameters = new Dictionary<FamilyParameter, bool>();

			foreach (FamilyParameter famParam in familyManager.Parameters)
			{
				familyParameters[famParam] = Utils.ParameterExist(famParam, familyInstance);	//Fills in all FamilyParameters in the current document
			}

			GetPushParamters(); //User Select all parameters to be transfered
		}
		//Retrieves a nested Family Instance from User selection
		private FamilyInstance GetFamilyInstance()
		{
			if (!doc.IsFamilyDocument) return null;  //Only execute in FamilyDocument

			var selection = Utils.PickObject(uidoc, "Pick family to push parameters to.");  //Select a family, Revit user interface
			if (selection == null) return null;  //If the user did not select (escaped), return


			var familyInstance = doc.GetElement(selection.ElementId) as FamilyInstance; //Cast the current selection as a FamilyInstance
			if (familyInstance == null) return null; //If the selection is not a family instance, skip

			return familyInstance;
		}
		//Envokes the UI to select FamilyParameters to be pushed
		private void GetPushParamters()
		{
			var vm = new ParameterSelectorViewModel(familyParameters, "Push");	//Initializes a new view and passes in this ViewModel as a DataContext
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
			if (pushParameters == null) return; //No parameter to be pushed, return
			if (familyInstance == null) return;	//If we failed to collect a family instance from user in the previous step, return
			foreach (FamilyParameter paramToPush in pushParameters)
			{
				SuccessMessage += ExecutePushParamters(familyInstance, paramToPush);	//Execute Push Parameter for each Selection and each Selected Paramter
			}			

			if (!String.IsNullOrEmpty(AlertMessage)) DialogUtils.Alert("Warning", AlertMessage);    //Finally, alert the user if we had any issues
			if (!String.IsNullOrEmpty(SuccessMessage)) DialogUtils.OK("Push successful", SuccessMessage);  //And, issue an OK message the user for all the successfully processed parameters
		}
        #endregion

        #region Internal Methods
        //Push parameter into selected FamilyInstance
        private string ExecutePushParamters(FamilyInstance familyInstance, FamilyParameter parameterToPush)
		{
			var nestedFamily = familyInstance.Symbol.Family; //Get the family from the selection
			var nestedFamilyDocument = doc.EditFamily(nestedFamily);    //Open the family

			var exist = RemoveExistingParameter(nestedFamilyDocument, parameterToPush);   //Remove the existing project parameter 
			if (!string.IsNullOrEmpty(exist)) AlertMessage += exist;
			
			var success = AddParameter(nestedFamilyDocument, parameterToPush);  //Add the Parameter to the Nested Family
			LoadBackFamily(nestedFamilyDocument, nestedFamily);     //Load back the Nested Family
			if (!string.IsNullOrEmpty(success)) return success;
			else return null;

			//FUTURE - Associate the newly created parameters as well
			//AssociateParameters(familyInstance, parameterToPush);	//Associate the newly created nested family parameters to the current family parameters
		}
		//Remove existing parameter
		private string RemoveExistingParameter(Document nestedFamilyDocument, FamilyParameter paramToPush)
		{
			//If the parameter already exists, remove the parameter before adding it again
			//Issue a warning maybe? Or skip this?
			foreach (FamilyParameter famparam in nestedFamilyDocument.FamilyManager.Parameters)   //Go through each parameter in the push document
			{
				if (famparam.Definition.Name.Equals(paramToPush.Definition.Name))
				{
					return $"Parameter '{famparam.Definition.Name}' alerady exists in the current Family and will be skipped.{Environment.NewLine}";
				}
			}
			return null;
		}
		//Adds a Project or Shared Parameter depending on the original parameter to be pushed
		private string AddParameter(Document nestedFamilyDocument, FamilyParameter parameterToPush)
		{
			if (parameterToPush.IsShared)
			{
				ExternalDefinition sharedParameter = GetSharedParameter(app, parameterToPush.Definition.Name); //Get the ExternalDefinition, in case it's a Shared Parameter
				if (sharedParameter != null) return AddSharedParameter(nestedFamilyDocument, sharedParameter, parameterToPush);    //Adds a Shared Parameter
				else return null;
			}
			else
			{
				return AddProjectParameter(nestedFamilyDocument, parameterToPush);   //Adds a (normal) Project Parameter
			}
		}
		//Creates a regular parameter
		private string AddProjectParameter(Document nestedFamilyDocument, FamilyParameter parameterToPush)
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
			return $"{parameterToPush.Definition.Name} pushed sucessfully{Environment.NewLine}";
		}
		//Adds a parameter as a SharedParemter
		private string AddSharedParameter(Document nestedFamilyDocument, ExternalDefinition sharedParameter, FamilyParameter parameterToPush)
		{
			// Add the parameter to the family
			try
			{
				using (Transaction ft = new Transaction(nestedFamilyDocument, "Push shared parameter"))
				{
					ft.Start();
					//Adds a Shared Parameter to the Family
					nestedFamilyDocument.FamilyManager.AddParameter(sharedParameter, parameterToPush.Definition.ParameterGroup, parameterToPush.IsInstance);
					ft.Commit();
				}
			}
			catch (Exception) { }
			return $"{parameterToPush.Definition.Name} pushed sucessfully{Environment.NewLine}";
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
		//NOT IN USE IN THIS VERSION
		private void AssociateParameters(FamilyInstance pushFamInstance, FamilyParameter paramToPush)
		{
			using (Transaction tw = new Transaction(doc, "Wire parameters"))
			{
				tw.Start();
				try
				{
					var famParameter = GetNestedParameter(pushFamInstance, paramToPush.Definition.Name);    //Finds the newly created parameter in the nested family
					doc.FamilyManager.AssociateElementParameterToFamilyParameter(famParameter, paramToPush);    //And associates it to the existing family parameter
				}
				catch (Exception) { }
				tw.Commit();
			}
		}
		//Retrieve the parameter from the nested family
		private Parameter GetNestedParameter(FamilyInstance familyInstance, string name)
		{
			Parameter parameter = familyInstance.LookupParameter(name); //Try to return an Instance parameter
			if (parameter == null) parameter = familyInstance.Symbol.LookupParameter(name); //Try to return a Type parameter
			return parameter;
		}
		//Gets a SharedParameter
		private ExternalDefinition GetSharedParameter(Autodesk.Revit.ApplicationServices.Application app, string name)
		{
			DefinitionFile defFile = app.OpenSharedParameterFile();
			if (defFile == null)
			{
				DialogUtils.Failure("Error", $"Shared parameters file does not exist. You are trying to push a Shared Parameter into another Family - make sure you have setup a Shared Parameters file.");
				return null;
				//throw new Exception("No SharedParameter File!");
			}

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

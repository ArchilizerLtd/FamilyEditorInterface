using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyEditorInterface.Associate.WPFSelectParameters.Model;
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
	/// ***** Pulls Parameters from a nested family into the current family *****
	/// 1. Open the nested family
	/// 2. Read all existing user-defined parameters (skip built-in)
	/// 3. UI allows the user to select parameters
	/// 4. Back to main family, creates the parameters
	/// 5. Optional (SHIFT) - associates the newly created parameters
	/// </summary>
	public class PullParameters
	{
		#region Properties & Fields
		private Autodesk.Revit.ApplicationServices.Application app; //Revit Application
		private UIDocument uidoc;   //Current Revit UIDocument
		private Document doc;   //Active (Family) Document
		private Document nestedDoc;	//The Family Document of the User-selected Nested Family
		private FamilyInstance familyInstance;	//The User-selected Family Instance
		private FamilyManager familyManager;    //The FamilyManager of this Document. Contains everything about Parameters
		private Dictionary<FamilyParameter, bool> familyParameters; //Contains all FamilyParameters in the current Document
		private List<ParameterSelectorModel> pullParameters = null;    //User selected list of parameters to be pushed in the selected Nested family
		private string AlertMessage;	//Contains all warnings to be displayed at the end of the run
		private string SuccessMessage;    //Contains all the successfully pulled parameters to be displayed at the end of the run
		#endregion

		#region Constructors & Initializers
		/// <summary>
		/// The public constructor. Initializes starting parameters
		/// </summary>
		/// <param name="app">Revit Application</param>
		/// <param name="uidoc">Current Revit UIDocument</param>
		/// <param name="doc">Active (Family) Document</param>
		public PullParameters(Autodesk.Revit.ApplicationServices.Application app, UIDocument uidoc, Document doc)
		{
			this.app = app;
			this.uidoc = uidoc;
			this.doc = doc;
			this.nestedDoc = GetNestedDocument();	//Retrieves a nested family from user selection

			if (nestedDoc == null) return;	//We need a nested family to execute this plugin

			Initialize();
		}
		//Initializes starting parameters
		private void Initialize()
		{
			familyManager = doc.FamilyManager;
			familyParameters = new Dictionary<FamilyParameter, bool>();

			foreach (FamilyParameter famParam in nestedDoc.FamilyManager.Parameters)
			{
				familyParameters[famParam] = Utils.ParameterExist(famParam, familyManager.Parameters); //Fills in all FamilyParameters in the current document
			}

			GetPullParamters(); //User Select all parameters to be transfered
			CloseNestedDocument();	//Finally, close the nested document as we don't need it anymore
		}


		//Envokes the UI to select FamilyParameters to be pushed
		private void GetPullParamters()
		{
			var vm = new ParameterSelectorViewModel(familyParameters, "Pull");  //Initializes a new view and passes in this ViewModel as a DataContext
			vm.Show();  //Show the UI
			pullParameters = vm.selectedFamilyParameters; //Collect all user-selected parameters
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// The Main method. Pushes selected parameters into a nested family
		/// </summary>
		public void Pull()
		{
			if (!doc.IsFamilyDocument) return;  //Only execute in FamilyDocument
			if (pullParameters == null) return; //No parameter to be pushed, return

			foreach (ParameterSelectorModel parameterToPull in pullParameters)
			{
				ExecutePullParamters(parameterToPull);  //Execute Push Parameter for each Selection and each Selected Paramter
			}

			if (!String.IsNullOrEmpty(AlertMessage)) DialogUtils.Alert("Warning", AlertMessage);	//Finally, alert the user if we had any issues
			if (!String.IsNullOrEmpty(SuccessMessage)) DialogUtils.OK("Pull successful", SuccessMessage);  //And, issue an OK message the user for all the successfully processed parameters
		}
		#endregion


		#region Internal Methods
		//Close the nested document
		private void CloseNestedDocument()
		{
			nestedDoc.Close(false);	//Close the nested document without saving
		}
		//Retrieves a Nested Family Documnet from User Selection
		private Document GetNestedDocument()
		{
			var selection = Utils.PickObject(uidoc, "Pick nested family to pull parameters from.");
			if (selection == null) return null;

			var familyInstance = doc.GetElement(selection.ElementId) as FamilyInstance;   //Cast the current selection as a FamilyInstance
			if (familyInstance == null)
			{
				DialogUtils.Alert("Warning", "No nested family selected"); //If the selection is not a family instance, issue a warning and fail
				return null;
			}
			var nestedFamily = familyInstance.Symbol.Family; //Get the family from the selection
			var nestedFamilyDocument = doc.EditFamily(nestedFamily);    //Open the family

			this.familyInstance = familyInstance; //remember the family instance to use it later

			return nestedFamilyDocument;
		}
		//Push parameter into selected FamilyInstance
		private void ExecutePullParamters(ParameterSelectorModel parameterToPull)
		{
			var exist = RemoveExistingParameter(parameterToPull);   //Check if existing project parameter 
			if(String.IsNullOrEmpty(exist))
			{
				var parameter = AddParameter(parameterToPull);    //Add the Parameter to the Nested Family
				if (parameter != null) SuccessMessage += $"{parameter.Definition.Name} created sucessfully{Environment.NewLine}";   //Report the good news
			}
			else
			{
				AlertMessage += exist;
			}
		}
		//Remove existing parameter
		private string RemoveExistingParameter(ParameterSelectorModel paramToPull)
		{
			//If the parameter already exists, return a alert message
			foreach (FamilyParameter familyParameter in familyManager.Parameters)   //Go through each parameter in the push document
			{
				if (familyParameter.Definition.Name.Equals(paramToPull.Name))
				{
					return $"Parameter '{paramToPull.Name}' alerady exists in the current Family and will be skipped.{Environment.NewLine}";
				}
			}
			return "";
		}
		//Adds a Project or Shared Parameter depending on the original parameter to be pushed
		private FamilyParameter AddParameter(ParameterSelectorModel parameterToPull)
		{
			if (parameterToPull.IsShared)
			{
				ExternalDefinition sharedParameter = GetSharedParameter(app, parameterToPull.Name); //Get the ExternalDefinition, in case it's a Shared Parameter
				return AddSharedParameter(sharedParameter, parameterToPull);    //Adds a Shared Parameter
			}
			else
			{
				return AddProjectParameter(parameterToPull);   //Adds a (normal) Project Parameter
			}
		}
		//Creates a regular parameter
		private FamilyParameter AddProjectParameter(ParameterSelectorModel parameterToPull)
		{
			FamilyParameter parameter = null;
			try
			{
				using (Transaction ft = new Transaction(doc, "Pull parameter"))
				{
					ft.Start();
					//Adds a Parameter to the Family
					parameter = familyManager.AddParameter(parameterToPull.Name, parameterToPull.ParameterGroup, parameterToPull.ParameterType, parameterToPull.IsInstance);
					ft.Commit();
				}
			}
			catch (Exception ex) { DialogUtils.Alert("Failed", ex.Message); }
			return parameter;
		}
		//Adds a parameter as a SharedParemter
		private FamilyParameter AddSharedParameter(ExternalDefinition sharedParameter, ParameterSelectorModel paramToPull)
		{
			FamilyParameter parameter = null;
			// Add the parameter to the family
			try
			{
				using (Transaction ft = new Transaction(doc, "Pull shared parameter"))
				{
					ft.Start();
					//Adds a Shared Parameter to the Family
					parameter = familyManager.AddParameter(sharedParameter, paramToPull.ParameterGroup, paramToPull.IsInstance);
					ft.Commit();
				}
			}
			catch (Exception) { }
			return parameter;
		}
		//Retrieve the parameter from the nested family
		private Parameter GetNestedParameter(string name)
		{
			Parameter parameter = familyInstance.LookupParameter(name);	//Try to return an Instance parameter
			if (parameter == null) parameter = familyInstance.Symbol.LookupParameter(name);	//Try to return a Type parameter
			return parameter;
		}
		//Associate the parameters
		//NOT IN USE IN THIS VERSION
		private string AssociateParameters(FamilyParameter newParameter)
		{
			using (Transaction tw = new Transaction(doc, "Wire parameters"))
			{
				tw.Start();
				try
				{
					var nestedParameter = GetNestedParameter(newParameter.Definition.Name);    //Finds the parameter in the nested family
					SetDocumentParameter(newParameter, nestedParameter);	//Set the value first, we don't want to lose it
					
					doc.FamilyManager.AssociateElementParameterToFamilyParameter(nestedParameter, newParameter);    //And associates it to the existing family parameter. The second parameter belongs to the Main family
					return $"'{newParameter.Definition.Name}' was processed successfully.{Environment.NewLine}";	//Helps to populates the SuccessMessage
				}
				catch (Exception ex) { DialogUtils.Alert("Error", ex.Message); return null; }
				tw.Commit();
			}
		}
		//Sets the value of the family parameter based on a parameter value
		private void SetDocumentParameter(FamilyParameter docParameter, Parameter famParameter)
		{
			switch (famParameter.StorageType)
			{
				case (StorageType.Double):
					var value = famParameter.AsDouble();
					familyManager.Set(docParameter, famParameter.AsDouble());
					break;
				case (StorageType.Integer):
					familyManager.Set(docParameter, famParameter.AsInteger());
					break;
				case (StorageType.String):
					familyManager.Set(docParameter, famParameter.AsString());
					break;
				case (StorageType.ElementId):
					familyManager.Set(docParameter, famParameter.AsElementId());
					break;
				case (StorageType.None):
					break;
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

			if (v == null || v.Count() < 1) return null;    //There is no Shared parameter with this name

			ExternalDefinition def = v.First();
			return def;
		}
		#endregion
	}
}

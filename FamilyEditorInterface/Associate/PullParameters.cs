using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyEditorInterface.Associate.WPFSelectParameters.ViewModel;
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
		private List<FamilyParameter> familyParameters; //Contains all FamilyParameters in the current Document
		private List<FamilyParameter> pullParameters = null;    //User selected list of parameters to be pushed in the selected Nested family
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
			familyManager = nestedDoc.FamilyManager;
			familyParameters = new List<FamilyParameter>();

			foreach (FamilyParameter famParam in familyManager.Parameters)
			{
				familyParameters.Add(famParam); //Fills in all FamilyParameters in the current document
			}

			GetPullParamters(); //User Select all parameters to be transfered
		}
		//Envokes the UI to select FamilyParameters to be pushed
		private void GetPullParamters()
		{
			var vm = new ParameterSelectorViewModel(familyParameters);  //Initializes a new view and passes in this ViewModel as a DataContext
			vm.Show();  //Show the UI
			pullParameters = vm.selectedParameters; //Collect all user-selected parameters
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

			foreach (FamilyParameter parameterToPull in pullParameters)
			{
				ExecutePullParamters(parameterToPull);  //Execute Push Parameter for each Selection and each Selected Paramter
			}
		}
		#endregion


		#region Internal Methods
		//Retrieves a Nested Family Documnet from User Selection
		private Document GetNestedDocument()
		{
			var selection = Utils.PickObject(uidoc, "Pick nested family to pull parameters from."); 

			var familyInstance = doc.GetElement(selection.ElementId) as FamilyInstance;   //Cast the current selection as a FamilyInstance
			if (familyInstance == null)
			{
				Utils.Alert("Warning", "No nested family selected"); //If the selection is not a family instance, issue a warning and fail
				return null;
			}
			var nestedFamily = familyInstance.Symbol.Family; //Get the family from the selection
			var nestedFamilyDocument = doc.EditFamily(nestedFamily);    //Open the family

			this.familyInstance = familyInstance; //remember the family instance to use it later

			return nestedFamilyDocument;
		}
		//Push parameter into selected FamilyInstance
		private void ExecutePullParamters(FamilyParameter parameterToPull)
		{
			RemoveExistingParameter(parameterToPull);   //Remove the existing project parameter 
			AddParameter(parameterToPull);    //Add the Parameter to the Nested Family
			AssociateParameters(familyInstance, parameterToPull);   //Associate the newly created nested family parameters to the current family parameters
		}
		//Remove existing parameter
		private void RemoveExistingParameter(FamilyParameter paramToPull)
		{
			//If the parameter already exists, remove the parameter before adding it again
			//Issue a warning maybe? Or skip this?
			using (Transaction ft = new Transaction(doc, "Remove parameter"))
			{
				ft.Start();
				try
				{
					foreach (FamilyParameter familyParameter in familyManager.Parameters)   //Go through each parameter in the push document
					{
						if (familyParameter.Definition.Name.Equals(paramToPull.Definition.Name))
						{
							familyManager.RemoveParameter(familyParameter); //If the parameter already exists, remove it. Or should we skip it?
							break;
						}
					}
				}			
				catch (Exception ex)
				{
					Utils.Alert("Failed", ex.Message);
				}
				ft.Commit();
			}
		}
		//Adds a Project or Shared Parameter depending on the original parameter to be pushed
		private void AddParameter(FamilyParameter parameterToPull)
		{
			if (parameterToPull.IsShared)
			{
				ExternalDefinition sharedParameter = GetSharedParameter(app, parameterToPull.Definition.Name); //Get the ExternalDefinition, in case it's a Shared Parameter
				AddSharedParameter(sharedParameter, parameterToPull);    //Adds a Shared Parameter
			}
			else
			{
				AddProjectParameter(parameterToPull);   //Adds a (normal) Project Parameter
			}
		}
		//Creates a regular parameter
		private void AddProjectParameter(FamilyParameter parameterToPull)
		{
			try
			{
				using (Transaction ft = new Transaction(doc, "Pull parameter"))
				{
					ft.Start();
					//Adds a Parameter to the Family
					familyManager.AddParameter(parameterToPull.Definition.Name, parameterToPull.Definition.ParameterGroup, parameterToPull.Definition.ParameterType, parameterToPull.IsInstance);
					ft.Commit();
				}
			}
			catch (Exception ex) { Utils.Alert("Failed", ex.Message); }
		}
		//Adds a parameter as a SharedParemter
		private void AddSharedParameter(ExternalDefinition sharedParameter, FamilyParameter paramToPull)
		{
			// Add the parameter to the family
			try
			{
				using (Transaction ft = new Transaction(doc, "Pull shared parameter"))
				{
					ft.Start();
					//Adds a Shared Parameter to the Family
					familyManager.AddParameter(sharedParameter, paramToPull.Definition.ParameterGroup, paramToPull.IsInstance);
					ft.Commit();
				}
			}
			catch (Exception) { }
		}
		//Associate the parameters
		private void AssociateParameters(FamilyInstance pushFamInstance, FamilyParameter paramToPull)
		{
			using (Transaction tw = new Transaction(doc, "Wire parameters"))
			{
				tw.Start();
				try
				{
					var famParameter = pushFamInstance.LookupParameter(paramToPull.Definition.Name);    //Finds the newly created parameter in the nested family
					doc.FamilyManager.AssociateElementParameterToFamilyParameter(famParameter, paramToPull);    //And associates it to the existing family parameter
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

			if (v == null || v.Count() < 1) return null;    //There is no Shared parameter with this name

			ExternalDefinition def = v.First();
			return def;
		}
		#endregion
	}
}

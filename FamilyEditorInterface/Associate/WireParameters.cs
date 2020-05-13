using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.Associate
{
	/// <summary>
	/// ***** Associates parameter between two families *****
	/// 1. Collects all parameters in the main family and in the nested family
	/// 2. Associates parameters with the same name
	/// </summary>
    public class WireParameters
    {
        #region Properties & Fields
        private enum ParamType { Type, Instance }
		private UIDocument uidoc;	
		private Document doc;
		private FamilyManager familyManager;
		private SortedList<string, FamilyParameter> famParam;
        #endregion

        #region Constructors & Initializers
        //Constructor
        public WireParameters(UIDocument uidoc, Document doc)
        {
			this.uidoc = uidoc;
			this.doc = doc;

			Initialize();
        }
		//Setup the familyManager, collect all familyParameters from it
		private void Initialize()
		{
			familyManager = doc.FamilyManager;
			famParam = new SortedList<string, FamilyParameter>();

			foreach (FamilyParameter fp in familyManager.Parameters)
			{
				famParam.Add(fp.Definition.Name, fp);	//Collect all parameters in the current Family in a sorted list
			}
		}
        #endregion

        #region Methods
        /// <summary>
        /// Associate the parameters of the selected family to the current family
        /// </summary>
        public void Wire()
        {
			if (!doc.IsFamilyDocument || !famParam.Any()) return;	//Only work in Family Document that has parameters to associate with

			var wireFamily = uidoc.Selection.PickObject(ObjectType.Element, "Pick Object"); //Get the family to associate to 
			if (wireFamily == null) return;
			var fam = doc.GetElement(wireFamily) as FamilyInstance;

			AssociateParameters(fam, ParamType.Instance);	//Associate all possible Instance parameters
			AssociateParameters(fam, ParamType.Type);	//Associate all possible Type
		}
		//Dispatch based on Parmater Type
		private void AssociateParameters(FamilyInstance family, ParamType pType)
		{
			switch (pType)
			{
				case (ParamType.Instance):
					AssociateParameters(family, family.Symbol);	//Instance Parameters
					break;
				case (ParamType.Type):
					AssociateParameters(family, family);	//Type Parameters
					break;
			}
		}
		//The MAIN method for this class 
		//Associate parameters bewteen the current and the selected family
		private void AssociateParameters(FamilyInstance family, Element element)
		{
			using (Transaction t = new Transaction(doc, "Wire parameters"))
			{
				t.Start();
				foreach (Parameter param in element.Parameters)
				{
					try
					{
						if (famParam.TryGetValue(param.Definition.Name, out var famParameter))
							doc.FamilyManager.AssociateElementParameterToFamilyParameter(param, famParameter);  //The MAIN method - associate the element parameters from both families
					}
					catch (Exception) { }
				}
				t.Commit();
			}
		}
        #endregion 
    }	
}

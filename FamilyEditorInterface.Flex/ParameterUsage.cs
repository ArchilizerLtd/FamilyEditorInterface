using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace FamilyEditorInterface.Flex
{
    /// <summary>
    /// Finds the family parameters that can affect the family: dimension and array labels,
    /// formula inputs, and parameters associated with element parameters (visibility, material, nested families).
    /// Same rule as the add-in's "used" flag. Parameters outside this set (e.g. analytical thermal values)
    /// can't break geometry, so flexing them only adds steps.
    /// </summary>
    internal static class ParameterUsage
    {
        public static HashSet<long> UsedParameterIds(Document doc)
        {
            var used = new HashSet<long>();
            FamilyManager mgr = doc.FamilyManager;

            foreach (Dimension dim in new FilteredElementCollector(doc)
                         .OfCategory(BuiltInCategory.OST_Dimensions)
                         .WhereElementIsNotElementType()
                         .OfType<Dimension>())
            {
                try { if (dim.FamilyLabel != null) used.Add(dim.FamilyLabel.Id.Value); }
                catch { /* some dimension types don't support labels */ }
            }

            foreach (BaseArray array in new FilteredElementCollector(doc)
                         .OfCategory(BuiltInCategory.OST_IOSArrays)
                         .WhereElementIsNotElementType()
                         .OfType<BaseArray>())
            {
                try { if (array.Label != null) used.Add(array.Label.Id.Value); }
                catch { }
            }

            var parameters = mgr.Parameters.Cast<FamilyParameter>().ToList();
            var formulas = parameters.Where(p => !string.IsNullOrEmpty(p.Formula)).Select(p => p.Formula).ToList();

            foreach (FamilyParameter fp in parameters)
            {
                if (!fp.AssociatedParameters.IsEmpty) used.Add(fp.Id.Value);
                // Name containment can over-match ("Width" in "Rough Width"); flexing one extra parameter is harmless.
                else if (formulas.Any(f => f.Contains(fp.Definition.Name))) used.Add(fp.Id.Value);
            }
            return used;
        }
    }
}

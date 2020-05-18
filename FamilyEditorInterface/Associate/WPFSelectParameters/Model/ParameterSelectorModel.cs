using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.Associate.WPFSelectParameters.Model
{
    public class ParameterSelectorModel
    {
        public FamilyParameter Parameter { get; internal set; }
        public BuiltInParameterGroup ParameterGroup { get; internal set; }
        public ParameterType ParameterType { get; internal set; }
        public string Name { get; internal set; }
        public string Group { get; internal set; }
        public bool IsShared { get; internal set; }
        public bool IsInstance { get; internal set; }
    }
}

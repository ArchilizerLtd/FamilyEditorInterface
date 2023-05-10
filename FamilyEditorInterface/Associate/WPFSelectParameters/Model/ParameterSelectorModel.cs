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

#if RELEASE2020 || RELEASE2021 

        public BuiltInParameterGroup ParameterGroup { get; internal set; }

        public ParameterType ParameterType { get; internal set; }

#elif RELEASE2022 || RELEASE2023 || RELEASE2024
        public ForgeTypeId ParameterGroup { get; internal set; }
        public ForgeTypeId ParameterType { get; internal set; }
#endif

        public string Name { get; internal set; }
        public string Group { get; internal set; }
        public bool IsShared { get; internal set; }
        public bool IsInstance { get; internal set; }
        public bool Exists { get; set; }
    }
}

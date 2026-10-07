using System;
using System.Collections.Generic;

namespace FamilyEditorInterface.Flex
{
    /// <summary>
    /// What to test. For now the plan is an explicit list of steps;
    /// samplers that generate steps from parameter ranges come next (docs/flex-engine, F4).
    /// </summary>
    public sealed class FlexPlan
    {
        public List<FlexStep> Steps { get; init; } = new List<FlexStep>();

        public int MaxSteps { get; init; } = 500;
        public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>Record warnings in the report (true for QA, false for interactive use).</summary>
        public bool RecordWarnings { get; init; } = true;
    }

    /// <summary>
    /// One assignment of values to one or more parameters, applied to one family type.
    /// Every step starts from the family's original state.
    /// </summary>
    public sealed class FlexStep
    {
        /// <summary>Family type name. Null means the type that is current when the run starts.</summary>
        public string Type { get; init; }

        public List<ParameterAssignment> Assignments { get; init; } = new List<ParameterAssignment>();
    }

    /// <summary>
    /// A parameter value in Revit internal units (feet, radians).
    /// Integer and Yes/No parameters take the value rounded to the nearest integer.
    /// </summary>
    public sealed class ParameterAssignment
    {
        public string Name { get; init; }
        public double Value { get; init; }

        public ParameterAssignment() { }

        public ParameterAssignment(string name, double value)
        {
            Name = name;
            Value = value;
        }
    }
}

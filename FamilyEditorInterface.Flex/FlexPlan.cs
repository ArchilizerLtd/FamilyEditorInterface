using System;
using System.Collections.Generic;

namespace FamilyEditorInterface.Flex
{
    /// <summary>
    /// What to test. Steps come from two places: the samplers, which generate them from parameter ranges,
    /// and the explicit <see cref="Steps"/> list, which is appended as is.
    /// </summary>
    public sealed class FlexPlan
    {
        public int Seed { get; init; } = 0;
        public int MaxSteps { get; init; } = 500;
        public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>Run against every family type. When false, only the type current at the start of the run.</summary>
        public bool AllTypes { get; init; } = false;

        /// <summary>
        /// Only sample parameters that can affect the family (labels, formulas, associations).
        /// Parameters listed in <see cref="Parameters"/> are always sampled.
        /// </summary>
        public bool OnlyUsedParameters { get; init; } = true;

        /// <summary>Per-parameter ranges. Parameters not listed fall back to <see cref="Defaults"/>.</summary>
        public List<ParameterRange> Parameters { get; init; } = new List<ParameterRange>();
        public RangeDefaults Defaults { get; init; } = new RangeDefaults();

        /// <summary>How to pick values. Samplers run in order; duplicate steps are removed.</summary>
        public List<SamplerSpec> Samplers { get; init; } = new List<SamplerSpec>
        {
            new SamplerSpec(SamplerKind.Defaults),
            new SamplerSpec(SamplerKind.Corners),
            new SamplerSpec(SamplerKind.OneAtATime, Steps: 3)
        };

        /// <summary>Extra hand-written steps, appended after the sampled ones.</summary>
        public List<FlexStep> Steps { get; init; } = new List<FlexStep>();

        /// <summary>Record warnings in the report (true for QA, false for interactive use).</summary>
        public bool RecordWarnings { get; init; } = true;
    }

    /// <summary>
    /// The range a parameter must survive, in Revit internal units (feet, radians).
    /// <see cref="Values"/>, when given, replaces Min/Max.
    /// </summary>
    public sealed record ParameterRange(
        string Name,
        double? Min = null,
        double? Max = null,
        double? Step = null,
        List<double> Values = null,
        bool Skip = false);

    /// <summary>
    /// Ranges for parameters the plan doesn't list: a scale around the current value.
    /// Parameters whose current value is zero have no default range and are not sampled.
    /// </summary>
    public sealed record RangeDefaults(double ScaleMin = 0.5, double ScaleMax = 2.0);

    public enum SamplerKind
    {
        /// <summary>One step per type with no changes: does the family regenerate as it is?</summary>
        Defaults,
        /// <summary>All parameters at min, all at max, then each parameter alone at min and at max.</summary>
        Corners,
        /// <summary>Each parameter alone across <c>Steps</c> evenly spaced values, the others at their current value.</summary>
        OneAtATime,
        /// <summary><c>Count</c> steps with every parameter at a seeded random value inside its range.</summary>
        Random
    }

    public sealed record SamplerSpec(SamplerKind Kind, int Steps = 0, int Count = 0)
    {
        public override string ToString() =>
            Kind switch
            {
                SamplerKind.OneAtATime => $"{Kind}:{Steps}",
                SamplerKind.Random => $"{Kind}:{Count}",
                _ => Kind.ToString()
            };
    }

    /// <summary>
    /// One assignment of values to one or more parameters, applied to one family type.
    /// Every step starts from the family's original state. A step with no assignments
    /// only regenerates the type as it is.
    /// </summary>
    public sealed class FlexStep
    {
        /// <summary>Family type name. Null means the type that is current when the run starts.</summary>
        public string Type { get; init; }

        /// <summary>What the step is testing, e.g. "all at minimum" or "Width at maximum". Optional.</summary>
        public string Label { get; init; }

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

        /// <summary>The value formatted in the document's units. Filled in by the engine for the report.</summary>
        public string Display { get; init; }

        public ParameterAssignment() { }

        public ParameterAssignment(string name, double value, string display = null)
        {
            Name = name;
            Value = value;
            Display = display;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FamilyEditorInterface.Flex
{
    public enum ParameterKind { Length, Angle, Number, Integer, YesNo }

    /// <summary>
    /// What the samplers need to know about a parameter. Kept free of Revit types so sampling
    /// is plain logic: the engine fills these in from the FamilyManager.
    /// </summary>
    public sealed record ParameterInfo(string Name, ParameterKind Kind, double CurrentValue);

    /// <summary>A parameter that was left out of sampling or of a step, and why.</summary>
    public sealed record SkippedParameter(string Name, string Reason);

    /// <summary>Turns parameter ranges and sampler specs into flex steps. No Revit dependency.</summary>
    public static class Sampling
    {
        /// <summary>A resolved range: either a continuous [Min, Max] (snapped to Step) or an explicit list.</summary>
        public sealed record ResolvedRange(ParameterInfo Parameter, double Min, double Max, double? Step, List<double> Values)
        {
            public double Clamp(double value) => Snap(Math.Min(Max, Math.Max(Min, value)));

            public double Snap(double value)
            {
                if (Parameter.Kind == ParameterKind.Integer || Parameter.Kind == ParameterKind.YesNo) return Math.Round(value, MidpointRounding.AwayFromZero);
                if (Step is double step && step > 0) return Math.Min(Max, Min + Math.Round((value - Min) / step) * step);
                return value;
            }

            /// <summary>n evenly spaced values from Min to Max, or the explicit list.</summary>
            public IEnumerable<double> Spread(int n)
            {
                if (Values != null) return Values;
                if (n <= 1 || Max <= Min) return new[] { Min };
                return Enumerable.Range(0, n).Select(i => Snap(Min + (Max - Min) * i / (n - 1))).Distinct();
            }

            public double Random(Random rng) =>
                Values != null ? Values[rng.Next(Values.Count)] : Snap(Min + rng.NextDouble() * (Max - Min));

            public double Low => Values?.Min() ?? Min;
            public double High => Values?.Max() ?? Max;
        }

        /// <summary>
        /// Works out the range of each parameter from the plan, falling back to a scale around the current value.
        /// </summary>
        public static List<ResolvedRange> ResolveRanges(IEnumerable<ParameterInfo> parameters, FlexPlan plan,
                                                        List<SkippedParameter> unsampled)
        {
            var explicitRanges = plan.Parameters.ToDictionary(p => p.Name, StringComparer.Ordinal);
            var ranges = new List<ResolvedRange>();

            foreach (ParameterInfo p in parameters)
            {
                explicitRanges.TryGetValue(p.Name, out ParameterRange given);
                if (given?.Skip == true)
                {
                    unsampled.Add(new SkippedParameter(p.Name, "skipped-by-plan"));
                    continue;
                }

                if (given?.Values is { Count: > 0 } values)
                {
                    ranges.Add(new ResolvedRange(p, values.Min(), values.Max(), null, values));
                    continue;
                }

                if (p.Kind == ParameterKind.YesNo)
                {
                    ranges.Add(new ResolvedRange(p, 0, 1, null, null));
                    continue;
                }

                double? min = given?.Min;
                double? max = given?.Max;
                if (min == null || max == null)
                {
                    if (Math.Abs(p.CurrentValue) < 1e-9)
                    {
                        // A scale around zero is no range at all.
                        unsampled.Add(new SkippedParameter(p.Name, "no-range"));
                        continue;
                    }
                    double a = p.CurrentValue * plan.Defaults.ScaleMin;
                    double b = p.CurrentValue * plan.Defaults.ScaleMax;
                    min ??= Math.Min(a, b);   // negative current values flip the scale
                    max ??= Math.Max(a, b);
                }

                if (p.Kind == ParameterKind.Integer)
                {
                    min = Math.Ceiling(min.Value);
                    max = Math.Floor(max.Value);
                }

                if (max < min)
                {
                    unsampled.Add(new SkippedParameter(p.Name, "empty-range"));
                    continue;
                }

                ranges.Add(new ResolvedRange(p, min.Value, max.Value, given?.Step, null));
            }
            return ranges;
        }

        /// <summary>Generates the steps for one family type. Duplicates are removed, order is kept.</summary>
        public static List<FlexStep> GenerateSteps(string type, IReadOnlyList<ResolvedRange> ranges,
                                                   IEnumerable<SamplerSpec> samplers, Random rng)
        {
            var steps = new List<FlexStep>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            void Add(IEnumerable<(ResolvedRange Range, double Value)> values)
            {
                var assignments = values
                    .Where(v => !SameValue(v.Value, v.Range.Parameter.CurrentValue))   // only real changes
                    .Select(v => new ParameterAssignment(v.Range.Parameter.Name, v.Value))
                    .ToList();
                if (seen.Add(Key(assignments))) steps.Add(new FlexStep { Type = type, Assignments = assignments });
            }

            foreach (SamplerSpec sampler in samplers)
            {
                switch (sampler.Kind)
                {
                    case SamplerKind.Defaults:
                        Add(Enumerable.Empty<(ResolvedRange, double)>());
                        break;

                    case SamplerKind.Corners:
                        if (ranges.Count == 0) break;
                        Add(ranges.Select(r => (r, r.Low)));
                        Add(ranges.Select(r => (r, r.High)));
                        foreach (ResolvedRange r in ranges)
                        {
                            Add(new[] { (r, r.Low) });
                            Add(new[] { (r, r.High) });
                        }
                        break;

                    case SamplerKind.OneAtATime:
                        foreach (ResolvedRange r in ranges)
                            foreach (double v in r.Spread(Math.Max(2, sampler.Steps)))
                                Add(new[] { (r, v) });
                        break;

                    case SamplerKind.Random:
                        if (ranges.Count == 0) break;
                        for (int i = 0; i < Math.Max(1, sampler.Count); i++)
                            Add(ranges.Select(r => (r, r.Random(rng))).ToList());
                        break;
                }
            }

            // A step that ends up changing nothing is a baseline. Keep it only if a Defaults sampler asked for one.
            bool wantsBaseline = samplers.Any(s => s.Kind == SamplerKind.Defaults);
            return steps.Where(s => s.Assignments.Count > 0 || wantsBaseline).ToList();
        }

        private static bool SameValue(double a, double b) => Math.Abs(a - b) <= 1e-9 * Math.Max(1, Math.Abs(b));

        private static string Key(List<ParameterAssignment> assignments) =>
            string.Join(";", assignments.OrderBy(a => a.Name, StringComparer.Ordinal)
                                        .Select(a => a.Name + "=" + a.Value.ToString("R", CultureInfo.InvariantCulture)));
    }
}

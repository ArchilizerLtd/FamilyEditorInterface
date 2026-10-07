using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FamilyEditorInterface.Flex
{
    /// <summary>Optional progress sink: the add-in can drive a progress bar, Design Automation can log.</summary>
    public interface IFlexProgress
    {
        void StepStarted(int index, int total, FlexStep step);
        void StepFinished(FlexStepResult result);
    }

    public static class FlexEngine
    {
        /// <summary>
        /// Runs the plan against a family document and returns a report.
        /// The document is left unchanged: every step runs in its own transaction group that is rolled back,
        /// and the whole run is wrapped in an outer group that is rolled back as well.
        /// </summary>
        /// <exception cref="ArgumentException">The document is not a family document.</exception>
        /// <exception cref="InvalidOperationException">The document already has an open transaction.</exception>
        public static FlexReport Run(Document familyDoc, FlexPlan plan,
                                     IFlexProgress progress = null,
                                     CancellationToken cancel = default)
        {
            if (familyDoc == null) throw new ArgumentNullException(nameof(familyDoc));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (!familyDoc.IsFamilyDocument) throw new ArgumentException("Flex runs only on family documents.", nameof(familyDoc));
            if (familyDoc.IsModifiable) throw new InvalidOperationException("Flex cannot start while a transaction is open.");

            FamilyManager mgr = familyDoc.FamilyManager;
            var report = new FlexReport
            {
                FamilyName = familyDoc.Title,
                Category = familyDoc.OwnerFamily?.FamilyCategory?.Name,
                RevitVersion = familyDoc.Application.VersionNumber,
                Seed = plan.Seed,
                Samplers = plan.Samplers.Select(s => s.ToString()).ToList()
            };

            List<FlexStep> steps = Expand(familyDoc, plan, report.SkippedParameters);
            report.PlannedSteps = steps.Count;

            var before = Fingerprint.Capture(familyDoc);
            var clock = Stopwatch.StartNew();
            int total = Math.Min(steps.Count, plan.MaxSteps);
            report.Truncated = steps.Count > plan.MaxSteps;

            using (var run = new TransactionGroup(familyDoc, "Flex"))
            {
                run.Start();
                try
                {
                    for (int i = 0; i < total; i++)
                    {
                        if (cancel.IsCancellationRequested || clock.Elapsed > plan.MaxDuration)
                        {
                            report.Truncated = true;
                            break;
                        }

                        FlexStep step = steps[i];
                        progress?.StepStarted(i, total, step);
                        FlexStepResult result = RunStep(familyDoc, mgr, step, i, plan.RecordWarnings);
                        report.Steps.Add(result);
                        progress?.StepFinished(result);
                    }
                }
                finally
                {
                    run.RollBack();
                }
            }

            report.DurationMs = clock.ElapsedMilliseconds;
            report.DocumentRestored = before.Matches(Fingerprint.Capture(familyDoc));
            return report;
        }

        /// <summary>
        /// Works out the full list of steps without running anything:
        /// sampled steps for each type in scope, then the plan's explicit steps.
        /// </summary>
        public static List<FlexStep> Expand(Document familyDoc, FlexPlan plan, List<SkippedParameter> unsampled = null)
        {
            FamilyManager mgr = familyDoc.FamilyManager;
            unsampled ??= new List<SkippedParameter>();
            var rng = new Random(plan.Seed);
            HashSet<long> used = plan.OnlyUsedParameters ? ParameterUsage.UsedParameterIds(familyDoc) : null;
            var listed = new HashSet<string>(plan.Parameters.Select(p => p.Name), StringComparer.Ordinal);
            var steps = new List<FlexStep>();

            var types = new List<FamilyType>();
            if (plan.AllTypes) foreach (FamilyType t in mgr.Types) types.Add(t);
            else if (mgr.CurrentType != null) types.Add(mgr.CurrentType);

            foreach (FamilyType type in types)
            {
                var parameters = new List<ParameterInfo>();
                foreach (FamilyParameter fp in mgr.Parameters)
                {
                    string reason = SkipReason(fp)
                                    ?? (type.HasValue(fp) ? null : "no-value")
                                    ?? (used == null || used.Contains(fp.Id.Value) || listed.Contains(fp.Definition.Name) ? null : "unused");
                    if (reason != null)
                    {
                        unsampled.Add(new SkippedParameter(fp.Definition.Name, reason));
                        continue;
                    }
                    double current = fp.StorageType == StorageType.Integer
                        ? type.AsInteger(fp) ?? 0
                        : type.AsDouble(fp) ?? 0;
                    parameters.Add(new ParameterInfo(fp.Definition.Name, KindOf(fp), current));
                }

                List<Sampling.ResolvedRange> ranges = Sampling.ResolveRanges(parameters, plan, unsampled);
                steps.AddRange(Sampling.GenerateSteps(plan.AllTypes ? type.Name : null, ranges, plan.Samplers, rng));
            }

            steps.AddRange(plan.Steps);

            // The same parameter is listed once per type; report it once.
            List<SkippedParameter> distinct = unsampled.Distinct().OrderBy(u => u.Name, StringComparer.Ordinal).ToList();
            unsampled.Clear();
            unsampled.AddRange(distinct);
            return steps;
        }

        private static FlexStepResult RunStep(Document doc, FamilyManager mgr, FlexStep step, int index, bool recordWarnings)
        {
            var result = new FlexStepResult { Index = index, Label = step.Label, Type = step.Type ?? mgr.CurrentType?.Name };

            // Resolve everything we can before touching the document.
            FamilyType type = null;
            if (step.Type != null)
            {
                type = FindType(mgr, step.Type);
                if (type == null)
                {
                    result.Status = FlexStepStatus.Error;
                    result.Exception = $"Family type '{step.Type}' not found.";
                    return result;
                }
            }

            var toApply = new List<(FamilyParameter Parameter, ParameterAssignment Assignment)>();
            foreach (ParameterAssignment assignment in step.Assignments)
            {
                FamilyParameter fp = mgr.get_Parameter(assignment.Name);
                string reason = SkipReason(fp);
                if (reason != null) result.SkippedParameters.Add(new SkippedParameter(assignment.Name, reason));
                else toApply.Add((fp, assignment));
            }

            // A step with no assignments is a baseline: it just regenerates the type as it is.
            if (toApply.Count == 0 && step.Assignments.Count > 0)
            {
                result.Status = FlexStepStatus.Skipped;
                return result;
            }

            // The step group isolates this step: whatever it commits is rolled back afterwards,
            // so the next step starts from the original state instead of a broken one.
            using (var stepGroup = new TransactionGroup(doc, $"Flex step {index}"))
            {
                stepGroup.Start();
                try
                {
                    using (var trans = new Transaction(doc, $"Flex step {index}"))
                    {
                        var collector = new FailureCollector(recordWarnings);
                        FailureHandlingOptions options = trans.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(collector);
                        options.SetClearAfterRollback(true);
                        trans.SetFailureHandlingOptions(options);

                        trans.Start();
                        try
                        {
                            if (type != null) mgr.CurrentType = type;
                            foreach (var (parameter, assignment) in toApply)
                            {
                                Apply(mgr, parameter, assignment.Value);
                                result.Applied.Add(new ParameterAssignment(assignment.Name, assignment.Value,
                                                                           Format(doc, parameter, assignment.Value)));
                            }
                            doc.Regenerate();
                        }
                        catch (Exception ex)
                        {
                            // Regeneration problems usually arrive as failures at commit,
                            // but invalid values and some constraint conflicts throw here.
                            if (trans.HasStarted() && !trans.HasEnded()) trans.RollBack();
                            result.Status = FlexStepStatus.Error;
                            result.Exception = ex.Message;
                            result.Failures.AddRange(collector.Failures);
                            return result;
                        }

                        TransactionStatus status = trans.Commit();
                        result.Failures.AddRange(collector.Failures);

                        if (collector.HasErrors || status != TransactionStatus.Committed)
                            result.Status = FlexStepStatus.Failed;
                        else if (result.Failures.Count > 0)
                            result.Status = FlexStepStatus.PassedWithWarnings;
                        else
                            result.Status = FlexStepStatus.Passed;
                    }
                }
                finally
                {
                    stepGroup.RollBack();
                }
            }

            return result;
        }

        private static string SkipReason(FamilyParameter fp)
        {
            if (fp == null) return "not-found";
            if (fp.IsDeterminedByFormula) return "determined-by-formula";
            if (fp.IsReporting) return "reporting";
            if (fp.StorageType != StorageType.Double && fp.StorageType != StorageType.Integer) return "unsupported-storage-type";
            return null;
        }

        private static void Apply(FamilyManager mgr, FamilyParameter fp, double value)
        {
            if (fp.StorageType == StorageType.Integer) mgr.Set(fp, (int)Math.Round(value, MidpointRounding.AwayFromZero));
            else mgr.Set(fp, value);
        }

        private static ParameterKind KindOf(FamilyParameter fp)
        {
            ForgeTypeId spec = fp.Definition.GetDataType();
            if (spec.Equals(SpecTypeId.Length)) return ParameterKind.Length;
            if (spec.Equals(SpecTypeId.Angle)) return ParameterKind.Angle;
            if (spec.Equals(SpecTypeId.Boolean.YesNo)) return ParameterKind.YesNo;
            return fp.StorageType == StorageType.Integer ? ParameterKind.Integer : ParameterKind.Number;
        }

        /// <summary>The value as the user would see it in this document's units, e.g. "600 mm".</summary>
        private static string Format(Document doc, FamilyParameter fp, double value)
        {
            try
            {
                switch (KindOf(fp))
                {
                    case ParameterKind.YesNo: return Math.Round(value) != 0 ? "Yes" : "No";
                    case ParameterKind.Integer: return ((int)Math.Round(value)).ToString();
                    default: return UnitFormatUtils.Format(doc.GetUnits(), fp.Definition.GetDataType(), value, false);
                }
            }
            catch
            {
                return null;
            }
        }

        private static FamilyType FindType(FamilyManager mgr, string name)
        {
            foreach (FamilyType type in mgr.Types)
            {
                if (type.Name == name) return type;
            }
            return null;
        }

        /// <summary>
        /// A cheap snapshot of the family's state (current type and all parameter values of every type)
        /// used to confirm that the run left the document unchanged.
        /// </summary>
        private sealed class Fingerprint
        {
            private string _currentType;
            private int _parameterCount;
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public static Fingerprint Capture(Document doc)
            {
                FamilyManager mgr = doc.FamilyManager;
                var fingerprint = new Fingerprint
                {
                    _currentType = mgr.CurrentType?.Name,
                    _parameterCount = mgr.Parameters.Size
                };

                foreach (FamilyType type in mgr.Types)
                {
                    foreach (FamilyParameter fp in mgr.Parameters)
                    {
                        if (!type.HasValue(fp)) continue;
                        string value = fp.StorageType switch
                        {
                            StorageType.Double => type.AsDouble(fp)?.ToString("R"),
                            StorageType.Integer => type.AsInteger(fp)?.ToString(),
                            StorageType.String => type.AsString(fp),
                            StorageType.ElementId => type.AsElementId(fp)?.Value.ToString(),
                            _ => null
                        };
                        fingerprint._values[$"{type.Name}|{fp.Id.Value}"] = value;
                    }
                }
                return fingerprint;
            }

            public bool Matches(Fingerprint other) =>
                _currentType == other._currentType
                && _parameterCount == other._parameterCount
                && _values.Count == other._values.Count
                && _values.All(kv => other._values.TryGetValue(kv.Key, out string v) && v == kv.Value);
        }
    }
}

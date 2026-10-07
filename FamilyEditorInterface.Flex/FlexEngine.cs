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
                RevitVersion = familyDoc.Application.VersionNumber
            };

            var before = Fingerprint.Capture(familyDoc);
            var clock = Stopwatch.StartNew();
            int total = Math.Min(plan.Steps.Count, plan.MaxSteps);
            report.Truncated = plan.Steps.Count > plan.MaxSteps;

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

                        FlexStep step = plan.Steps[i];
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

        private static FlexStepResult RunStep(Document doc, FamilyManager mgr, FlexStep step, int index, bool recordWarnings)
        {
            var result = new FlexStepResult { Index = index, Type = step.Type ?? mgr.CurrentType?.Name };

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
                if (reason != null) result.SkippedParameters.Add(new SkippedParameter { Name = assignment.Name, Reason = reason });
                else toApply.Add((fp, assignment));
            }

            if (toApply.Count == 0)
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
                                result.Applied.Add(assignment);
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
            if (fp.StorageType == StorageType.Integer) mgr.Set(fp, (int)Math.Round(value));
            else mgr.Set(fp, value);
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

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dialog.Service;
using FamilyEditorInterface.Flex;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FamilyEditorInterface.Requests
{
    /// <summary>
    /// Runs the Flex Engine on the active family and reports the result through the usual dialogs.
    /// Unlike Shuffle, nothing is kept: the family is left exactly as it was.
    /// </summary>
    internal static class FlexTest
    {
        private const int MaxListedFailures = 10;

        internal static string ReportFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "Archilizer", "FamilyEditorInterface", "FlexReports");

        internal static void Execute(UIApplication uiapp)
        {
            Document doc = uiapp.ActiveUIDocument?.Document;
            if (doc == null || !doc.IsFamilyDocument)
            {
                Command.global_message = "Please run this command in a family document.";
                TaskDialog.Show("Message", Command.global_message);
                return;
            }

            var plan = new FlexPlan();   // defaults: baseline, corners, 3 values per parameter, current type
            FlexReport report;
            using (var progress = new ProgressAdapter())
            {
                report = FlexEngine.Run(doc, plan, progress, progress.Token);
            }
            uiapp.ActiveUIDocument.RefreshActiveView();

            string path = Path.Combine(ReportFolder, $"{SafeName(doc.Title)}_{DateTime.Now:yyyyMMdd-HHmmss}.json");
            try { FlexJson.Write(report, path); }
            catch (Exception ex) { path = $"(not saved: {ex.Message})"; }

            Summarize(report, path);
        }

        private static void Summarize(FlexReport report, string path)
        {
            string run = $"{report.Steps.Count} of {report.PlannedSteps} steps in {report.DurationMs / 1000.0:0.0} s"
                         + (report.Truncated ? " (stopped early)" : "");

            if (!report.DocumentRestored)
            {
                RequestError.ErrorLog.Add(new Message("Family not fully restored",
                    "Some parameter values differ from before the flex test. Please check the family and report this as a bug."));
            }

            if (report.Verdict == FlexVerdict.Fail)
            {
                var failed = report.Steps.Where(s => s.Status == FlexStepStatus.Failed || s.Status == FlexStepStatus.Error).ToList();
                foreach (FlexStepResult step in failed.Take(MaxListedFailures))
                {
                    RequestError.ErrorLog.Add(new Message($"Step {step.Index}: {Describe(step)}", Problems(step)));
                }
                if (failed.Count > MaxListedFailures)
                {
                    RequestError.ErrorLog.Add(new Message("...", $"{failed.Count - MaxListedFailures} more failed steps."));
                }
                RequestError.ErrorLog.Add(new Message($"{failed.Count} failed, {report.Passed} passed.", $"{run}.{Environment.NewLine}Report: {path}"));
                return;
            }

            if (RequestError.ErrorLog.Count > 0)
            {
                RequestError.ErrorLog.Add(new Message("Report", path));
                return;
            }

            int warned = report.Steps.Count(s => s.Status == FlexStepStatus.PassedWithWarnings);
            RequestError.NotifyLog =
                $"Flex test passed: {run}." + Environment.NewLine
                + (warned > 0 ? $"{warned} steps raised warnings.{Environment.NewLine}" : "")
                + (report.SkippedParameters.Count > 0 ? $"{report.SkippedParameters.Count} parameters were not flexed (unused, formulas, reporting, no range).{Environment.NewLine}" : "")
                + $"Report: {path}";
        }

        private static string Describe(FlexStepResult step)
        {
            if (step.Applied.Count == 0) return "baseline (no changes)";
            if (step.Applied.Count == 1)
            {
                ParameterAssignment a = step.Applied[0];
                string value = a.Display ?? a.Value.ToString("0.###");
                return step.Label != null ? $"{step.Label} ({value})" : $"{a.Name} = {value}";
            }
            return step.Label != null
                ? $"{step.Label} ({step.Applied.Count} parameters)"
                : string.Join(", ", step.Applied.Select(a => $"{a.Name} = {a.Display ?? a.Value.ToString("0.###")}"));
        }

        // One line per error: the message and the elements that failed, e.g. "Can't create sweep: Sweep (Frame/Mullion)".
        private static string Problems(FlexStepResult step)
        {
            var lines = step.Failures
                .Where(f => f.Severity != "Warning")
                .Select(f =>
                {
                    var elements = f.FailingElements
                        .Select(e => e.Category != null ? $"{e.Kind} ({e.Category})" : e.Kind)
                        .Where(e => e != null)
                        .Distinct()
                        .Take(3)
                        .ToList();
                    return elements.Count > 0 ? $"{f.Message}: {string.Join(", ", elements)}" : f.Message;
                })
                .Distinct()
                .ToList();
            if (step.Exception != null) lines.Insert(0, step.Exception);
            return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : step.Status.ToString();
        }

        private static string SafeName(string title)
        {
            string name = Path.GetFileNameWithoutExtension(title);
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        /// <summary>Drives the existing progress bar window; its Cancel button stops the run after the current step.</summary>
        private sealed class ProgressAdapter : IFlexProgress, IDisposable
        {
            private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
            private ProgressBarView _view;

            public CancellationToken Token => _cancel.Token;

            public void StepStarted(int index, int total, FlexStep step)
            {
                _view ??= new ProgressBarView("Flex test", "{0} of " + total + " steps. {1}", total);
                _view.Increment(step.Assignments.Count == 0
                    ? "Baseline"
                    : string.Join(", ", step.Assignments.Select(a => a.Name)));
            }

            public void StepFinished(FlexStepResult result)
            {
                if (_view != null && _view.getAbortFlag()) _cancel.Cancel();
            }

            public void Dispose()
            {
                _view?.Dispose();
                _cancel.Dispose();
            }
        }
    }
}

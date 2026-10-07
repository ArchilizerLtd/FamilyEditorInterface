using System.Collections.Generic;
using System.Linq;

namespace FamilyEditorInterface.Flex
{
    public enum FlexVerdict { Pass, PassWithWarnings, Fail }

    public enum FlexStepStatus
    {
        Passed,
        PassedWithWarnings,
        /// <summary>Revit raised an error and the change was rolled back.</summary>
        Failed,
        /// <summary>The API threw while applying the step (bad value, unknown type, ...).</summary>
        Error,
        /// <summary>Nothing in the step could be applied (all assignments skipped).</summary>
        Skipped
    }

    public sealed class FlexReport
    {
        public string FamilyName { get; set; }
        public string Category { get; set; }
        public string RevitVersion { get; set; }

        public List<FlexStepResult> Steps { get; } = new List<FlexStepResult>();

        public long DurationMs { get; set; }

        /// <summary>True if the run stopped early (MaxSteps, MaxDuration or cancellation).</summary>
        public bool Truncated { get; set; }

        /// <summary>
        /// True if the family's current type and parameter values after the run match those before it.
        /// A false value means the rollback did not fully restore the document and is a bug.
        /// </summary>
        public bool DocumentRestored { get; set; }

        public int Passed => Steps.Count(s => s.Status == FlexStepStatus.Passed || s.Status == FlexStepStatus.PassedWithWarnings);
        public int Failed => Steps.Count(s => s.Status == FlexStepStatus.Failed || s.Status == FlexStepStatus.Error);
        public int Skipped => Steps.Count(s => s.Status == FlexStepStatus.Skipped);

        public FlexVerdict Verdict =>
            Failed > 0 ? FlexVerdict.Fail
            : Steps.Any(s => s.Status == FlexStepStatus.PassedWithWarnings) ? FlexVerdict.PassWithWarnings
            : FlexVerdict.Pass;
    }

    public sealed class FlexStepResult
    {
        public int Index { get; set; }
        public string Type { get; set; }
        public FlexStepStatus Status { get; set; }

        /// <summary>Assignments that were applied, in internal units.</summary>
        public List<ParameterAssignment> Applied { get; } = new List<ParameterAssignment>();

        public List<SkippedParameter> SkippedParameters { get; } = new List<SkippedParameter>();

        /// <summary>Errors and (if recorded) warnings that Revit raised for this step.</summary>
        public List<FlexFailure> Failures { get; } = new List<FlexFailure>();

        /// <summary>Exception message when Status is Error.</summary>
        public string Exception { get; set; }
    }

    public sealed class SkippedParameter
    {
        public string Name { get; set; }

        /// <summary>not-found, determined-by-formula, reporting, unsupported-storage-type</summary>
        public string Reason { get; set; }
    }

    public sealed class FlexFailure
    {
        public System.Guid FailureId { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public List<FlexElementRef> FailingElements { get; } = new List<FlexElementRef>();
        public List<FlexElementRef> AdditionalElements { get; } = new List<FlexElementRef>();
    }

    public sealed class FlexElementRef
    {
        public long Id { get; set; }

        /// <summary>API class name, e.g. Dimension, ReferencePlane, GenericForm.</summary>
        public string Kind { get; set; }

        public string Name { get; set; }
        public string Category { get; set; }

        /// <summary>For dimensions: the family parameter the dimension is labelled with.</summary>
        public string Label { get; set; }
    }
}

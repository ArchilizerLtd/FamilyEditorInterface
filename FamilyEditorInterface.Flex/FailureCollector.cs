using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace FamilyEditorInterface.Flex
{
    /// <summary>
    /// Records every failure Revit raises during a transaction, with its definition id, severity and elements.
    /// Warnings are deleted (and optionally recorded); any error rolls the transaction back.
    /// </summary>
    internal sealed class FailureCollector : IFailuresPreprocessor
    {
        private readonly bool _recordWarnings;

        public List<FlexFailure> Failures { get; } = new List<FlexFailure>();
        public bool HasErrors { get; private set; }

        public FailureCollector(bool recordWarnings)
        {
            _recordWarnings = recordWarnings;
        }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            Document doc = failuresAccessor.GetDocument();

            foreach (FailureMessageAccessor msg in failuresAccessor.GetFailureMessages())
            {
                FailureSeverity severity = msg.GetSeverity();

                if (severity == FailureSeverity.Warning)
                {
                    if (_recordWarnings) Failures.Add(Describe(doc, msg, severity));
                    failuresAccessor.DeleteWarning(msg);
                }
                else
                {
                    Failures.Add(Describe(doc, msg, severity));
                    HasErrors = true;
                }
            }

            return HasErrors ? FailureProcessingResult.ProceedWithRollBack : FailureProcessingResult.Continue;
        }

        private static FlexFailure Describe(Document doc, FailureMessageAccessor msg, FailureSeverity severity)
        {
            var failure = new FlexFailure
            {
                FailureId = msg.GetFailureDefinitionId().Guid,
                Severity = severity.ToString(),
                Message = SafeDescription(msg)
            };
            foreach (ElementId id in msg.GetFailingElementIds()) failure.FailingElements.Add(DescribeElement(doc, id));
            foreach (ElementId id in msg.GetAdditionalElementIds()) failure.AdditionalElements.Add(DescribeElement(doc, id));
            return failure;
        }

        private static string SafeDescription(FailureMessageAccessor msg)
        {
            try { return msg.GetDescriptionText(); }
            catch { return "Unknown failure"; }
        }

        // Elements still exist while failures are being preprocessed, so resolve them here
        // rather than later, when a rollback may have removed them.
        private static FlexElementRef DescribeElement(Document doc, ElementId id)
        {
            var reference = new FlexElementRef { Id = id.Value };
            try
            {
                Element element = doc.GetElement(id);
                if (element == null) return reference;

                reference.Kind = element.GetType().Name;
                reference.Name = element.Name;
                reference.Category = element.Category?.Name;
                if (element is Dimension dimension) reference.Label = dimension.FamilyLabel?.Definition?.Name;
            }
            catch
            {
                // Description is best-effort; the id alone is still useful.
            }
            return reference;
        }
    }
}

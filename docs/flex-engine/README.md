# Flex Engine

A headless library that stress-tests a Revit family. It drives the family's parameters through a set of values, regenerates after each change, records anything that breaks, and leaves the document exactly as it found it.

It has two consumers:

1. **FamilyEditorInterface (this add-in).** The interactive "flex / shuffle" features call the engine and show its report in the UI.
2. **The Parametric Family Builder** (see [../family-builder/README.md](../family-builder/README.md)). The builder's generate → flex → repair loop uses the engine as its verifier. In that setting the engine runs headless, either in a local Revit session or on Autodesk Platform Services Design Automation for Revit.

> Status: design draft. Nothing in this document is implemented yet.

---

## 1. What exists today

The add-in already does most of the low-level work, but the code is tied to the UI.

| Capability | Where | Notes |
|---|---|---|
| Set a parameter value and regenerate | [RequestExecute.cs](../../FamilyEditorInterface/Requests/RequestExecute.cs) `ExecuteParameterChange(...)` overloads | One `Transaction` per value, inside a `TransactionGroup` |
| Catch regeneration failures | `FailureHandler : IFailuresPreprocessor` ([RequestExecute.cs:446](../../FamilyEditorInterface/Requests/RequestExecute.cs#L446)) | Deletes warnings, returns `ProceedWithRollBack` on errors |
| Random values ("Shuffle") | `FamilyParameterViewModel.Shuffle` + `Utils.GetRandomItemValue` | Random sampling only; no ranges, no plan |
| Skip formula-driven / reporting parameters | `IsDeterminedByFormula`, `IsReporting` checks | Good. Keep this. |

### Gaps to close before the engine can be used in a loop

1. **UI coupling.** Execution needs a `UIApplication`/`UIDocument` and calls `RefreshActiveView()`, `TaskDialog.Show`, and `ProgressBarView`. Design Automation has no UI, so none of these are available there.
2. **Lossy failure capture.** `FailureHandler` keeps a single `ErrorMessage` string and overwrites it on each message. The engine needs every failure, including its `FailureDefinitionId`, severity, and failing element IDs.
3. **Warnings are discarded.** Deleting warnings is right for an interactive tool, but for QA they are signal. "Elements have duplicate type marks" and "Line is slightly off axis" both indicate a poorly built family.
4. **Changes are kept.** The current flow `Commit`s each step and `Assimilate`s the group. A flex run should roll back the whole group at the end so the family is unchanged.
5. **Results go to global state.** `RequestError.ErrorLog` and `RequestError.NotifyLog` are static collections. The engine has to return a report object.
6. **No geometric checks.** Today "success" means "Revit didn't raise an error." Families also break silently: geometry stops tracking a parameter, a solid collapses, or a void stops cutting.

---

## 2. Design goals

- **No UI dependency.** References `RevitAPI.dll` only. Never references `RevitAPIUI.dll` or WPF.
- **Non-destructive.** The document is byte-for-byte unchanged in content after a run. The whole run happens inside a `TransactionGroup` that is rolled back.
- **Deterministic.** The same plan and the same seed produce the same steps, so failures can be reproduced.
- **Machine-readable output.** A `FlexReport` serializes to JSON and is stable enough for a model to consume.
- **Bounded.** Step count and wall-clock limits are honoured. Design Automation work items have a time limit.

---

## 3. Proposed project layout

```
FamilyEditorInterface.sln
├── FamilyEditorInterface/          (existing add-in; WPF, RevitAPIUI)
│     └── calls → FlexEngine
├── FamilyEditorInterface.Flex/     (NEW: class library, net10.0-windows, RevitAPI only)
│     ├── FlexEngine.cs
│     ├── Planning/                 FlexPlan, samplers
│     ├── Execution/                step runner, FailureCollector
│     ├── Checks/                   IFlexCheck implementations
│     └── Reporting/                FlexReport, JSON serialization
└── FamilyEditorInterface.Flex.DA/  (NEW, later: Design Automation entry point)
      └── IExternalDBApplication that reads plan.json, opens the .rfa, writes report.json
```

The existing add-in becomes a thin client. Its "Shuffle" button builds a `FlexPlan` with a random sampler and shows the `FlexReport` in the existing dialogs.

---

## 4. Public API (sketch)

```csharp
namespace FamilyEditorInterface.Flex
{
    public static class FlexEngine
    {
        /// Runs the plan against a family document and returns a report.
        /// The document is left unchanged: all work is rolled back at the end.
        public static FlexReport Run(Document familyDoc, FlexPlan plan,
                                     IFlexProgress progress = null,
                                     CancellationToken cancel = default);
    }

    /// Optional progress sink. The add-in implements it with the WPF progress bar.
    /// Design Automation implements it with console logging.
    public interface IFlexProgress
    {
        void StepStarted(int index, int total, FlexStep step);
        void StepFinished(int index, FlexStepResult result);
    }
}
```

### 4.1 FlexPlan: what to test

```csharp
public sealed class FlexPlan
{
    public int Seed { get; init; } = 0;
    public int MaxSteps { get; init; } = 500;
    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMinutes(5);

    /// Which family types to run against. Empty means all types in FamilyManager.Types.
    public IReadOnlyList<string> Types { get; init; } = [];

    /// Per-parameter ranges and behaviour. Parameters not listed fall back to Defaults.
    public IReadOnlyList<ParameterRange> Parameters { get; init; } = [];
    public RangeDefaults Defaults { get; init; } = new();

    /// How to pick values. Samplers are combined in order.
    public IReadOnlyList<SamplerSpec> Samplers { get; init; } =
        [ new SamplerSpec(SamplerKind.Corners), new SamplerSpec(SamplerKind.OneAtATime, Steps: 5) ];

    /// Which checks to run after every step.
    public IReadOnlyList<CheckSpec> Checks { get; init; } = CheckSpec.Default;

    /// Record warnings in the report (true for QA, false for interactive use).
    public bool RecordWarnings { get; init; } = true;
}

public sealed record ParameterRange(
    string Name,
    double? Min,                // internal units (feet, radians), as the Revit API uses
    double? Max,
    double? Step = null,        // snap to increments, e.g. 50 mm module
    IReadOnlyList<double> Values = null,  // explicit list overrides Min/Max
    bool Skip = false);

public sealed record RangeDefaults(
    double LengthScaleMin = 0.5,   // when no range is given: 50%–200% of current value
    double LengthScaleMax = 2.0,
    bool IncludeZero = false);     // try 0 for lengths (useful for finding fragile families)

public enum SamplerKind
{
    Defaults,     // current values of each type (baseline)
    Corners,      // all-min, all-max, and each parameter at min/max with others default
    OneAtATime,   // sweep each parameter across its range, others at default
    Pairwise,     // all-pairs combinations (catches interacting constraints cheaply)
    Random,       // seeded random points inside ranges (what Shuffle does today)
    TypeCatalog   // every row of the family's type catalog (.txt next to the .rfa)
}

public sealed record SamplerSpec(SamplerKind Kind, int Steps = 0, int Count = 0);
```

Revit does not store min/max values for family parameters, so ranges come from the plan. In the builder, the plan is generated from the same product data that produced the family, for example "width 600–2400 mm in 300 mm steps". In the add-in, the user can type ranges or accept the defaults.

### 4.2 Steps and execution

A **step** is one assignment of values to one or more parameters, applied to one family type.

```
FlexEngine.Run
 └─ TransactionGroup "Flex" .Start()
     ├─ capture baseline (per type): values + geometry snapshot
     ├─ for each step:
     │    Transaction "Flex step n" with FailureCollector
     │      ├─ FamilyManager.CurrentType = step.Type
     │      ├─ FamilyManager.Set(param, value) for each assignment
     │      ├─ Document.Regenerate()
     │      ├─ run checks against the baseline snapshot
     │      └─ Commit, or the failure handler rolls back
     │    record FlexStepResult
     └─ TransactionGroup .RollBack()   ← the document is restored
```

Rules carried over from the current code:

- Skip parameters where `IsDeterminedByFormula` or `IsReporting` is true. Record them as `Skipped` rather than dropping them silently.
- Use `SetClearAfterRollback(true)` so a failed step doesn't leave failures pending for the next step.

New rules:

- A failed step does not stop the run. The next step starts from the baseline values, not from the broken state, so one failure doesn't cascade into the steps after it.
- Optional **bisection.** When a multi-parameter step fails, re-run it one parameter at a time to find which parameter (or pair) is responsible. This makes the report far more useful for repair.

### 4.3 FailureCollector: replaces FailureHandler

```csharp
internal sealed class FailureCollector : IFailuresPreprocessor
{
    public List<FlexFailure> Failures { get; } = new();
    public bool RecordWarnings { get; init; }

    public FailureProcessingResult PreprocessFailures(FailuresAccessor fa)
    {
        bool hasError = false;
        foreach (var msg in fa.GetFailureMessages())
        {
            var severity = msg.GetSeverity();
            if (severity == FailureSeverity.Warning)
            {
                if (RecordWarnings) Failures.Add(FlexFailure.From(msg));
                fa.DeleteWarning(msg);
            }
            else
            {
                Failures.Add(FlexFailure.From(msg));
                hasError = true;
            }
        }
        return hasError ? FailureProcessingResult.ProceedWithRollBack
                        : FailureProcessingResult.Continue;
    }
}
```

`FlexFailure.From` captures `GetFailureDefinitionId().Guid`, `GetDescriptionText()`, `GetSeverity()`, `GetFailingElementIds()`, and `GetAdditionalElementIds()`. It also resolves element IDs to a short description: category, name, and for dimensions, the label parameter.

### 4.4 Checks: catching silent breakage

Each check compares the regenerated state against the baseline snapshot of the same type.

```csharp
public interface IFlexCheck
{
    string Id { get; }
    IEnumerable<FlexFinding> Evaluate(FlexContext ctx, FlexStep step);
}
```

| Check id | What it catches | How |
|---|---|---|
| `regen.error` | Revit refused the change (built in, from `FailureCollector`) | Error-severity failure |
| `regen.warning` | Sloppy construction | Warning-severity failure |
| `geom.bbox-tracks` | A parameter that no longer drives geometry | For length parameters mapped to an axis, the bounding-box extent should change by about Δvalue. The mapping comes from the plan, or is inferred from a baseline sweep. |
| `geom.volume-positive` | Collapsed or inverted solids | Every `Solid.Volume` > ε |
| `geom.solid-count` | A sweep or extrusion that disappeared, or a void that stopped cutting | The number of solids equals the baseline count, unless the plan allows it to vary |
| `geom.disjoint` | Geometry that detached from the main body | Optional; connectivity of solids |
| `param.formula-eval` | Formulas that evaluate to invalid values at the edges of ranges, such as negative lengths or division by zero | Read back formula-driven parameters after regeneration |
| `catalog.row-valid` | Type catalog rows that don't build | `TypeCatalog` sampler plus `regen.error` |

Geometry is read with `element.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine })` over the family's `GenericForm` and `FamilyInstance` elements. The snapshot stores a per-element bounding box, solid count, and total volume, which keeps checks cheap.

---

## 5. FlexReport: what comes back

The report is designed to be read by people (the add-in dialog) and by a model (the builder's repair step).

```jsonc
{
  "schemaVersion": "1.0",
  "family": { "name": "Window_Casement_Archetype", "category": "Windows", "revitVersion": "2027" },
  "plan": { "seed": 42, "samplers": ["Corners", "OneAtATime:5"], "maxSteps": 500 },
  "summary": {
    "steps": 64, "passed": 61, "failed": 3, "skipped": 2,
    "durationMs": 18450,
    "verdict": "fail"            // "pass" | "pass-with-warnings" | "fail"
  },
  "skippedParameters": [
    { "name": "Rough Width", "reason": "determined-by-formula" }
  ],
  "failures": [
    {
      "step": 37,
      "type": "900x1200",
      "assignments": { "Width": 0.9843, "Sill Height": 0.0 },   // internal units (ft)
      "assignmentsDisplay": { "Width": "300 mm", "Sill Height": "0 mm" },
      "culprit": ["Width"],                                       // from bisection
      "findings": [
        {
          "check": "regen.error",
          "failureId": "8b8b6c9e-....",
          "severity": "Error",
          "message": "Constraints are not satisfied",
          "elements": [
            { "id": 1234, "kind": "Dimension", "label": "Frame Width" },
            { "id": 1302, "kind": "ReferencePlane", "name": "Frame Left" }
          ]
        }
      ]
    }
  ],
  "warnings": [ /* same shape as failures, severity Warning */ ]
}
```

Notes:

- `assignments` are in Revit internal units so a run can be replayed exactly. `assignmentsDisplay` is for people.
- Element IDs are only valid inside that `.rfa`. The builder maps them back to DSL node names using a name convention (see the builder's DSL doc), so the repair model can say "fix constraint `frame.width`" instead of "fix element 1234".
- `verdict` is the single field a pipeline gates on.

---

## 6. Running headless (Design Automation)

- Entry point: an `IExternalDBApplication` that subscribes to `DesignAutomationBridge.DesignAutomationReadyEvent`, from the `Autodesk.Forge.DesignAutomation.Revit` NuGet package.
- Inputs: `family.rfa`, `plan.json`. Output: `report.json`, plus optional preview images exported per failed step.
- No `UIApplication` exists. Everything goes through `Document` and `Application`. This is why the engine must not reference `RevitAPIUI`.
- Before relying on it, confirm that Design Automation supports Revit 2027. Engine availability on Design Automation usually lags the desktop release.

The same DA app can run locally against a desktop Revit session for development (open the document, call `FlexEngine.Run`, write JSON). That keeps cloud iteration loops short.

---

## 7. Implementation chunks

Each chunk is small enough to finish and review on its own.

| # | Chunk | Done when |
|---|---|---|
| F1 | Create `FamilyEditorInterface.Flex` project (RevitAPI only), wire into the solution | Builds; the add-in references it |
| F2 | `FailureCollector` that records every failure with ID, severity, and elements | Unit-style test family produces a full failure list |
| F3 | Step runner: TransactionGroup with per-step transactions, reset to baseline, final rollback | Document unchanged after a run (verify by comparing parameter values and element count) |
| F4 | `FlexPlan` + `Defaults`, `Corners`, `OneAtATime`, `Random` samplers (seeded) | Same seed produces the same step list |
| F5 | `FlexReport` + JSON serialization (System.Text.Json) | Report round-trips; schema documented |
| F6 | Port the add-in's Shuffle to the engine; show the report in the existing failure dialog | Feature parity with today, plus a better error list |
| F7 | Geometry snapshot + `geom.volume-positive`, `geom.solid-count` | Catches a deliberately broken test family |
| F8 | `geom.bbox-tracks` with axis mapping from the plan | Catches a parameter that was un-labelled from its dimension |
| F9 | Bisection for multi-parameter failures | Report names the culprit parameter on a pairwise failure |
| F10 | `Pairwise` and `TypeCatalog` samplers | Runs a type catalog end to end |
| F11 | Local headless runner (`plan.json` in, `report.json` out) | Runs from the command line against desktop Revit |
| F12 | Design Automation app bundle + activity | Same report produced in the cloud |

### Test fixtures to build alongside

A small set of deliberately broken families makes every chunk testable:

- `ok_box.rfa`: a clean parametric box (baseline).
- `overconstrained.rfa`: fails at small widths.
- `unlabelled_dim.rfa`: a width parameter that drives nothing.
- `zero_thickness.rfa`: an extrusion that collapses at a range edge.
- `bad_formula.rfa`: a formula that goes negative at the minimum value.

`rac_basic_sample_family_test.rfa` in the repo root can serve as an extra real-world sample.

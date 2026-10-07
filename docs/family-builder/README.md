# Parametric Family Builder

An Archilizer product concept: turn manufacturer product data (spec sheets, catalogs, drawings, images) into **parametric, flex-validated Revit families**, automatically and at catalog scale.

> Status: concept and plan. This document is the reference for the architecture and the roadmap. It lives in the FamilyEditorInterface repo for now because the [Flex Engine](../flex-engine/README.md) is built here. The builder will most likely move to its own repo once Phase 1 starts.

Related docs:

- [Flex Engine](../flex-engine/README.md): the verifier at the heart of the loop.
- [Family DSL](DSL.md): the declarative spec the builder generates and compiles.

---

## 1. The problem

- Manufacturers need Revit content so their products get specified. Today they pay BIM content agencies to build families by hand. It's slow, quality varies, and every catalog change means rework.
- Existing automated tools (for example, pipelines built on CadQuery/OpenCascade) produce **static geometry**. Once it reaches Revit it's a dumb solid: no reference planes, no labelled dimensions, no formulas, no type catalogs. Designers can't resize it, so it has little value at the design stage.
- An emerging US initiative to connect developers with material manufacturers through a marketplace would multiply the demand for consistent, high-quality, parametric content.

**Our thesis:** creating a family once is a solved problem. The unsolved part is producing *and maintaining* hundreds of parametric families at consistent quality. If product data goes in and validated families come out, a catalog update becomes a re-run instead of a re-model.

## 2. What makes this hard to copy

1. **Flex-validated quality.** Every family is automatically stress-tested across its parameter ranges before it ships, and it comes with a report that proves it.
2. **An archetype library** of well-built family skeletons that gets better with each category added.
3. **The data pipeline:** reliably turning messy manufacturer PDFs into structured parameters.
4. **The data flywheel:** every generate → flex → repair cycle produces labelled training data no competitor has.

---

## 3. Architecture

```
 spec sheet / image / catalog / STEP
            │
     [1] EXTRACT ─────────────── LLM + vision ─────────────► ProductData (JSON)
            │
     [2] ROUTE ───────────────── match against archetype corpus
            │                              │
     archetype match                  no good match
            │                              │
     [3a] MAP parameters           [3b] GENERATE FamilySpec (model)
            │                              │
            └────────────► FamilySpec (DSL, see DSL.md)
                                   │
                           [4] COMPILE ── Revit API ──► .rfa + type catalog
                                   │
                           [5] FLEX ───── Flex Engine ──► FlexReport
                                   │
                    ┌──── pass ────┴──── fail ─────┐
                    │                              │
             [7] PUBLISH                 [6] REPAIR ─► patch the FamilySpec
       .rfa + catalog + QA report                  │   (rules first, then model)
                                                   └──► back to [4], at most N times,
                                                        then escalate to a human
```

### 3.1 Components

| # | Component | Input | Output | Notes |
|---|---|---|---|---|
| 1 | **Extractor** | PDF, images, web pages, STEP | `ProductData` JSON: dimensions and ranges, options and variants, materials, identity data, and source citations | Vision LLM. Each value carries a confidence score and a page/region reference so a person can verify it. |
| 2 | **Router** | `ProductData` | An archetype ID with a fit score, or "none" | Starts as retrieval plus rules (category, key dimensions). Learned later. |
| 3a | **Mapper** | `ProductData` + archetype spec | `FamilySpec` (the archetype with values, types, and metadata filled in) | Deterministic where possible. A model fills naming and grouping gaps. |
| 3b | **Generator** | `ProductData` (+ images) + nearest archetypes as examples | `FamilySpec` | The model **only ever emits DSL**, never Revit API calls or geometry. |
| 4 | **Compiler** | `FamilySpec` | `.rfa`, type catalog `.txt`, and an ID map (DSL node → element ID) | Owns all Revit best practice: plane hierarchy, locking, formula placement, subcategories. Families are well built by construction. |
| 5 | **Flex Engine** | `.rfa` + `FlexPlan` (generated from the spec's ranges) | `FlexReport` | See [Flex Engine](../flex-engine/README.md). |
| 6 | **Repairer** | `FamilySpec` + `FlexReport` (element IDs mapped back to DSL node names) | A JSON Patch (RFC 6902) against the `FamilySpec` | Known failure patterns are fixed by rules. The model handles the rest. Patches are small and diffable, and they're logged. |
| 7 | **Publisher** | Passing `.rfa` + report | A release bundle: `.rfa` per supported Revit version, catalog, QA report, preview images, metadata (IFC/bSDD) | Families don't open in older Revit versions, so compile per version or in the oldest supported one. |

### 3.2 Key design decisions

1. **The model writes DSL, the compiler writes Revit.** A constrained, schema-validated output means a model can't produce a family that is malformed in structure, only one that fails flex. Flex failures are the kind we can detect and repair.
2. **The Flex Engine is the verifier and the reward signal.** Pass/fail gates publishing. The failure and fix pairs become training data.
3. **Frontier model first, small model later.** Start with a frontier LLM, few-shot examples, and retrieval over archetype specs. Log every trace. Distill into a specialised small model once there are a few thousand verified (input → spec) pairs. Training first, on a small corpus, would delay the product and produce a weaker model.
4. **The compiler is ground truth, so data can be synthetic.** Randomizing archetype parameters and combining features gives thousands of valid specs. Compiling them, rendering views, and auto-generating spec sheets gives (image/spec → DSL) training pairs at almost no cost.
5. **Archetypes first, generation second.** Most products in a category share a skeleton. Mapping onto a proven archetype is reliable. Generation covers the long tail.
6. **Headless from day one.** Every Revit-touching component runs without UI, so the same code works in a local Revit session and on Design Automation.

### 3.3 Data contracts

All artifacts are versioned JSON, so every stage can be tested and replayed on its own.

| Artifact | Producer → Consumer | Defined in |
|---|---|---|
| `ProductData` | Extractor → Router, Mapper, Generator | Phase 3 (to be written) |
| `FamilySpec` | Mapper / Generator / Repairer → Compiler | [DSL.md](DSL.md) |
| `IdMap` | Compiler → Repairer | [DSL.md §6](DSL.md#6-traceability-dsl-ids-in-the-rfa) |
| `FlexPlan` / `FlexReport` | Builder ↔ Flex Engine | [Flex Engine §4–5](../flex-engine/README.md#4-public-api-sketch) |
| `RepairPatch` | Repairer → Compiler | JSON Patch over `FamilySpec` |
| `Trace` | Every stage → dataset store | All of the above for one run, plus timings and model versions |

---

## 4. Roadmap: actionable chunks

Phases are ordered by dependency. Each chunk has a clear deliverable and a test for when it's done. A **market validation** track runs in parallel and decides how far past Phase 4 we go.

### Phase 0: Flex Engine (foundation, in this repo)

The verifier comes first. It's useful on its own as a "family QA" tool and it de-risks the core loop.

| ID | Chunk | Done when |
|---|---|---|
| P0.1 | Flex Engine chunks F1–F6 (headless library, failure collector, step runner, samplers, report, port Shuffle) | The add-in's Shuffle runs on the engine and produces a JSON `FlexReport` |
| P0.2 | Broken-family test fixtures | Each fixture fails with the expected finding |
| P0.3 | Geometry checks F7–F8 | The engine catches silent breakage (unlabelled dimension, collapsed solid) |
| P0.4 | Local headless runner F11 | `plan.json` + `.rfa` → `report.json` from the command line |

**Milestone:** run the engine on 20 real manufacturer families downloaded from public content sites. A results table showing how many break, and how, is the first piece of market evidence.

### Phase 1: DSL and compiler

| ID | Chunk | Done when |
|---|---|---|
| P1.1 | Write DSL v0.1 as a JSON Schema (from [DSL.md](DSL.md)) | Schema validates the examples in DSL.md |
| P1.2 | Choose the first category (proposal: louvres or a simple casement window) and hand-write 3–5 archetype specs | Specs reviewed by a senior family author |
| P1.3 | Compiler: family from template, parameters (incl. formulas, shared parameters), reference planes, labelled dimensions | A spec with only planes and parameters compiles and flexes |
| P1.4 | Compiler: plane-bounded extrusions and voids, alignment locks, materials, visibility | The `box_frame` example in DSL.md compiles and passes flex |
| P1.5 | Compiler: arrays driven by a count parameter, nested families, type catalogs | All archetypes compile and pass flex |
| P1.6 | `IdMap` via Extensible Storage | Flex report element IDs resolve to DSL node IDs |
| P1.7 | Where the DSL strains, log it and revise to v0.2 | List of language gaps closed or deferred |

**Milestone:** 5 archetypes, compiled from DSL, all pass flex, built in Revit 2027. Every Revit-side feature the product needs is now proven.

### Phase 2: Repair loop

| ID | Chunk | Done when |
|---|---|---|
| P2.1 | Failure taxonomy: group `FlexReport` failures by failure ID and pattern across the fixtures and Phase 0's real families | A documented list of the top ~15 failure patterns |
| P2.2 | Rule-based repairs for the top patterns (for example, add a minimum-value clamp formula, or re-host a plane on its parent) | Rules fix the seeded failures in the fixtures |
| P2.3 | LLM repairer: spec + report → JSON Patch, with schema validation of the patched spec | It fixes failures the rules don't cover, on a held-out set |
| P2.4 | Loop orchestration: max N iterations, rollback to the best spec seen, escalation | The loop terminates on every input; escalations are logged |
| P2.5 | Trace logging to a dataset store | Every run is replayable from its trace |

### Phase 3: Extraction

| ID | Chunk | Done when |
|---|---|---|
| P3.1 | Define the `ProductData` schema for the first category | Schema reviewed against 10 real spec sheets |
| P3.2 | Gold set: hand-annotate ~30 spec sheets | Gold JSON + source PDFs under version control |
| P3.3 | Vision LLM extractor with source citations and confidence scores | Measured accuracy on the gold set; low-confidence fields flagged |
| P3.4 | Review UI: show each extracted value next to its source region; accept or edit | A person can verify one product in < 5 min |

### Phase 4: End-to-end demo (first category)

| ID | Chunk | Done when |
|---|---|---|
| P4.1 | Router v0 (rules + retrieval) | Picks the right archetype on the gold set |
| P4.2 | Mapper: `ProductData` + archetype → `FamilySpec` with types/catalog | Gold-set products compile and pass flex |
| P4.3 | Generator v0 (frontier LLM, few-shot from archetypes) for products with no matching archetype | Measured compile rate and flex pass rate |
| P4.4 | Publisher: release bundle with QA report and previews | A bundle a manufacturer could put on their website |
| P4.5 | **Demo:** one real manufacturer's catalog section, PDF in → families out | Recorded demo + metrics: % auto-passed, % repaired, % escalated, time per family |

**Milestone:** the pitch asset. Go/no-go for Phases 5–7, informed by the market track.

### Phase 5: Cloud and scale

| ID | Chunk | Done when |
|---|---|---|
| P5.1 | Confirm Design Automation support for target Revit versions | Supported version matrix documented |
| P5.2 | Compiler and Flex Engine as DA app bundles/activities | The demo pipeline runs fully in the cloud |
| P5.3 | Job orchestration: batch a catalog, retries, cost tracking | 100 families processed unattended; cost per family known |
| P5.4 | Multi-version output | Each family published for every supported Revit version |

### Phase 6: Data flywheel and the specialised model

| ID | Chunk | Done when |
|---|---|---|
| P6.1 | Decompiler: existing well-built `.rfa` → `FamilySpec` (best-effort) | Round trip (spec → rfa → spec) is stable on the archetypes |
| P6.2 | Synthetic data generator: randomized specs → compiled → rendered views + synthetic spec sheets | 10k+ verified (input → spec) pairs |
| P6.3 | Evaluation harness: held-out real products; metrics are compile rate, flex pass rate, parameter coverage, repair iterations, human edit time | Baseline numbers for the frontier-model pipeline |
| P6.4 | Fine-tune or distill a small model on synthetic + real traces | Beats the frontier baseline on cost per family at equal pass rate |
| P6.5 | Use repair traces as preference data for the generator | Fewer repair iterations per family |

### Phase 7: Productization

| ID | Chunk | Done when |
|---|---|---|
| P7.1 | Metadata standards: IFC classes, bSDD properties, ISO 23386/23387 data templates, ETIM where relevant | Published families carry standard metadata |
| P7.2 | Catalog maintenance: diff a new catalog version against the last, regenerate only what changed | An update run on a changed catalog |
| P7.3 | Integrations: marketplace/platform API, manufacturer portal | Depends on market track outcomes |
| P7.4 | Standalone "Family QA" product, from Phase 0 | Optional early revenue: a flex report for any family |

### Parallel track: market validation

| ID | Chunk | Done when |
|---|---|---|
| M1 | Research the US marketplace initiative: who runs it, timeline, content requirements | Brief written |
| M2 | Competitive scan: content agencies, content platforms, any automated parametric attempts | Landscape table |
| M3 | Interviews: ~10 manufacturers, ~5 content agencies. What do they pay per family today, how often do they update, what do users complain about? | Interview notes + synthesis |
| M4 | Use the Phase 0 milestone (how many real families break) as outreach material | Used in at least 5 conversations |
| M5 | Pilot: one manufacturer, one category, service model ("parametric families, delivered fast"), run on internal tooling | Signed pilot |

---

## 5. Risks

| Risk | Mitigation |
|---|---|
| The DSL is too narrow for real products | Phase 1 is explicitly about finding where it strains. Allow an escape hatch: a nested "static geometry" family for genuinely free-form parts. |
| Extraction accuracy from messy PDFs | Citations + confidence + a human review UI. Start with categories that have clean tabular spec sheets. |
| Autodesk or a large content platform builds this | Partnering may be the better route. The Flex Engine and the dataset are the assets that survive either way. |
| Design Automation lags new Revit versions | Run desktop Revit headless as a fallback. Target the oldest supported version for output anyway. |
| Revit-only lock-in | The DSL is not Revit-specific in principle. An Archicad GDL compiler is a later expansion. |
| The marketplace initiative stalls | The value proposition to manufacturers and agencies stands on its own without it. |

## 6. Open questions

- Which category first? Candidates: louvres, casement windows, plumbing fixtures, curtain wall panels. Criteria: many SKUs, clean spec data, frequent updates, simple topology.
- Service first, or tool licensed to agencies first?
- Which Revit versions do customers need? This sets the oldest compile target.
- Where does the dataset live, and what are the IP terms with pilot manufacturers for their data and our derived training data?

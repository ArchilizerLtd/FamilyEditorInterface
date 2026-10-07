# Family DSL (draft v0.1)

The declarative spec that describes a parametric Revit family. The [builder](README.md) generates it (by mapping or by model), the compiler turns it into an `.rfa`, and the repairer patches it.

> Status: first draft, written to be argued with. Phase 1 of the roadmap exists to find where it breaks.

## 1. Principles

1. **Describe intent, not API calls.** The spec says "the frame's left face sits on plane `left`." The compiler decides how to sketch, align, and lock.
2. **Everything is named.** Every plane, dimension, solid, and parameter has a stable `id`. Flex reports and repair patches refer to these IDs, never to Revit element IDs.
3. **Geometry is bounded by reference planes.** Solids are defined by the planes that bound them, not by coordinates. This is what makes a family flex well, and the compiler enforces it.
4. **Planes form a tree.** Each reference plane is positioned relative to a parent plane, ending at the template's origin planes. No circular references and no free-floating planes.
5. **Ranges are part of the spec.** Each driving parameter declares the range it must survive. That range becomes the `FlexPlan`.
6. **Schema-validated.** A spec that fails JSON Schema validation never reaches Revit.

Specs are authored in YAML for readability and stored and exchanged as JSON.

## 2. Top-level structure

```yaml
dsl: 0.1
family:
  id: box_frame
  name: Archilizer_BoxFrame
  template: Metric Generic Model.rft   # resolved per Revit version by the compiler
  category: Generic Models
  minRevitVersion: 2027
  metadata: { ... }                    # manufacturer, model, URL, IFC class, bSDD props
parameters: [ ... ]
planes: [ ... ]
dimensions: [ ... ]
solids: [ ... ]
arrays: [ ... ]                        # optional
nested: [ ... ]                        # optional
types: [ ... ]                         # or typeCatalog
```

## 3. Parameters

```yaml
parameters:
  - id: width
    name: Width
    dataType: length            # length | angle | number | integer | yesno | material | text | url
    scope: type                 # type | instance
    group: dimensions           # maps to a GroupTypeId
    default: 900 mm
    range: { min: 300 mm, max: 2400 mm, step: 50 mm }   # → FlexPlan
    shared: false               # or a GUID from a shared parameter file

  - id: height
    name: Height
    dataType: length
    scope: type
    default: 1200 mm
    range: { min: 300 mm, max: 3000 mm, step: 50 mm }

  - id: depth
    name: Depth
    dataType: length
    scope: type
    default: 100 mm
    range: { min: 50 mm, max: 300 mm }

  - id: frame_t
    name: Frame Thickness
    dataType: length
    scope: type
    default: 50 mm
    range: { min: 30 mm, max: 120 mm }

  - id: opening_w
    name: Opening Width
    dataType: length
    scope: type
    formula: "Width - 2 * Frame Thickness"   # formula-driven → skipped by flex, checked by param.formula-eval
    guard: { min: 10 mm }                    # compiler wraps the formula: if(x < 10 mm, 10 mm, x)

  - id: frame_mat
    name: Frame Material
    dataType: material
    scope: type
    default: "Aluminium, Anodised"
```

Units are written with suffixes (`mm`, `m`, `ft`, `in`, `deg`) and converted to Revit internal units by the compiler. `guard` is a common repair target: the repairer adds one when flex shows a formula going negative.

## 4. Reference planes and dimensions

```yaml
planes:
  # Template origin planes are predefined: origin.lr (Center Left/Right), origin.fb (Center Front/Back), origin.ref (Ref. Level)
  - id: left
    parent: origin.lr
    offset: { param: width, factor: -0.5 }    # placed at -Width/2
    isReference: left
  - id: right
    parent: origin.lr
    offset: { param: width, factor: 0.5 }
    isReference: right
  - id: top
    parent: origin.ref
    offset: { param: height }
    isReference: top
  - id: back
    parent: origin.fb
    offset: { param: depth }
    isReference: back
  - id: inner_left
    parent: left
    offset: { param: frame_t }
    isReference: weak
  - id: inner_right
    parent: right
    offset: { param: frame_t, factor: -1 }
    isReference: weak
  - id: inner_bottom
    parent: origin.ref
    offset: { param: frame_t }
    isReference: weak
  - id: inner_top
    parent: top
    offset: { param: frame_t, factor: -1 }
    isReference: weak

dimensions:
  - id: dim.width
    between: [left, right]
    label: width
  - id: dim.width_eq
    between: [left, origin.lr, right]
    equality: true                 # EQ, keeps the family centred
  - id: dim.frame_l
    between: [left, inner_left]
    label: frame_t
  - id: dim.frame_r
    between: [inner_right, right]
    label: frame_t
```

The compiler creates the planes in tree order, places the dimensions in a suitable view, and labels them. The `offset` values give the initial positions, and the labelled dimensions keep them constrained. `isReference` maps to the plane's "Is Reference" property (left, right, front, back, top, bottom, strongReference, weak, notAReference).

## 5. Geometry

### 5.1 Plane-bounded box: the workhorse

```yaml
solids:
  - id: frame
    kind: extrusion
    profile:
      box: { left: left, right: right, bottom: origin.ref, top: top }   # rectangle; each edge locked to its plane
    extent: { from: origin.fb, to: back }                              # extrusion start/end locked to planes
    material: frame_mat
    subcategory: Frame
    visible: true               # or a yesno parameter id

  - id: opening
    kind: void-extrusion
    profile:
      box: { left: inner_left, right: inner_right, bottom: inner_bottom, top: inner_top }
    extent: { from: origin.fb, to: back }
    cuts: [frame]
```

### 5.2 Other primitives (v0.1 scope)

| kind | Defined by | Notes |
|---|---|---|
| `extrusion` / `void-extrusion` | `profile` + `extent` | `profile.box` (4 planes) or `profile.polyline` (below) |
| `sweep` | `path` (plane-bounded line/arc chain) + `profile` (nested profile family or inline) | Common for frames and handrails |
| `blend` | `bottomProfile`, `topProfile`, `extent` | Use sparingly; it flexes poorly |
| `revolve` | `profile`, `axis` (plane intersection) | Round fixtures |

`profile.polyline` lists vertices as **plane intersections**, not coordinates:

```yaml
profile:
  polyline:
    - [left, origin.ref]
    - [right, origin.ref]
    - [right, top]
    - [chamfer_v, top]        # vertex where plane chamfer_v meets plane top
    - [left, chamfer_h]
  closed: true
```

The compiler locks every sketch segment to its plane, which is what makes the profile flex.

### 5.3 Arrays

```yaml
arrays:
  - id: blades
    of: blade                   # a solid or nested family id
    direction: [bottom_blade, top_blade]   # first and last positions locked to these planes
    count: { param: blade_count }          # integer parameter, typically a formula
    minCount: 2                 # Revit arrays break below 2; the compiler guards the formula
```

### 5.4 Nested families

```yaml
nested:
  - id: handle
    family: { spec: handle_lever }        # another FamilySpec, or { rfa: path } for static content
    shared: true
    host: { plane: right, offset: { param: handle_offset } }
    associate:                            # nested param ← host param
      Handle Height: handle_h
      Material: hardware_mat
```

Static geometry (for example, a manufacturer's STEP model of a decorative part) goes in a nested family with no parameters. This is the escape hatch for free-form shapes that aren't worth making parametric.

## 6. Traceability: DSL IDs in the `.rfa`

The compiler writes each node's `id` to the element it creates, using an Extensible Storage schema (`Archilizer.FamilySpecId`, one string field). It also emits an `IdMap` JSON file (element ID → DSL ID) next to the `.rfa`.

The Flex Engine resolves failing element IDs through this map, so a report entry reads:

```json
{ "check": "regen.error", "message": "Constraints are not satisfied",
  "nodes": ["dim.frame_l", "inner_left"] }
```

The repairer then works on spec nodes. Revit element IDs never reach the model.

## 7. Types

```yaml
types:
  - name: 900 x 1200
    values: { width: 900 mm, height: 1200 mm }
  - name: 1200 x 1500
    values: { width: 1200 mm, height: 1500 mm }

# or, for large ranges:
typeCatalog:
  columns: [width, height, frame_mat]
  rows: from-product-data         # Mapper fills this from ProductData variants
```

Every type and catalog row is automatically added to the `FlexPlan` via the `TypeCatalog` sampler.

## 8. Repair patches

Repairs are JSON Patch (RFC 6902) operations over the JSON form of the spec:

```json
[
  { "op": "add", "path": "/parameters/2/guard", "value": { "min": "10 mm" } },
  { "op": "replace", "path": "/parameters/1/range/min", "value": "40 mm" }
]
```

Note the second operation: narrowing a range is a valid repair only when the product data supports it. The repairer must justify range changes against `ProductData`, and these patches are flagged for human review.

## 9. Not in v0.1 (yet)

- Formulas beyond Revit's own formula syntax (the spec passes formulas through unchanged).
- Detail/annotation elements: symbolic lines in plan, masking regions, 2D representations per view.
- Visibility by detail level (coarse/medium/fine).
- Connectors (MEP), host-based families (wall/face hosted), and curtain panel–specific templates.
- Material assets and appearance. v0.1 references materials by name only.

Each of these is expected to come in with the first archetype that needs it.

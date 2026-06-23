# ALPS Visio Add-In

A **Microsoft Visio VSTO add-in** that imports subject-oriented process models from
**PASS/ALPS** OWL files and draws them as native, editable Visio diagrams.

The add-in reads an OWL/RDF model through the external [`alps.net.api`](https://www.nuget.org/packages/alps.net.api)
library and renders each model element — subjects, messages, behaviours, states and
transitions — as shapes on the matching Visio stencil. Models that already carry
layout coordinates are placed exactly; models without coordinates are arranged by a
built-in auto-layout.

> **Platform note:** This is a .NET Framework 4.8 VSTO project with Office COM interop.
> It builds and runs **only on Windows** with Visual Studio 2022 and Visio installed.

---

## What is PASS / ALPS?

**PASS** (*Parallel Activity Specification Schema*) is a subject-oriented business
process modelling language: a process is described as a set of **subjects** (active
entities) that exchange **messages** and each follow their own **behaviour** — a state
machine of send/receive/do states connected by transitions.

**ALPS** (*Abstract Layered PASS*) extends PASS with a multi-layer concept. In this
add-in, a model with more than one layer is what distinguishes an ALPS model from a
plain PASS model.

Two diagram types are produced:

- **SID** — *Subject Interaction Diagram*: the subjects and the messages between them.
- **SBD** — *Subject Behaviour Diagram*: the internal state machine of one subject.

---

## Features

- Import a PASS/ALPS process model from an `.owl` file into Visio.
- Render the **SID** (subjects + message connectors) and one **SBD** page per subject.
- Place shapes from the coordinates in the OWL file when present.
- **Auto-layout** when the model has no coordinates: SBD states cascade into a tree,
  SID subjects line up in a row, message boxes centre on their connectors.
- Open the bundled ALPS/PASS stencils and show a layer explorer from the ribbon.

---

## Requirements

- Windows
- Visual Studio 2022 with the **Office/SharePoint development** workload
- Microsoft Visio (desktop)
- .NET Framework 4.8 developer pack

---

## Build & run

This is a **non-SDK MSBuild project** that uses a `packages.config`-style NuGet
restore (a `packages/` folder, not `<PackageReference>`).

1. Clone the repository and open `ALPS_Visio_Tools.sln` in Visual Studio 2022.
2. Restore NuGet packages (`nuget restore ALPS_Visio_Tools.sln`, or let VS auto-restore).
   A missing `packages/` folder is the usual cause of build errors. If `alps.net.api`
   cannot be found, configure the matching NuGet package source.
3. Build the solution (or `msbuild ALPS_Visio_Tools.sln /p:Configuration=Debug`).
4. Press **F5** — Visual Studio launches Visio with the add-in registered and the
   debugger attached. There is no command-line entry point.

The VSTO manifest is signed with a temporary key (`*_TemporaryKey.pfx`). See
[docs/publish_test_certificate](docs/publish_test_certificate) and the LaTeX notes in
[docs/latex](docs/latex) for certificate and publishing details. End-user installation
is described in **[docs/AddIn installation-guide.pdf](docs/AddIn%20installation-guide.pdf)**.

---

## Usage

After the add-in loads, an **ALPS/PASS ADDIN** ribbon tab appears with an **ALPS Tools**
group containing three buttons:

| Button | Action |
| --- | --- |
| **Import OWL** | Opens a file dialog; the chosen `.owl` model is parsed and drawn into Visio. |
| **Open ALPS/PASS Stencils** | Opens the bundled SID/SBD shape stencils. |
| **Show layer Explorer** | Opens the layer/model explorer window. |

Import a model into a **fresh Visio document** for the cleanest result. When the
stencil's welcome/license message box appears after the import, dismiss it — the SID
page keeps its name.

### Test models

The [`docs/`](docs) folder ships ready-to-import example models:

- `[Test]_Vacation_Request_2D.owl` — vacation-request process **with** coordinates.
- `[Test]_Vacation_Request.owl` — the same process **without** coordinates (auto-layout).
- `[Test]_AutoLayout_NoCoords.owl` — a branch/loop process **without** coordinates,
  built to exercise the SBD tree layout and the SID row layout.
- `[Test]_Escaping_Quotes_2D.owl` — a model whose labels contain `"` characters
  (exercises Visio formula escaping).

---

## Architecture

Everything centres on turning a parsed OWL model into Visio shapes:

1. **Startup** — `ThisAddIn` wires up Visio app events and builds the ribbon (`ALPSRibbon`).
2. **Trigger** — *Import OWL* calls `OWLImporter.Instance.Parse(file)`.
3. **Parse** — `OWLImporter` drives `alps.net.api`'s reader, loading the bundled
   ontologies plus the user's OWL into an in-memory `IPASSProcessModel` graph.
4. **Class substitution** — `VisioClassFactory` makes the parser instantiate this
   project's `Visio*` classes (which implement `IVisioImportable`) instead of the
   API's plain classes, so each model element knows how to draw itself.
5. **Render** — the model's `ImportToVisio()` walks the object graph
   (model → layer → subjects + messages → behaviour → states + transitions); each node
   delegates to an **import helper** (`IShapeImport`) that creates the shape and sets
   its ShapeSheet cells via `VisioHelper`.

Reference diagrams (in [`docs/`](docs)):

- [docs/ThisAddIn.svg](docs/ThisAddIn.svg) — startup and entry points
- [docs/OWLImporter.svg](docs/OWLImporter.svg) — the import pipeline
- [docs/PASSProcessModel.svg](docs/PASSProcessModel.svg) — the model object graph

Two interfaces define the rendering contract:

- `IVisioImportable.ImportToVisio(page)` — every drawable element.
- `IVisioImportableWithShape` adds `PrepareDimensions()` (returns `false` when the
  element has no coordinates) and `GetShape()`.

---

## Project structure

```
ALPS_Visio_Tools.sln                 Solution (single project)
ALPS_Visio_AddIn-rewrite/            The add-in
├── ThisAddIn.cs                      Startup, Visio event wiring
├── ALPSRibbon.cs                     Ribbon tab + buttons
├── OWLImporter.cs                    Parse + drive the import
├── VisioHelper.cs                    Visio COM helpers (shapes, pages, ShapeSheet)
├── ShapeFinder.cs                    Locates stencil masters
├── Constants.cs                      Visio constants (page types, properties, stencils)
├── OWLShapes/                        Model object graph — the Visio* classes
│   ├── IVisioImportable(.WithShape)  Rendering contract
│   ├── VisioClassFactory.cs          Substitutes Visio* classes during parsing
│   ├── ImportFunctionality/          IShapeImport helpers (Subject/State/Transition/…)
│   ├── InteractionDescribing/        Subjects, messages (SID level)
│   └── BehaviorDescribing/           States + transitions (SBD level)
├── PageManagement/                   Page/model controllers, snapping, geometry
├── UI/                               WPF windows (layer explorer, property dialogs)
└── Resources/                        Bundled ontologies, icons, strings
docs/                                 Diagrams, notes, install guide, test OWL models
```

---

## Current state & limitations

This codebase is an active refactor. The German design notes in
[ALPS_Visio_AddIn-rewrite/TODO.md](ALPS_Visio_AddIn-rewrite/TODO.md) are the
authoritative overview of the architecture and the open tasks. Highlights:

- Only the **first** model in an OWL file is imported (multi-model import is planned).
- `FullySpecifiedSubject` is the most complete subject type; several ontology features
  (e.g. group states, subject execution mapping, some transition types) are not yet
  rendered, partly because the API does not always match the ontology.
- Performance during import is dominated by Visio itself.

Additional notes and a deeper code walk-through live in
[docs/documentation.md](docs/documentation.md), with shape-data details in
[docs/Data in ShapeSheet.md](docs/Data%20in%20ShapeSheet.md).

---

## License & contact

This repository belongs to the ALPS/PASS tooling around
[@MatthesElstermann](https://github.com/MatthesElstermann). For questions about the
model semantics or the original add-in, that is the place to start.

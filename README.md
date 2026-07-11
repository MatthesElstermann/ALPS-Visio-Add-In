# ALPS Visio Add-In

A **Microsoft Visio VSTO add-in** for subject-oriented process modelling with
**PASS/ALPS**: it imports OWL process models and draws them as native, editable Visio
diagrams, re-arranges existing diagrams, verifies an implementation model against its
specification, and checks shape labels with an ML classifier plus LLM suggestions.

The heavy lifting of parsing OWL/RDF is delegated to the external
[`alps.net.api`](https://www.nuget.org/packages/alps.net.api) library; this add-in adds
the Visio layer on top.

> **Platform note:** This is a .NET Framework 4.8 VSTO project with Office COM interop.
> It builds and runs **only on Windows** with Visual Studio 2022 and Visio installed.

---

## What is PASS / ALPS?

**PASS** (*Parallel Activity Specification Schema*) is a subject-oriented business
process modelling language: a process is described as a set of **subjects** (active
entities) that exchange **messages** and each follow their own **behaviour** — a state
machine of send/receive/do states connected by transitions.

**ALPS** (*Abstract Layered PASS*) extends PASS with a multi-layer concept: abstract
layers specify *what must happen*, implementing layers refine *how*. In this add-in, a
model with more than one layer is what distinguishes an ALPS model from a plain PASS
model.

Two diagram types are produced and managed:

- **SID** — *Subject Interaction Diagram*: the subjects and the messages between them.
- **SBD** — *Subject Behaviour Diagram*: the internal state machine of one subject.

---

## The ribbon at a glance

After the add-in loads, an **ALPS/PASS ADDIN** ribbon tab appears with four groups:

| Group | Button | Action |
| --- | --- | --- |
| Standard Functions | **Open ALPS/PASS Stencils** | Opens the ALPS/PASS shape stencils from the *My Shapes* folder. |
| ALPS Layer Editing | **Show layer Explorer** | Opens the layer/model explorer (tree view of models, SID layers and SBD pages). |
| OWL PASS Tools | **Import OWL** | Imports a PASS/ALPS model from an `.owl` file and draws it. |
| OWL PASS Tools | **ALPS Verification** | Checks an implementation model against a specification model and shows a report with an overall verdict. |
| OWL PASS Tools | **PASS BPMN Converter** | *Not implemented yet* (placeholder carried over from the original add-in). |
| OWL PASS Tools | **Auto Arrange** | Re-arranges the active SID/SBD page from its shapes. Split button: click = left-to-right, arrow = pick **Left-Right** or **Top-Down**. |
| PASS NL Checker | **PASS NL Checker** | Checks every shape label with the local ML model and asks an LLM for better labels where invalid. |
| PASS NL Checker | **NL-Modell trainieren** | Retrains the NL Checker's local ML model from the bundled training data. |
| PASS NL Checker | **NL-Checker Einstellungen** | Choose the LLM provider (UniGPT/OpenAI/Anthropic) and per-provider model + API key for the label suggestions. |

The features in detail:

### Import OWL

Opens a file dialog, parses the chosen `.owl`/`.rdf` file through `alps.net.api` and
renders the model: one **SID page** per layer (subjects + message connectors) and one
**SBD page** per fully specified subject, linked to its subject shape.

- Shapes are placed at the **coordinates from the OWL file** when present.
- Models **without coordinates** are arranged by a built-in auto-layout (SBD states
  cascade into a tree, SID subjects line up in a row).
- Only the **first** model in a file is imported (multi-model import is planned).
- Labels containing quotes and duplicate page names are handled safely (escaping via
  `QuoteLiteral`, unique page names via `GetUniquePageName`).
- Failed imports show an error dialog with the full cause chain instead of failing
  silently.

### Auto Arrange

Re-arranges an **already drawn** SID or SBD page purely from its shapes — no parsed
model required. The state graph is rebuilt from the connectors' glue, then a layered
layout is applied (layer = longest path from a start state). Subjects line up in a row
or column. The whole operation is one undo scope, so a single **Ctrl+Z** reverts it.

### ALPS Verification

Picks a **specification** (abstract) and an **implementation** OWL model, pairs their
elements via the `implements` references and runs the SID checks: communication
restrictions, subject-type conformance, message-connector conformance. The raw check
output is shown in a window, followed by an **overall verdict**
(`BESTANDEN` / `NICHT BESTANDEN`) that also counts specification elements without an
implementation counterpart.

Ported from the KIT master-thesis prototype
([andikra/ALPS-Verification-Thesis](https://github.com/andikra)) — SBD checks are not
implemented yet; the verdict covers the SID level only.

### PASS NL Checker

Checks every relevant shape label in the active document with the **local ML model**
(offline): an **ML.NET binary classifier** predicts whether the label is a valid name
for its shape type (do/send/receive states, subjects, messages, …). The model is
trained on first use from a bundled training set and cached under
`%APPDATA%\ALPS_Visio_AddIn\nl_model.zip`. It can be retrained (replacing the cached
model) at any time via the **NL-Modell trainieren** button.

For labels judged invalid, an **LLM** is asked for two improved label suggestions.
The LLM is used **only for these suggestions** — the validity check itself always runs
locally. Three providers are supported (button *NL-Checker Einstellungen*) — the
**UniGPT endpoint of the University of Münster** (OpenAI-compatible,
default model `Llama-3.3-70B`), **OpenAI** (`gpt-4o-mini` by default) and
**Anthropic** (`claude-opus-4-8` by default; consider `claude-haiku-4-5` for lower
cost). Model name and API key are stored **per provider** in
`%APPDATA%\ALPS_Visio_AddIn\nl_checker_settings.json` (plain text; an old
`llm_api_key.txt` from earlier versions is migrated automatically). Without an API key
the check still runs — only the suggestions are skipped.

### Layer editing & snapping

The **layer explorer** shows all models in the document with their SID layers and SBD
pages and lets you edit layer names, priorities and the `extends` relation between
layers.

When a SID layer **extends** another layer, the extended layer is displayed as the
page background, and **extension shapes snap**: dragging an *ActorExtension* onto a
background subject (or a *StateExtension*/guard state onto a background state on SBD
pages) links the two — including the associated behaviour pages. Moving a snapped shape
away asks for confirmation / unsnaps it.

---

## Requirements

- Windows
- Visual Studio 2022 with the **Office/SharePoint development** workload
- Microsoft Visio (desktop)
- .NET Framework 4.8 developer pack
- The **ALPS/PASS stencils** in Visio's *My Shapes* folder (see below)

### Stencils

The add-in looks for the newest stencil files matching

```
Abstract PASS SID Visio Shapes v<version>.vssm
Abstract PASS SBD Visio Shapes v<version>.vssm
```

in the folder(s) configured as Visio's **My Shapes** path (`Application.MyShapesPath`).
Without them, opening stencils and importing models fails with a message that names the
expected file.

---

## Build, run & test

This is a **non-SDK MSBuild project** that uses a `packages.config`-style NuGet
restore (a `packages/` folder, not `<PackageReference>`).

1. Clone the repository and open `ALPS_Visio_Tools.sln` in Visual Studio 2022.
2. Restore NuGet packages (`nuget restore ALPS_Visio_Tools.sln`, or let VS
   auto-restore). A missing `packages/` folder is the usual cause of build errors.
3. Build the solution (or `msbuild ALPS_Visio_Tools.sln /p:Configuration=Debug`).
4. Press **F5** — Visual Studio launches Visio with the add-in registered and the
   debugger attached. There is no command-line entry point.

The VSTO manifest is signed with a temporary key (`*_TemporaryKey.pfx`). End-user
installation — including the certificate steps — is described in the
[installation guide](#end-user-installation-certificate-import) at the end of this
README (with screenshots in
[docs/AddIn installation-guide.pdf](docs/AddIn%20installation-guide.pdf)).

### Tests

`ALPS_Visio_AddIn.Tests/` is an NUnit test project (SDK-style, net48) covering the
**Visio-independent** logic — run it from the VS Test Explorer:

- `VisioHelperQuoteLiteralTests` — ShapeSheet string escaping
- `VisioHelperGetStencilTests` — SID/SBD stencil routing for every master
- `AlpsReaderWriterFactoryTests` — regression test for a CWD-dependent
  `alps.net.api` constructor bug the add-in works around
- `VerifierTests` — the whole verification pipeline (parse → check → verdict) on the
  example models in `docs/`

Anything that draws through Visio COM is not headless-testable and is verified
manually in Visio.

### Test models

The [`docs/`](docs) folder ships ready-to-import example models:

- `[Test]_Vacation_Request_2D.owl` — vacation-request process **with** coordinates.
- `[Test]_Vacation_Request.owl` — the same process **without** coordinates (auto-layout).
- `[Test]_AutoLayout_NoCoords.owl` — a branch/loop process **without** coordinates,
  built to exercise the SBD tree layout and the SID row layout.
- `[Test]_Escaping_Quotes_2D.owl` — a model whose labels contain `"` characters
  (exercises Visio formula escaping).
- `[Verif]_Spec_*.owl` / `[Verif]_Impl_*.owl` — specification/implementation pairs for
  the ALPS Verification.

Import a model into a **fresh Visio document** for the cleanest result.

---

## Architecture

Everything centres on turning a parsed OWL model into Visio shapes:

1. **Startup** — `ThisAddIn` wires up Visio app events (document/page/window) and
   builds the ribbon (`ALPSRibbon`). A `ModelController` tracks which pages belong to
   which model and feeds the layer explorer.
2. **Trigger** — *Import OWL* calls `OWLImporter.Instance.Parse(file)`.
3. **Parse** — `OWLImporter` drives `alps.net.api`'s reader, loading the bundled
   ontologies plus the user's OWL into an in-memory `IPASSProcessModel` graph. (The
   reader singleton is obtained through `AlpsReaderWriterFactory`, which works around a
   working-directory-dependent constructor bug in the library.)
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
  element has no coordinates — implemented centrally in `VisualizationBounds`) and
  `GetShape()`.

---

## Project structure

```
ALPS_Visio_Tools.sln                 Solution (add-in + test project)
ALPS_Visio_AddIn-rewrite/            The add-in
├── ThisAddIn.cs                      Startup, Visio event wiring
├── ALPSRibbon.cs                     Ribbon tab + all buttons
├── OWLImporter.cs                    Parse + drive the import
├── AlpsReaderWriterFactory.cs        Safe access to the alps.net.api reader singleton
├── AutoArranger.cs                   Auto Arrange (re-layout of drawn SID/SBD pages)
├── VisioHelper.cs                    Visio COM helpers (shapes, pages, ShapeSheet)
├── ShapeFinder.cs                    Locates the newest stencils in My Shapes
├── Constants.cs                      Visio constants (page types, properties, stencils)
├── OWLShapes/                        Model object graph — the Visio* classes
│   ├── IVisioImportable(.WithShape)  Rendering contract
│   ├── VisualizationBounds.cs        Shared PrepareDimensions implementation
│   ├── VisioClassFactory.cs          Substitutes Visio* classes during parsing
│   ├── ImportFunctionality/          IShapeImport helpers (Subject/State/Transition/…)
│   ├── InteractionDescribing/        Subjects, messages (SID level)
│   └── BehaviorDescribing/           States + transitions (SBD level)
├── PageManagement/                   Page/model controllers, snapping, geometry
├── UI/                               WPF windows (layer explorer, property dialogs)
├── NLChecker/                        PASS NL Checker (ML.NET + LLM) & API-key handling
├── Verification/                     ALPS Verification (SID checks + verdict)
└── Resources/                        Bundled ontologies, icons, strings
ALPS_Visio_AddIn.Tests/              NUnit tests for the Visio-independent logic
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
- The **PASS BPMN Converter** button is a placeholder — the feature does not exist yet.
- The **ALPS Verification** is a prototype: SID checks only, SBD checks are empty.
- The NL Checker's LLM side supports UniGPT (Uni Münster), OpenAI and Anthropic;
  other providers require a code change in `NLChecker/LlmClient.cs`.
- Performance during import is dominated by Visio itself.

Additional notes and a deeper code walk-through live in
[docs/documentation.md](docs/documentation.md), with shape-data details in
[docs/Data in ShapeSheet.md](docs/Data%20in%20ShapeSheet.md).

---

## License & contact

This repository belongs to the ALPS/PASS tooling around
[@MatthesElstermann](https://github.com/MatthesElstermann). For questions about the
model semantics or the original add-in, that is the place to start.

---

## End-user installation (certificate import)

*Markdown version of "Certificate import for Windows to install ALPS Visio Plugin" by
Matthes Elstermann and Lukas Gnad — the original PDF with screenshots is
[docs/AddIn installation-guide.pdf](docs/AddIn%20installation-guide.pdf).*

### Read before installation

The ALPS Visio plugin can only be installed if the plugin's certificate is trusted by
the Windows system. The certificate used to sign the plugin is a **testing
certificate**, so a standard Windows system will not accept it as coming from a trusted
authority.

To install anyway, the certificate can be imported into the *Trusted Root Certification
Authorities* store manually. **This is a potential security risk** — Windows will then
trust anything signed by this certificate — so it is **highly recommended to remove the
certificate again right after a successful installation** (see below).

### Installing the certificate

1. Go to the plugin directory, right-click **`setup.exe`** and choose **Properties**.
2. Open the **Digital Signatures** tab, select the certificate in the *Signature list*
   and click **Details**.
3. Under *Signer information*, click **View Certificate**.
4. In the certificate window, click **Install Certificate…** — the *Certificate Import
   Wizard* opens.
5. **Store location:** choose **Current User** (the certificate should not affect other
   users on the system) and continue.
6. **Certificate store:** do *not* let Windows pick the store automatically — select
   **"Place all certificates in the following store"** and browse to
   **Trusted Root Certification Authorities**. (With the automatic choice the
   certificate ends up in a store that is *not* consulted for software installation,
   and the plugin setup will still fail.)
7. Windows shows a **security warning** that it cannot validate the certificate and
   that you install it at your own risk. Confirm with **Yes** and finish the wizard.
8. Close all windows and run the plugin installation (`setup.exe`).

Afterwards, remove the certificate again (next section).

### Removing the installed certificate

Where the certificate lives depends on the store location chosen during the import:

- Installed for the **current user**: open the Windows search bar and run
  **"Manage user certificates"** (`certmgr.msc`).
- Installed for the **whole system**: open the Windows search bar and run
  **"Manage computer certificates"** (`certlm.msc`).

In the certificate manager, navigate to **Trusted Root Certification Authorities →
Certificates** in the tree on the left, locate the installed certificate (issued to
*Elstermann*) and delete it.

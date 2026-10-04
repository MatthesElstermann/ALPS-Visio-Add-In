using alps.net.api.ALPS;
using alps.net.api.ALPS.ALPSModelElements.ALPSSIDComponents;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static ALPS_Visio_AddIn_rewrite.Constants.Properties;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
	public class SubjectImport : PASSProcessModelElementImport
	{
		private readonly ISubject subject;

		/// <summary>
		/// Shape import for subject
		/// </summary>
		public SubjectImport(ISubject subject) : base(subject)
		{
			this.subject = subject;
		}

		public override void Import(string shapeType, Visio.Page page, IList<ISimple2DVisualizationPoint> bounds)
        {
			base.Import(shapeType, page, bounds);

            // TODO: hasSubjectExecutionMapping
            // VH.SetProperty(shape, Constants.Properties.ExecutionMapping, subject.getSubjectExecutionMapping().getExecutionMappingDefinition())
            // getImplementedInterfacesIDReferences() liefert die reinen URIs; das frueher
            // hier genutzte getImplementedInterfaces() (ein Dictionary) erzeugte ueber
            // string.Join eine "[key, value]"-Zeichenkette, die nicht wieder einlesbar war.
            // Defensiv: ein Fehler beim Auslesen der implements darf den Import nicht abbrechen.
            try { VH.SetProp(shape, Constants.Properties.Subject.Implements, string.Join(";", subject.getImplementedInterfacesIDReferences())); }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("SubjectImport implements failed: " + ex); }
            // TODO: final -> ont

            // MultiSubject
            VH.SetPropBool(shape, Constants.Properties.Subject.Multi, subject is IMultiSubject);
            // hasMaximumSubjectInstanceRestriction
            VH.SetProp(shape, Constants.Properties.MaximumNumberOfInstantiation, subject.getInstanceRestriction().ToString());

            // AbstractSubject
            VH.SetPropBool(shape, Constants.Properties.Subject.Abstract, subject.isAbstract());

            // FullySpecifiedSubject
            if (subject is IFullySpecifiedSubject fullySpecifiedSubject)
            {
                // TODO: hasInputPoolConstraint
                // fullySpecifiedSubject.getInputPoolConstraints()
                // TODO: hasDataDefinition
                // fullySpecifiedSubject.getSubjectDataDefinition()
                // TODO: containsBehavior
                // fullySpecifiedSubject.getBehaviors()
                // TODO: containsBaseBehavior
                if (fullySpecifiedSubject.getSubjectBaseBehavior() is IVisioImportable importable)
                {
                    // Sichtbarer Seitenname mit Typ-Praefix und dem LABEL des Subjekts ("SBD: Worker"),
                    // der universelle Name behaelt die stabile Modell-ID.
                    Visio.Page SBDPage = VH.CreateSBDPage(page, BehaviorPageName("SBD", fullySpecifiedSubject, page), ("" + fullySpecifiedSubject.getModelComponentID()), this.GetShape());
                    importable.ImportToVisio(SBDPage);
                }
            }

            if (subject is IStandaloneMacroSubject standaloneMacroSubject)
            {
                if (standaloneMacroSubject.getBehavior() is IVisioImportable importable)
                {
                    Visio.Page SBDPage = VH.CreateSBDPage(page, BehaviorPageName("SBD", standaloneMacroSubject, page), ("" + standaloneMacroSubject.getModelComponentID()), this.GetShape());
                    importable.ImportToVisio(SBDPage);
                }
            }

            // StartSubject
            VH.SetPropBool(shape, Constants.Properties.Subject.Start, subject.isRole(ISubject.Role.StartSubject));

            // InterfaceSubject
            if (subject is IInterfaceSubject interfaceSubject)
            {
                VH.SetHyperlink(shape, Constants.Properties.Subject.LinkedResource, interfaceSubject.getReferencedSubject()?.getModelComponentID());
            }

            // SubjectExtension / GuardExtension / MacroExtension: persist the subject<->subject
            // correspondence so the SBD/GBD background can be derived without a live SID snap
            // (see SBDPageController.tryDeriveExtends). The extended subject is referenced by its
            // model component ID; the snap derivation matches base subjects on that ID.
            if (subject is ISubjectExtension subjectExtension)
            {
                ISubject extended = subjectExtension.getExtendedSubject();
                if (extended != null)
                    VH.SetHyperlinkSubAddress(shape, Constants.Properties.ExtendedSubject, extended.getModelComponentID());

                // Draw each extension behavior (the GBD content) onto its own page. Guarded so a
                // failure on one behavior neither aborts the whole import nor leaves the document
                // in a half-drawn state.
                foreach (ISubjectBehavior extensionBehavior in subjectExtension.getExtensionBehaviors().Values)
                {
                    if (!(extensionBehavior is IVisioImportable importable)) continue;
                    try
                    {
                        // GBD nur fuer Guard-Verhalten; andere Erweiterungsverhalten sind SBDs.
                        // Benannt nach dem ERWEITERTEN Subjekt ("GBD: Worker (Layer_Guard)"), sonst
                        // nach der Erweiterung selbst.
                        // Seitentyp wie bei einer per Schablone angelegten GBD ("SubjectGuardBehavior").
                        // Mit dem SBD-Typ behandelte die Schablonen-VBA die Seite wie eine normale SBD
                        // und entfernte ihren Hintergrund (Basis-SBD nicht mehr sichtbar).
                        bool isGuard = extensionBehavior is IGuardBehavior;
                        Visio.Page gbdPage = VH.CreateSBDPage(page,
                            BehaviorPageName(isGuard ? "GBD" : "SBD", extended ?? subject, page),
                            "" + extensionBehavior.getModelComponentID(), this.GetShape(),
                            isGuard ? Constants.Properties.GBDPage : Constants.Properties.SBDPage);
                        importable.ImportToVisio(gbdPage);
                    }
                    catch (System.Exception e)
                    {
                        Debug.WriteLine($"[Import] GBD for '{extensionBehavior.getModelComponentID()}' failed: {e.Message}");
                    }
                }
            }

            // TODO: SubjectGroup
            if (subject is ISubjectGroup subjectGroup)
            {
                //subjectGroup.getContainedSubjects()

                if (subjectGroup is ISystemInterfaceSubject systemInterfaceSubject)
                {
                    //systemInterfaceSubject.getContainedInterfaceSubjects()
                }
            }
        }

        /// <summary>
        /// Seitenname einer Verhaltensseite: "SBD: Worker (Layer_Base)". Die Ebene stammt aus dem
        /// Namen der SID-Seite ("SID: Layer_Base"), auf der das Subjekt liegt.
        /// </summary>
        private static string BehaviorPageName(string kind, ISubject subject, Visio.Page sidPage)
        {
            string layer = sidPage.Name.StartsWith("SID: ") ? sidPage.Name.Substring("SID: ".Length) : sidPage.Name;
            return kind + ": " + VH.DisplayName(subject) + " (" + layer + ")";
        }
    }
}

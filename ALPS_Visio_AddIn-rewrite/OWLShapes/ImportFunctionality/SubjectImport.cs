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
			Debug.WriteLine($"[Import] >> {subject.GetType().Name} '{subject.getModelComponentID()}' (master '{shapeType}')");
			base.Import(shapeType, page, bounds);
			Debug.WriteLine($"[Import]    shape placed for '{subject.getModelComponentID()}'");

            // TODO: hasSubjectExecutionMapping
            // VH.SetProperty(shape, Constants.Properties.ExecutionMapping, subject.getSubjectExecutionMapping().getExecutionMappingDefinition())
            VH.SetProp(shape, Constants.Properties.Subject.Implements, string.Join(";", subject.getImplementedInterfaces()));
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
                    Visio.Page SBDPage = VH.CreateSBDPage(page, ("SBD: " + fullySpecifiedSubject.getModelComponentID()), ("" + fullySpecifiedSubject.getModelComponentID()), this.GetShape());
                    importable.ImportToVisio(SBDPage);
                }
            }

            if (subject is IStandaloneMacroSubject standaloneMacroSubject)
            {
                if (standaloneMacroSubject.getBehavior() is IVisioImportable importable)
                {
                    Visio.Page SBDPage = VH.CreateSBDPage(page, ("SBD: " + standaloneMacroSubject.getModelComponentID()), ("" + standaloneMacroSubject.getModelComponentID()), this.GetShape());
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
                Debug.WriteLine($"[Import]    ext block for '{subject.getModelComponentID()}'");
                ISubject extended = subjectExtension.getExtendedSubject();
                Debug.WriteLine($"[Import]    extendedSubject = '{extended?.getModelComponentID() ?? "null"}'");
                if (extended != null)
                {
                    VH.SetHyperlinkSubAddress(shape, Constants.Properties.ExtendedSubject, extended.getModelComponentID());
                    Debug.WriteLine("[Import]    extendedSubject link written");
                }

                // The extension behaviors (the GBD content) are drawn in part 2 of the import build-out.
                foreach (ISubjectBehavior extensionBehavior in subjectExtension.getExtensionBehaviors().Values)
                {
                    Debug.WriteLine($"[Import]    behavior '{extensionBehavior.getModelComponentID()}' " +
                                    $"({extensionBehavior.GetType().Name}) importable={extensionBehavior is IVisioImportable}");
                    if (!(extensionBehavior is IVisioImportable importable)) continue;
                    Visio.Page gbdPage = VH.CreateSBDPage(page,
                        "GBD: " + extensionBehavior.getModelComponentID(),
                        "" + extensionBehavior.getModelComponentID(), this.GetShape());
                    Debug.WriteLine("[Import]    GBD page created, drawing behavior...");
                    importable.ImportToVisio(gbdPage);
                    Debug.WriteLine("[Import]    GBD drawn");
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
    }
}

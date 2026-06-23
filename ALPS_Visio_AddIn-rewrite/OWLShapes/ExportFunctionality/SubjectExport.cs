using alps.net.api.ALPS;
using alps.net.api.ALPS.ALPSModelElements.ALPSSIDComponents;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using System.Linq;
using static ALPS_Visio_AddIn_rewrite.Constants.Properties;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
	public class SubjectExport : PASSProcessModelElementExport
	{
		private readonly ISubject subject;

		/// <summary>
		/// Shape export for subject
		/// </summary>
		public SubjectExport(ISubject subject) : base(subject)
		{
			this.subject = subject;
		}

		public override void Export(string shapeType, Visio.Page page, IList<ISimple2DVisualizationPoint> bounds)
        {
			base.Export(shapeType, page, bounds);

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
                if (fullySpecifiedSubject.getSubjectBaseBehavior() is IVisioExportable exportable)
                {
                    Visio.Page SBDPage = VH.CreateSBDPage(page, ("SBD: " + fullySpecifiedSubject.getModelComponentID()), ("" + fullySpecifiedSubject.getModelComponentID()), this.GetShape());
                    exportable.ExportToVisio(SBDPage);
                }
            }

            if (subject is IStandaloneMacroSubject standaloneMacroSubject)
            {
                if (standaloneMacroSubject.getBehavior() is IVisioExportable exportable)
                {
                    Visio.Page SBDPage = VH.CreateSBDPage(page, ("SBD: " + standaloneMacroSubject.getModelComponentID()), ("" + standaloneMacroSubject.getModelComponentID()), this.GetShape());
                    exportable.ExportToVisio(SBDPage);
                }
            }

            // StartSubject
            VH.SetPropBool(shape, Constants.Properties.Subject.Start, subject.isRole(ISubject.Role.StartSubject));

            // InterfaceSubject
            if (subject is IInterfaceSubject interfaceSubject)
            {
                VH.SetHyperlink(shape, Constants.Properties.Subject.LinkedResource, interfaceSubject.getReferencedSubject()?.getModelComponentID());
            }

            // TODO: SubectExtension
            if (subject is ISubjectExtension subjectExtension)
            {
                //subjectExtension.getExtendedSubject()
                //subjectExtension.getExtensionBehaviors()
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

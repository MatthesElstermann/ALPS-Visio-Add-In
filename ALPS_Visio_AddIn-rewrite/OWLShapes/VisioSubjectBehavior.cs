using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSubjectBehavior : SubjectBehavior, IVisioImportable
    {
        // Layout constants in drawing units (assumed mm for ALPS/PASS metric documents)
        private const double LayoutMarginX = 25.0;
        private const double LayoutMarginY = 25.0;
        private const double LayoutStepX = 70.0;
        private const double LayoutStepY = 40.0;

        public VisioSubjectBehavior(IModelLayer layer, string labelForID = null, ISubject subject = null, ISet<IBehaviorDescribingComponent> behaviorDescribingComponents = null, IState initialStateOfBehavior = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, subject, behaviorDescribingComponents, initialStateOfBehavior, priorityNumber, comment, additionalLabel, additionalAttribute) { }
        protected VisioSubjectBehavior() { }

        public void ImportToVisio(Visio.Page currentPage)
        {
            // TODO: set page dimensions
            // TODO: hasInitialState
            // TODO: ExtensionBehavior
            // TODO: GuardBehavior

            bool anyHadCoordinates = false;
            var importedStates = new List<IState>();

            // Import states first so transition connectors can glue to existing shapes
            foreach (IBehaviorDescribingComponent component in this.getBehaviorDescribingComponents().Values.OrderBy(c => c is ITransition))
            {
                if (!(component is IVisioImportable importable)) continue;

                if (importable is IVisioImportableWithShape shapeImportable)
                    if (shapeImportable.PrepareDimensions()) anyHadCoordinates = true;

                if (component is IState state)
                {
                    importable.ImportToVisio(currentPage);
                    importedStates.Add(state);
                }
                else if (component is ITransition)
                {
                    importable.ImportToVisio(currentPage);
                }
            }

            if (!anyHadCoordinates && importedStates.Count > 0)
                ApplyTreeLayout(importedStates, currentPage);
        }

        /// <summary>
        /// Positions state shapes in a cascading tree when the OWL file has no coordinates.
        /// Root states (no incoming transitions) start at the top; successors are placed to
        /// the right and below, giving an indented-tree visual similar to a flow chart.
        /// </summary>
        private void ApplyTreeLayout(IList<IState> states, Visio.Page page)
        {
            // Result["mm"] gives the value in mm regardless of the document's unit setting
            double pageHeightMM = page.PageSheet.CellsU[Constants.ShapeCells.PageHeight].Result["mm"];
            var visited = new HashSet<string>();
            double y = pageHeightMM - LayoutMarginY;

            var roots = states.Where(s => s.getIncomingTransitions().Count == 0).ToList();
            if (roots.Count == 0) roots.Add(states[0]);

            foreach (IState root in roots)
                y = PlaceSubtree(root, LayoutMarginX, y, visited) - LayoutStepY;
        }

        /// <summary>
        /// Recursively places a state and all reachable successors.
        /// Returns the Y coordinate immediately below the placed subtree.
        /// </summary>
        private double PlaceSubtree(IState state, double x, double y, HashSet<string> visited)
        {
            string id = state.getModelComponentID();
            if (visited.Contains(id)) return y;
            visited.Add(id);

            if (state is IVisioImportableWithShape importable)
            {
                Visio.Shape shape = importable.GetShape();
                if (shape != null)
                {
                    VH.SetCellMM(shape, Constants.ShapeCells.PinX, x);
                    VH.SetCellMM(shape, Constants.ShapeCells.PinY, y);
                }
            }

            double newY = y;
            foreach (ITransition transition in state.getOutgoingTransitions().Values)
            {
                IState target = transition.getTargetState();
                if (target != null && !visited.Contains(target.getModelComponentID()))
                    newY = PlaceSubtree(target, x + LayoutStepX, newY, visited) - LayoutStepY;
            }

            if (state.getOutgoingTransitions().Count > 0) newY += LayoutStepY;
            return newY;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSubjectBehavior();
        }
    }
}
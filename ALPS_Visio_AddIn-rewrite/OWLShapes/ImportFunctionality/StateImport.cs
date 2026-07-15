using System.Collections.Generic;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class StateImport : PASSProcessModelElementImport
    {
		private readonly IState state;

        /// <summary>
        /// Shape import for state
        /// </summary>
        public StateImport(IState state) : base(state)
        {
            this.state = state;
        }

        public override void Import(string shapeType, Visio.Page page, IList<ISimple2DVisualizationPoint> bounds)
        {
            base.Import(shapeType, page, bounds);

            // EndState
            VH.SetPropBool(shape, Constants.Properties.State.End, state.isStateType(IState.StateType.EndState));
            // InitialStateOfBehavior
            VH.SetPropBool(shape, Constants.Properties.State.Start, state.isStateType(IState.StateType.InitialStateOfBehavior));
            // AbstractState
            VH.SetPropBool(shape, Constants.Properties.State.Abstract, state.isStateType(IState.StateType.Abstract));
            // FinalizedState
            VH.SetPropBool(shape, Constants.Properties.State.Finalized, state.isStateType(IState.StateType.Finalized));

            // implements: URIs der umgesetzten Spezifikations-States (Grundlage der
            // ALPS-Verifikation). Reine ID-Referenzen, semikolongetrennt. Defensiv:
            // darf den Import nicht abbrechen.
            try { VH.SetProp(shape, Constants.Properties.State.Implements, string.Join(";", state.getImplementedInterfacesIDReferences())); }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("StateImport implements failed: " + ex); }

            // TODO: hasFunctionSpecification max 1 FunctionSpecification (hasToolSpecificDefinition exactly 1 string)
            // -> ReceiveFunction (EnvironmentChoice, AutoReceiveEarliest), SendFunction (Default), DoFunction (EnvironmentChoice, AutomaticEvaluation)

            // TODO: ChoiceSegment

            // TODO: GroupState // ONT + alps.net.api

            // TODO: StatePlaceHolder

            // all
            // inCycle // ont + api
            // implements
            // multiplicityLowerBound // ont + api
            // multiplicityUpperBound // ont + api

            // DoState
            // dataMappingIncoming
            // dataMappingOutgoing
        }
    }
}

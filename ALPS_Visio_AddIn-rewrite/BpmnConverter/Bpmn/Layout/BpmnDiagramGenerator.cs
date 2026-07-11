#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using PassBpmnConverter.Bpmn.BpmnDI;
using PassBpmnConverter.Bpmn.DC;
using PassBpmnConverter.Bpmn.DI;

namespace PassBpmnConverter.Bpmn;

public class BpmnDiagramGenerator
{
    private const double GridCellSize = 175;
    private const double ParticipantPadding = 75;
    private const double ParticipantSpacing = 75;

    private readonly Dictionary<IFlowElementsContainer, Grid?> _grids = new Dictionary<IFlowElementsContainer, Grid?>();

    public static void GenerateDiagram(IBpmnModel bpmnModel)
    {
        new BpmnDiagramGenerator().GenerateModelDiagram(bpmnModel);
    }

    private void GenerateModelDiagram(IBpmnModel bpmnModel)
    {
        if (bpmnModel == null || bpmnModel.Definitions == null)
            return;

        ICollaboration? collaboration = bpmnModel.Definitions.RootElements.OfType<ICollaboration>().FirstOrDefault();

        if (collaboration == null)
            return;

        GenerateLayout(collaboration);

        IBpmnPlane bpmnPlane = GenerateDiagram(collaboration);

        IBpmnDiagram bpmnDiagram = new BpmnDiagram()
        {
            Id = BpmnUtility.GenerateUniqueIdentifier(),
            BpmnPlane = bpmnPlane
        };

        bpmnModel.Definitions.Diagrams.Clear();
        bpmnModel.Definitions.Diagrams.Add(bpmnDiagram);
    }

    private void GenerateLayout(ICollaboration collaboration)
    {
        foreach (IParticipant participant in collaboration.Participants)
        {
            if (participant.ProcessRef != null)
            {
                _grids.Add(participant.ProcessRef, GenerateLayout(participant.ProcessRef));
            }
        }
    }

    private Grid? GenerateLayout(IFlowElementsContainer flowElementsContainer)
    {
        Grid grid = new Grid();

        Queue<(IFlowNode current, IFlowNode? previous)> stack = new Queue<(IFlowNode current, IFlowNode? previous)>();

        Dictionary<IFlowNode, List<IBoundaryEvent>> _boundaryEvents = new Dictionary<IFlowNode, List<IBoundaryEvent>>();

        IEnumerable<IFlowNode> initialFlowNodes = flowElementsContainer.FlowElements.OfType<IFlowNode>().Where(flowNode => flowNode.Incoming.Count == 0 && flowNode is not IBoundaryEvent);

        // make sure event sub processes are added below base process 
        initialFlowNodes = initialFlowNodes.OrderBy(flowNode => flowNode is ISubProcess);

        foreach (IFlowNode flowNode in initialFlowNodes)
        {
            stack.Enqueue((flowNode, null));
        }

        foreach (IBoundaryEvent boundaryEvent in flowElementsContainer.FlowElements.OfType<IBoundaryEvent>())
        {
            if (_boundaryEvents.TryGetValue(boundaryEvent.AttachedToRef, out List<IBoundaryEvent>? boundaryEvents))
            {
                boundaryEvents.Add(boundaryEvent);
            }
            else
            {
                _boundaryEvents[boundaryEvent.AttachedToRef] = [boundaryEvent];
            }
        }

        while (stack.TryDequeue(out var elements))
        {
            (IFlowNode current, IFlowNode? previous) = elements;

            if (grid.Contains(current))
                continue;

            if (current is not IBoundaryEvent && previous != null && grid.Contains(previous))
            {
                grid.AddAfter(current, previous);
            }
            else
            {
                grid.Add(current);

                if (current is IFlowElementsContainer subFlowElementsContainer)
                {
                    Grid? subGrid = GenerateLayout(subFlowElementsContainer);
                    _grids.Add(subFlowElementsContainer, subGrid);
                }
            }

            if (_boundaryEvents.TryGetValue(current, out List<IBoundaryEvent>? boundaryEvents))
            {
                foreach (IBoundaryEvent boundaryEvent in boundaryEvents)
                {
                    stack.Enqueue((boundaryEvent, current));
                }
            }

            foreach (ISequenceFlow sequenceFlow in current.Outgoing)
            {
                stack.Enqueue((sequenceFlow.TargetRef, current));
            }
            foreach (ISequenceFlow sequenceFlow in current.Incoming)
            {
                stack.Enqueue((sequenceFlow.SourceRef, current));
            }
        }

        return grid;
    }

    private IBpmnPlane GenerateDiagram(ICollaboration collaboration)
    {
        IBpmnPlane bpmnPlane = new BpmnPlane()
        {
            Id = GenerateDiagramIdentifier(collaboration),
            BpmnElement = collaboration,
        };

        double currentY = 0;

        foreach (IParticipant participant in collaboration.Participants)
        {
            IBounds participantBounds;

            if (participant.ProcessRef != null && _grids.TryGetValue(participant.ProcessRef, out Grid? grid) && grid != null)
            {
                List<IDiagramElement> diagramElements = GenerateDiagram(grid);

                IEnumerable<IBpmnShape> bpmnShapes = diagramElements.OfType<IBpmnShape>();
                double minX = bpmnShapes.Min(bpmnShape => bpmnShape.Bounds.X);
                double minY = bpmnShapes.Min(bpmnShape => bpmnShape.Bounds.Y);
                double maxX = bpmnShapes.Max(bpmnShape => bpmnShape.Bounds.X + bpmnShape.Bounds.Width);
                double maxY = bpmnShapes.Max(bpmnShape => bpmnShape.Bounds.Y + bpmnShape.Bounds.Height);

                double offsetX = -minX + ParticipantPadding;
                double offsetY = currentY - minY + ParticipantPadding;

                participantBounds = new Bounds()
                {
                    X = 0,
                    Y = currentY,
                    Width = maxX - minX + ParticipantPadding * 2,
                    Height = maxY - minY + ParticipantPadding * 2
                };

                // shift contained elements
                foreach (IDiagramElement diagramElement in diagramElements)
                {
                    if (diagramElement is IBpmnShape bpmnShape)
                    {
                        IBounds bounds = bpmnShape.Bounds;
                        bounds.X += offsetX;
                        bounds.Y += offsetY;
                        bpmnShape.Bounds = bounds;
                    }
                    if (diagramElement is IBpmnEdge bpmnEdge)
                    {
                        foreach (IPoint waypoint in bpmnEdge.Waypoints)
                        {
                            waypoint.X += offsetX;
                            waypoint.Y += offsetY;
                        }
                    }
                    bpmnPlane.DiagramElements.Add(diagramElement);
                }
            }
            else
            {
                (int width, int height) = GetDefaultShapeSize(participant);
                participantBounds = new Bounds()
                {
                    X = 0,
                    Y = currentY,
                    Width = width,
                    Height = height
                };
            }

            IBpmnShape participantBpmnShape = new BpmnShape()
            {
                Id = GenerateDiagramIdentifier(participant),
                BpmnElement = participant,
                Bounds = participantBounds,
            };
            bpmnPlane.DiagramElements.Add(participantBpmnShape);

            currentY = participantBounds.Y + participantBounds.Height + ParticipantSpacing;
        }

        return bpmnPlane;
    }

    private List<IDiagramElement> GenerateDiagram(Grid grid)
    {
        List<IBpmnShape> bpmnShapes = new List<IBpmnShape>();
        List<IDiagramElement> diagramElements = new List<IDiagramElement>();

        List<(IFlowNode flowNode, int row, int col)> elements = grid.GetAllElementsWithPosition();

        // flow nodes must be added before sequence flows
        foreach ((IFlowNode flowNode, int row, int col) in elements)
        {
            IBpmnShape shape = CreateShape(flowNode, row, col);

            if (flowNode is IBoundaryEvent boundaryEvent)
            {
                IBounds attachedToBounds = bpmnShapes.First(bpmnShape => bpmnShape.BpmnElement == boundaryEvent.AttachedToRef).Bounds;
                shape.Bounds = new Bounds()
                {
                    X = attachedToBounds.X + attachedToBounds.Width / 2 - shape.Bounds.Width / 2,
                    Y = attachedToBounds.Y + attachedToBounds.Height - shape.Bounds.Height / 2,
                    Width = shape.Bounds.Width,
                    Height = shape.Bounds.Height
                };
            }

            if (flowNode is IFlowElementsContainer flowElementsContainer)
            {
                if (_grids.TryGetValue(flowElementsContainer, out Grid? subGrid) && subGrid != null)
                {
                    List<IDiagramElement> subElements = GenerateDiagram(subGrid);
                    diagramElements.AddRange(subElements);
                }
            }

            bpmnShapes.Add(shape);
            diagramElements.Add(shape);
        }

        foreach (ISequenceFlow sequenceFlow in elements.Select(elements => elements.flowNode).SelectMany(flowNodes => flowNodes.Outgoing))
        {
            IBpmnShape? source = bpmnShapes.FirstOrDefault(diagramElement => diagramElement is IBpmnShape bpmnShape && bpmnShape.BpmnElement == sequenceFlow.SourceRef) as IBpmnShape;
            IBpmnShape? target = bpmnShapes.FirstOrDefault(diagramElement => diagramElement is IBpmnShape bpmnShape && bpmnShape.BpmnElement == sequenceFlow.TargetRef) as IBpmnShape;

            if (source == null || target == null)
            {
                Console.WriteLine($"Warning: Cannot find {nameof(IDiagramElement)} for source or target of {nameof(ISequenceFlow)}. {nameof(ISequenceFlow)} will not have a {nameof(IDiagramElement)}.");
                continue;
            }

            IBpmnEdge edge = CreateEdge(sequenceFlow, source, target);
            FixEdgeDockingPoints(edge, sequenceFlow.SourceRef, sequenceFlow.TargetRef);
            diagramElements.Add(edge);
        }

        return diagramElements;
    }

    private static string GenerateDiagramIdentifier(IBaseElement baseElement)
    {
        return baseElement.Id + "_di";
    }

    private static IBpmnShape CreateShape(IBaseElement baseElement, int row, int col)
    {
        (int width, int height) = GetDefaultShapeSize(baseElement);

        IBounds bounds = new Bounds()
        {
            X = (col * GridCellSize) - width / 2,
            Y = (row * GridCellSize) - height / 2,
            Width = width,
            Height = height
        };

        IBpmnShape bpmnShape = new BpmnShape()
        {
            Id = GenerateDiagramIdentifier(baseElement),
            BpmnElement = baseElement,
            Bounds = bounds,
        };

        return bpmnShape;
    }

    private static (int width, int height) GetDefaultShapeSize(IBaseElement element)
    {
        if (element is IParticipant)
        {
            return (400, 100);
        }

        if (element is ISubProcess)
        {
            return (100, 80);
        }

        if (element is ITask)
        {
            return (100, 80);
        }

        if (element is IGateway)
        {
            return (50, 50);
        }

        if (element is IEvent)
        {
            return (36, 36);
        }

        return (100, 80);
    }

    private static IBpmnEdge CreateEdge(ISequenceFlow sequenceFlow, IBpmnShape source, IBpmnShape target)
    {
        IBpmnEdge bpmnEdge = new BpmnEdge()
        {
            Id = GenerateDiagramIdentifier(sequenceFlow),
            BpmnElement = sequenceFlow,
            SourceElement = source,
            TargetElement = target,
            Waypoints = new List<IPoint>()
            {
                GetBoundsCenter(source.Bounds) ,
                GetBoundsCenter(target.Bounds)
            }
        };

        return bpmnEdge;
    }

    private static IPoint GetBoundsCenter(IBounds bounds)
    {
        return new Point { X = bounds.X + bounds.Width / 2, Y = bounds.Y + bounds.Height / 2 };
    }

    private static void FixEdgeDockingPoints(IBpmnEdge bpmnEdge, IFlowNode source, IFlowNode target)
    {
        if (bpmnEdge.Waypoints.Count != 2)
            return;

        IPoint from = bpmnEdge.Waypoints[0];
        IPoint to = bpmnEdge.Waypoints[1];

        double deltaX = to.X - from.X;
        double deltaY = to.Y - from.Y;

        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        double directionX = deltaX / distance;
        double directionY = deltaY / distance;

        double angleInRadians = Math.Atan2(directionY, directionX);
        double angleInDegrees = angleInRadians * (180.0 / Math.PI);

        (double offsetX, double offsetY) = GetOffset(angleInDegrees, source, true);
        from.X += offsetX;
        from.Y += offsetY;

        (offsetX, offsetY) = GetOffset(angleInDegrees, target, false);
        to.X += offsetX;
        to.Y += offsetY;
    }

    private static (double, double) GetOffset(double angleInDegrees, IFlowNode flowNode, bool isSource)
    {
        if (!isSource)
            angleInDegrees += 180;

        double offsetX = 0;
        double offsetY = 0;

        if (flowNode is IEvent)
        {
            offsetX = Math.Cos(DegToRad(angleInDegrees)) * 18;
            offsetY = Math.Sin(DegToRad(angleInDegrees)) * 18;
        }
        else if (flowNode is IGateway)
        {
            (offsetX, offsetY) = GetRectangleOffset(angleInDegrees + 45, 36, 36);
            (offsetX, offsetY) = RotatePoint(offsetX, offsetY, -45);
        }
        else if (flowNode is IActivity)
        {
            (offsetX, offsetY) = GetRectangleOffset(angleInDegrees, 100, 80);
        }

        return (offsetX, offsetY);
    }

    // adapted from: https://stackoverflow.com/questions/4061576/finding-points-on-a-rectangle-at-a-given-angle
    private static (double, double) GetRectangleOffset(double angleInDegrees, double width, double height)
    {
        double angle = DegToRad(angleInDegrees);
        double diag = Math.Atan2(height, width);
        double tangent = Math.Tan(angle);

        double x;
        double y;

        if (angle > -diag && angle <= diag)
        {
            x = width / 2f;
            y = width / 2f * tangent;
        }
        else if (angle > diag && angle <= Math.PI - diag)
        {
            x = height / 2f / tangent;
            y = height / 2f;
        }
        else if (angle > Math.PI - diag && angle <= Math.PI + diag)
        {
            x = -width / 2f;
            y = -width / 2f * tangent;
        }
        else
        {
            x = -height / 2f / tangent;
            y = -height / 2f;
        }

        return (x, y);
    }

    // adapted from: https://stackoverflow.com/questions/13695317/rotate-a-point-around-another-point
    private static (double, double) RotatePoint(double x, double y, double angleInDegrees)
    {
        double angle = DegToRad(angleInDegrees);
        double cosTheta = Math.Cos(angle);
        double sinTheta = Math.Sin(angle);
        return (cosTheta * x - sinTheta * y, sinTheta * x + cosTheta * y);
    }

    private static double DegToRad(double degrees)
    {
        if (degrees > 360)
            degrees -= 360;
        if (degrees < 0)
            degrees += 360;

        return Math.PI / 180 * degrees;
    }
}
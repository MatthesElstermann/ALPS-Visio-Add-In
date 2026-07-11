#nullable enable
using System;
using System.Collections.Generic;
namespace PassBpmnConverter.Bpmn;

public class Grid
{
    private readonly List<List<IFlowNode?>> _grid = new List<List<IFlowNode?>>();

    public bool Contains(IFlowNode element)
    {
        for (int row = 0; row < _grid.Count; row++)
        {
            for (int col = 0; col < _grid[row].Count; col++)
            {
                if (_grid[row][col] == element)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Add(IFlowNode element)
    {
        Set(element, _grid.Count, 0);
    }

    public void AddAfter(IFlowNode newElement, IFlowNode previousElement)
    {
        (int row, int col) = FindPosition(previousElement);

        IFlowNode? element = Get(row, col + 1);
        if (element == null)
        {
            Set(newElement, row, col + 1);
        }
        else
        {
            InsertBelow(newElement, row, col + 1);
        }
    }

    private void InsertBelow(IFlowNode element, int row, int col)
    {
        _grid.Insert(row + 1, new List<IFlowNode?>());

        Set(element, row + 1, col);
    }

    public IFlowNode? Get(int row, int col)
    {
        if (row >= _grid.Count || col >= _grid[row].Count)
            return null;

        return _grid[row][col];
    }

    public void Set(IFlowNode element, int row, int col)
    {
        while (row >= _grid.Count)
        {
            _grid.Add(new List<IFlowNode?>());
        }

        while (col >= _grid[row].Count)
        {
            _grid[row].Add(null);
        }

        _grid[row][col] = element;
    }

    public (int row, int col) FindPosition(IFlowNode element)
    {
        for (int row = 0; row < _grid.Count; row++)
        {
            for (int col = 0; col < _grid[row].Count; col++)
            {
                if (_grid[row][col] == element)
                {
                    return (row, col);
                }
            }
        }

        throw new InvalidOperationException($"Could not find element {element} in grid.");
    }

    public List<(IFlowNode element, int row, int col)> GetAllElementsWithPosition()
    {
        List<(IFlowNode element, int row, int col)> elements = new List<(IFlowNode element, int row, int col)>();

        for (int row = 0; row < _grid.Count; row++)
        {
            for (int col = 0; col < _grid[row].Count; col++)
            {
                IFlowNode? element = _grid[row][col];
                if (element != null)
                {
                    elements.Add((element, row, col));
                }
            }
        }

        return elements;
    }
}

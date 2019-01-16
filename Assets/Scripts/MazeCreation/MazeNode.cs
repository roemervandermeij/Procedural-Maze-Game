using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class representing individual maze node, specifying all possible neighbors, 
/// neighbors it is connected with.
/// </summary>
public class MazeNode : System.IComparable<MazeNode>
{
    public Vector3 Position { get; private set; }
    public string Identifier { get; private set; }
    public List<int> shortestPathInd; // FIXME ugh, there has to be a nicer way 
    public bool IsActive { get; set; } = true;
    public bool OnDeadEnd;
    public bool OnLoop;
    public bool NotConnectedToPath;
    public List<MazeNode> AllNeighbors { get; private set; } // FIXME implement as readonlylist getter, or, make an own mazenodelist...
    public List<MazeNode> ConnectedNeighbors { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="MazeNode"/> class.
    /// </summary>
    /// <param name="position">Position of the node.</param>
    /// <param name="identifier">Unique identifier for the node.</param>
    public MazeNode(Vector3 position, string identifier)
    {
        this.Position = position;
        this.Identifier = identifier;
        this.AllNeighbors = new List<MazeNode>();
        this.ConnectedNeighbors = new List<MazeNode>();
        this.shortestPathInd = new List<int>();
    }


    /// <summary>
    /// Increments the shortest path indices by one.
    /// </summary>
    public void IncrementShortestPathIndices()
    {
        if (shortestPathInd.Count > 0)
        {
            List<int> newShortestPathInd = new List<int>(shortestPathInd.Count);
            foreach (int ind in shortestPathInd)
            { newShortestPathInd.Add(ind + 1); }
            shortestPathInd = newShortestPathInd;
        }
    }

    /// <summary>
    /// Remove connection between node and neighbor.
    /// </summary>
    /// <param name="neighbor">Node with which connection to remove.</param>
    public void RemoveConnectionByReference(MazeNode neighbor)
    {
        if (neighbor == this) { throw new System.ArgumentException("Neighbor cannot be self."); }
        else
        {
            ConnectedNeighbors.Remove(neighbor);
            neighbor.ConnectedNeighbors.Remove(this);
        }
    }
    /// <summary>
    /// Remove connection between node and neighbors.
    /// </summary>
    /// <param name="neighbors">List of nodes to remove reciprocally.</param>
    public void RemoveConnectionByReference(List<MazeNode> neighbors)
    {
        while (neighbors.Count > 0)
        {
            RemoveConnectionByReference(neighbors[neighbors.Count - 1]);
            neighbors.RemoveAt(neighbors.Count - 1);
        }
    }
    /// <summary>
    /// Remove AllNeighbors connection between node and neighbor.
    /// </summary>
    /// <param name="neighbor">Node with which connection to remove.</param>
    public void RemoveBaseConnectionByReference(MazeNode neighbor)
    {
        if (neighbor == this) { throw new System.ArgumentException("Neighbor cannot be self."); }
        else
        {
            AllNeighbors.Remove(neighbor);
            neighbor.AllNeighbors.Remove(this);
        }
    }
    /// <summary>
    /// Remove AllNeighbors connection between node and neighbors.
    /// </summary>
    /// <param name="neighbors">List of nodes to remove reciprocally.</param>
    public void RemoveBaseConnectionByReference(List<MazeNode> neighbors)
    {
        neighbors = new List<MazeNode>(neighbors);
        while (neighbors.Count > 0)
        {
            RemoveBaseConnectionByReference(neighbors[neighbors.Count - 1]);
            neighbors.RemoveAt(neighbors.Count - 1);
        }
    }

    /// <summary>
    /// Adds connection to <paramref name="neighbor"/> in <see cref="ConnectedNeighbors"/>.
    /// </summary>
    /// <param name="neighbor">Neighboring node.</param>
    public void AddConnectionByReference(MazeNode neighbor)
    {
        CheckIfPartOfAllNeighborsByReference(neighbor); // if this goes through, connection is appropriate
        neighbor.CheckIfPartOfAllNeighborsByReference(this); // if this goes through, connection is appropriate
        //if (!ConnectedNeighbors.Contains(neighbor))
        if (!ConnectedNeighbors.Contains(neighbor)) { ConnectedNeighbors.Add(neighbor); }
        if (!neighbor.ConnectedNeighbors.Contains(this)) { neighbor.ConnectedNeighbors.Add(this); }
    }
    /// <summary>
    /// Add connections to <paramref name="neighbors"/> in <see cref="ConnectedNeighbors"/>.
    /// </summary>
    /// <param name="neighbors">Neighbors.</param>
    public void AddConnectionByReference(List<MazeNode> neighbors)
    {
        foreach (MazeNode neighbor in neighbors) { AddConnectionByReference(neighbor); }
    }
    /// <summary>
    /// Adds connection to node with <paramref name="identifier"/> in <see cref="ConnectedNeighbors"/>.
    /// </summary>
    /// <param name="identifier">Neighboring node identifier.</param>
    public void AddConnectionByIdentifier(string identifier)
    {
        CheckIfPartOfAllNeighborsByIdentifier(identifier); // if this goes through, connection is appropriate
        MazeNode neighbor = AllNeighbors.Find(x => x.Identifier == identifier);
        AddConnectionByReference(neighbor);
        //if (!ConnectedNeighbors.Contains(neighbor)) { ConnectedNeighbors.Add(neighbor); }
        //if (!neighbor.ConnectedNeighbors.Contains(this)) { neighbor.ConnectedNeighbors.Add(this); }
    }


    /// <summary>
    /// Renames the node.
    /// </summary>
    /// <param name="identifier">Identifier.</param>
    public void RenameNode(string identifier)
    {
        Identifier = identifier;
    }

    /// <summary>
    /// Adds connection to <paramref name="neighbor"/> in <see cref="AllNeighbors"/>.
    /// </summary>
    /// <param name="neighbor">Neighboring node.</param>
    public void AddBaseConnectionByReference(MazeNode neighbor)
    {
        if (AllNeighbors.Find(x => x.Identifier == neighbor.Identifier) == null)
        { AllNeighbors.Add(neighbor); }
        if (neighbor.AllNeighbors.Find(x => x.Identifier == this.Identifier) == null)
        { neighbor.AllNeighbors.Add(this); }
    }

    /// <summary>
    /// Add connections to <paramref name="neighbors"/> in <see cref="AllNeighbors"/>.
    /// </summary>
    /// <param name="neighbors">Neighbors.</param>
    public void AddBaseConnectionByReference(List<MazeNode> neighbors)
    {
        foreach (MazeNode neighbor in neighbors) { AddBaseConnectionByReference(neighbor); }
    }

    /// <summary>
    /// Checks if node is part of <see cref="AllNeighbors"/>, by reference.
    /// </summary>
    /// <param name="other">Node.</param>
    public void CheckIfPartOfAllNeighborsByReference(MazeNode other)
    {
        if (!AllNeighbors.Contains(other)) { throw new System.ArgumentException("Node not found among AllNeighbors field."); }
    }
    /// <summary>
    /// Checks if node is part of <see cref="AllNeighbors"/>, by identifier.
    /// </summary>
    /// <param name="identifier">Node identifier.</param>
    public void CheckIfPartOfAllNeighborsByIdentifier(string identifier)
    {
        if (AllNeighbors.Find(x => x.Identifier == identifier) == null) { throw new System.ArgumentException("Node not found among AllNeighbors field."); }
    }

    /// <summary>
    /// Translate the node's position by <paramref name="translation"/>.
    /// </summary>
    /// <param name="translation">Translation.</param>
    public void Translate(Vector3 translation)
    {
        this.Position = this.Position + translation;
    }

    /// <summary>
    /// Translate the node's position by <paramref name="rotation"/>, from the
    /// perspective of 0,0,0.
    /// </summary>
    /// <param name="rotation">Rotation.</param>
    public void Rotate(Quaternion rotation)
    {
        this.Position = rotation * this.Position;
    }

    /// <summary>
    /// Scale the node's position by multiplying position with <paramref name="scale"/>.
    /// </summary>
    /// <param name="scale">Scale.</param>
    public void Scale(float scale)
    {
        this.Position = this.Position * scale;
    }

    /// <summary>
    /// Implement IComparable CompareTo method - provide default sort order.
    /// </summary>
    /// <returns>Relative position in sort order</returns>
    /// <param name="other">Other.</param>
    public int CompareTo(MazeNode other)
    {
        return System.String.Compare(this.Identifier, other.Identifier);
    }


    /// <summary>
    /// Does identifier contains any of the strings in list.
    /// </summary>
    /// <returns><c>true</c>, if identifier contains string, <c>false</c> otherwise.</returns>
    /// <param name="keys">String.</param>
    public bool DoesIdentifierContainString(List<string> keys)
    {
        bool answer = false;
        foreach (string key in keys)
        { answer = Identifier.Contains(key); if (answer) break; }
        return answer;
    }

    /// <summary>
    /// Does identifier contains string.
    /// </summary>
    /// <returns><c>true</c>, if identifier contains string, <c>false</c> otherwise.</returns>
    /// <param name="key">String.</param>
    public bool DoesIdentifierContainString(string key)
    {
        return Identifier.Contains(key);
    }

}

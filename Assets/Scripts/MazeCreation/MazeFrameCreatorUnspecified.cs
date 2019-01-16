using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class used for creating a maze from provided list of maze nodes.
/// </summary>
public class MazeFrameCreatorUnspecified : MazeFrameCreator
{

    private readonly List<MazeNode> listOfMazeNodes;

    /// <summary>
    /// Initializes a new instance of the <see cref="MazeFrameCreatorUnspecified"/> class 
    /// </summary>
    /// <param name="listOfMazeNodes">List of nodes to use as maze base.</param>
    public MazeFrameCreatorUnspecified(List<MazeNode> listOfMazeNodes)
    {
        this.listOfMazeNodes = listOfMazeNodes;
    }

    /// <summary>
    /// Generate empty maze frame.
    /// </summary>
    /// <returns>The empty maze frame.</returns>
    public override MazeFrame GenerateEmptyMazeFrame()
    {
        List<MazeNode> mazeBase = GetMazeBase();
        SetStartAndEndNodes(ref mazeBase);
        return new MazeFrame(mazeBase, Scale, GetMinPathLength(), GetMaxPathLength(), null);
    }



    /// <summary>
    /// Gets the minimum path length based on size.
    /// </summary>
    /// <returns>The minimum path length.</returns>
    protected override int GetMinPathLength()
    {
        return 1;
    }

    /// <summary>
    /// Gets the maximum path length based on size.
    /// </summary>
    /// <returns>The max path length.</returns>
    protected override int GetMaxPathLength()
    {
        return listOfMazeNodes.Count;
    }


    /// <summary>
    /// Creates a list of nodes, arranged in a 3D square grid, to be used for 
    /// building a maze using one of the implemented algorithms.
    /// </summary>
    /// <returns>List of nodes.</returns>
    private List<MazeNode> GetMazeBase()
    {
        return listOfMazeNodes;
    }

    /// <summary>
    /// Sets the start and end nodes.
    /// </summary>
    /// <param name="mazeBase">Maze base.</param>
    protected override void SetStartAndEndNodes(ref List<MazeNode> mazeBase)
    {
        // Start and end node are unspecified and will not be labeled.
        // Sort for later easy reference
        mazeBase.Sort();
    }

    /// <summary>
    /// Gets quadrants.
    /// </summary>
    /// <returns>The quadrants.</returns>
    /// <param name="mazeBase">Maze base.</param>
    protected override List<List<MazeNode>> GetQuadrants(List<MazeNode> mazeBase)
    {
        throw new System.NotImplementedException();
    }

}

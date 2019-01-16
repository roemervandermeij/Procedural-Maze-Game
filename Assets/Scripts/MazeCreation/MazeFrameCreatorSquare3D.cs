using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class used for creating a square 3D maze (i.e. a cube).
/// </summary>
public class MazeFrameCreatorSquare3D : MazeFrameCreator // FIXME add mazebasebase saving
{

    /// <summary>
    /// Gets or sets the size of the maze frame to be created (converted to odd numbers).
    /// </summary>
    /// <value>Size of maze.</value>
    public Vector3Int Size { get; private set; }

    // Maze neighbor options
    private bool allowBackwards = true;
    private bool allowSlanted = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MazeFrameCreatorSquare3D"/> class 
    /// and set the size of the maze to be created.
    /// </summary>
    /// <param name="size">Size.</param>
    public MazeFrameCreatorSquare3D(Vector3Int size, int numberOfQuadrants = 0)
    {
        if ((size.x % 2) == 0) { size.x++; }
        if ((size.y % 2) == 0) { size.y++; }
        if ((size.z % 2) == 0) { size.z++; }
        this.Size = size;
        if (numberOfQuadrants != 0)
        { throw new System.NotImplementedException(); }
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
        return Size.z;
    }

    /// <summary>
    /// Gets the maximum path length based on size.
    /// </summary>
    /// <returns>The max path length.</returns>
    protected override int GetMaxPathLength()
    {
        return Size.x * Size.y * Size.z;
    }


    /// <summary>
    /// Creates a list of nodes, arranged in a 3D square grid, to be used for 
    /// building a maze using one of the implemented algorithms.
    /// </summary>
    /// <returns>List of nodes.</returns>
    private List<MazeNode> GetMazeBase()
    {
        List<MazeNode> mazeBase = new List<MazeNode>(Size.x * Size.y * Size.z);

        // The following creates a Square maze defined by a square set of 0-indexed 
        // incrementing xyz indices.


        // Generate all nodes
        for (int ix = 0; ix < Size.x; ix++)
        {
            for (int iy = 0; iy < Size.y; iy++)
            {
                for (int iz = 0; iz < Size.z; iz++)
                {
                    // Set random jitter
                    float[] randJitter = { Random.Range(-(jitter * Scale.x), jitter * Scale.x), Random.Range(-(jitter * Scale.y), jitter * Scale.y), Random.Range(-(jitter * Scale.z), jitter * Scale.z) };
                    mazeBase.Add(new MazeNode(new Vector3(ix * Scale.x + randJitter[0], iy * Scale.y + randJitter[1], iz * Scale.z + randJitter[2]), "." + ix + "." + iy + "." + iz));
                }
            }
        }

        // Generate neighbors
        for (int inode = 0; inode < mazeBase.Count; inode++)
        {
            // Parse identifier
            string[] identifier = mazeBase[inode].Identifier.Split('.'); // FIXME first element is "" --> use syntax string.Split(new char[] { '-' }, System.StringSplitOptions.RemoveEmptyEntries)
            int[] gather = new int[3];
            int count = 0;
            for (int i = 0; i < identifier.Length; i++)
            {
                if (identifier[i].Length != 0)
                {
                    gather[count] = System.Convert.ToInt32(identifier[i]);
                    count++;
                }
            }
            int ix = gather[0];
            int iy = gather[1];
            int iz = gather[2];

            // Six possible neighbors in straight directions ( ..Size and 0 indicate the boundaries nodes)
            // Up (x y+1 z)
            if (iy != Size.y - 1)
            {
                string neighborIdentifier = ("." + (ix) + "." + (iy + 1) + "." + (iz)); // xyz
                mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
            }
            // Down (x y-1 z)
            if (iy != 0)
            {
                string neighborIdentifier = ("." + (ix) + "." + (iy - 1) + "." + (iz)); // xyz
                mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
            }
            // Left (x-1 y z)
            if (ix != 0)
            {
                string neighborIdentifier = ("." + (ix - 1) + "." + (iy) + "." + (iz)); // xyz
                mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
            }
            // Right (x+1 y z)
            if (ix != Size.x - 1)
            {
                string neighborIdentifier = ("." + (ix + 1) + "." + (iy) + "." + (iz)); // xyz
                mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
            }
            // Forward (x y z+1)
            if (iz != Size.z - 1)
            {
                string neighborIdentifier = ("." + (ix) + "." + (iy) + "." + (iz + 1)); // xyz
                mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
            }
            if (allowBackwards)
            {// Backward (x y z-1)
                if (iz != 0)
                {
                    string neighborIdentifier = ("." + (ix) + "." + (iy) + "." + (iz - 1)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }
            }

            if (allowSlanted)
            {
                // 16 Additional slanted directions! 
                // Forward-left-center  (x-1 y z+1)
                if ((ix != 0) && (iz != Size.z - 1))
                {
                    string neighborIdentifier = ("." + (ix - 1) + "." + (iy + 0) + "." + (iz + 1)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }
                // Forward-right-center (x+1 y z+1)
                if ((ix != Size.x - 1) && (iz != Size.z - 1))
                {
                    string neighborIdentifier = ("." + (ix + 1) + "." + (iy + 0) + "." + (iz + 1)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }
                // Forward-left-up      (x-1 y+1 z+1)
                if ((ix != 0) && (iy != Size.y - 1) && (iz != Size.z - 1))
                {
                    string neighborIdentifier = ("." + (ix - 1) + "." + (iy + 1) + "." + (iz + 1)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }
                // Forward-right-up     (x+1 y+1 z+1)
                if ((ix != Size.x - 1) && (iy != Size.y - 1) && (iz != Size.z - 1))
                {
                    string neighborIdentifier = ("." + (ix + 1) + "." + (iy + 1) + "." + (iz + 1)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }
                // Forward-left-down    (x-1 y-1 z+1)
                if ((ix != 0) && (iy != 0) && (iz != Size.z - 1))
                {
                    string neighborIdentifier = ("." + (ix - 1) + "." + (iy - 1) + "." + (iz + 1)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }
                // Forward-right-down   (x+1 y-1 z+1)
                if ((ix != Size.x - 1) && (iy != 0) && (iz != Size.z - 1))
                {
                    string neighborIdentifier = ("." + (ix + 1) + "." + (iy - 1) + "." + (iz + 1)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }


                // Center-left-up       (x-1 y+1 z)
                if ((ix != 0) && (iy != Size.y - 1))
                {
                    string neighborIdentifier = ("." + (ix - 1) + "." + (iy + 1) + "." + (iz + 0)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }
                // Center-right-up      (x+1 y+1 z)
                if ((ix != Size.x - 1) && (iy != Size.y - 1))
                {
                    string neighborIdentifier = ("." + (ix + 1) + "." + (iy + 1) + "." + (iz + 0)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }
                // Center-left-down     (x-1 y-1 z)
                if ((ix != 0) && (iy != 0))
                {
                    string neighborIdentifier = ("." + (ix - 1) + "." + (iy - 1) + "." + (iz + 0)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }
                // Center-right-down    (x+1 y-1 z)
                if ((ix != Size.x - 1) && (iy != 0))
                {
                    string neighborIdentifier = ("." + (ix + 1) + "." + (iy - 1) + "." + (iz + 0)); // xyz
                    mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                }

                if (allowBackwards)
                {
                    // Backward-left-center  (x-1 y z-1)
                    if ((ix != 0) && (iz != 0))
                    {
                        string neighborIdentifier = ("." + (ix - 1) + "." + (iy + 0) + "." + (iz - 1)); // xyz
                        mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                    }
                    // Backward-right-center (x+1 y z-1)
                    if ((ix != Size.x - 1) && (iz != 0))
                    {
                        string neighborIdentifier = ("." + (ix + 1) + "." + (iy + 0) + "." + (iz - 1)); // xyz
                        mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                    }
                    // Backward-left-up      (x-1 y+1 z-1)
                    if ((ix != 0) && (iy != Size.y - 1) && (iz != 0))
                    {
                        string neighborIdentifier = ("." + (ix - 1) + "." + (iy + 1) + "." + (iz - 1)); // xyz
                        mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                    }
                    // Backward-right-up     (x+1 y+1 z-1)
                    if ((ix != Size.x - 1) && (iy != Size.y - 1) && (iz != 0))
                    {
                        string neighborIdentifier = ("." + (ix + 1) + "." + (iy + 1) + "." + (iz - 1)); // xyz
                        mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                    }
                    // Backward-left-down    (x-1 y-1 z-1)
                    if ((ix != 0) && (iy != 0) && (iz != 0))
                    {
                        string neighborIdentifier = ("." + (ix - 1) + "." + (iy - 1) + "." + (iz - 1)); // xyz
                        mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                    }
                    // Backward-right-down   (x+1 y-1 z-1)
                    if ((ix != Size.x - 1) && (iy != 0) && (iz != 0))
                    {
                        string neighborIdentifier = ("." + (ix + 1) + "." + (iy - 1) + "." + (iz - 1)); // xyz
                        mazeBase[inode].AllNeighbors.Add(mazeBase.Find(x => x.Identifier == neighborIdentifier));
                    }
                }
            }
        }

        // Sort for later easy reference
        mazeBase.Sort();

        return mazeBase;
    }

    /// <summary>
    /// Sets the start and end nodes.
    /// </summary>
    /// <param name="mazeBase">Maze base.</param>
    protected override void SetStartAndEndNodes(ref List<MazeNode> mazeBase)
    {
        //// Add start and end nodes extending out from the maze
        //int distToMaze = 3;// in node index units
        //                   // First find set entry and exit nodes, default as center value in XY plane at most 
        //                   // backwards and most forward z coordinate respectively
        //int[] entryInd = { Size.x / 2, Size.y / 2, 0 }; // int division floors, but since it's zero indexed, this results in the center of the xy-plane
        //int[] exitInd = { Size.x / 2, Size.y / 2, (Size.z - 1) }; // int division floors, but since it's zero indexed, this results in the center of the xy-plane
        //MazeNode entryNode = mazeBase.Find(x => x.Identifier == ("." + entryInd[0] + "." + entryInd[1] + "." + entryInd[2]));
        //MazeNode exitNode = mazeBase.Find(x => x.Identifier == ("." + exitInd[0] + "." + exitInd[1] + "." + exitInd[2]));

        //// Add start node
        //MazeNode startNode = new MazeNode(new Vector3(entryInd[0] * Scale.x, entryInd[1] * Scale.y, (entryInd[2] - distToMaze) * Scale.z), "start");
        //startNode.AllNeighbors.Add(entryNode);
        //entryNode.AllNeighbors.Add(startNode);
        //mazeBase.Add(startNode);

        //// Add end node
        //MazeNode endNode = new MazeNode(new Vector3(exitInd[0] * Scale.x, exitInd[1] * Scale.y, (exitInd[2] + distToMaze) * Scale.z), "end");
        //endNode.AllNeighbors.Add(exitNode);
        //exitNode.AllNeighbors.Add(endNode);
        //mazeBase.Add(endNode);

        // Set start and end nodes, as center value in XY plane at most 
        // backwards and most forward z coordinate respectively
        int[] startInd = { Size.x / 2, Size.y / 2, 0 }; // int division floors, but since it's zero indexed, this results in the center of the xy-plane
        int[] endInd = { Size.x / 2, Size.y / 2, (Size.z - 1) }; // int division floors, but since it's zero indexed, this results in the center of the xy-plane
        MazeNode startNode = mazeBase.Find(x => x.Identifier == ("." + startInd[0] + "." + startInd[1] + "." + startInd[2]));
        MazeNode endNode = mazeBase.Find(x => x.Identifier == ("." + endInd[0] + "." + endInd[1] + "." + endInd[2]));
        startNode.RenameNode("start");
        endNode.RenameNode("end");

        // Sort for later easy reference
        mazeBase.Sort();
    }

    /// <summary>
    /// Get quadrants.
    /// </summary>
    /// <returns>The quadrants.</returns>
    /// <param name="mazeBase">Maze base.</param>
    protected override List<List<MazeNode>> GetQuadrants(List<MazeNode> mazeBase)
    {
        throw new System.NotImplementedException();
    }

}

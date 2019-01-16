# Procedural-Maze-Game

This is a work-in-progress procedural maze game developed in the Unity3D engine. The game is not finished, I put it on GitHub as a concrete example of C#/OOP.

Amongst other classes/functionality, it contains:
####  A set of classes and methods that allow easy manipulation of maze objects to form levels of arbitrary numbers of sub-mazes
  * _Assets/Scripts/MazeCreation/MazeFrame.cs_</br> Main maze class containing a set of interconnected _MazeNode_ objects, and a large set of methods to safely manipulate the MazeFrame object, such as concatenating mazes, merging mazes, rotating/translating in the game world, recursively manipulating its MazeNode objects, etc.
  * _Assets/Scripts/MazeCreation/MazeNode.cs_</br> Class describing individual maze nodes, i.e. a point in space with unconnected and connected neighbors. It contains methods to manipulate individual nodes and their connections. 
  
#### Tools for generating mazes inside an arbitrary set of connected nodes (a NxMxL square structure, vertices from an arbitrary mesh, etc) using inheritance and polymorphism for easy interfacing and reliable method signatures
  * _Assets/Scripts/MazeCreation/MazeFrameCreator.cs_</br> Abstract parent class containing maze creation algorithms (Hunt and Kill, Backtracking), to generate a maze (a set of connected _MazeNode_ objects) within a _MazeFrame_ created by child classes.
  * _Assets/Scripts/MazeCreation/MazeFrameCreatorSquare3D.cs_</br>Child class inheriting from _MazeFrameCreator.cs_ that creates the MazeFrame to generate a maze in. Square3D creates an XxYxZ set of nodes, as a cube (named w.r.t. to planned Square2.5D, a set of XZ planes sparsely connected within Y)
  * _Assets/Scripts/MazeCreation/MazeFrameCreatorMesh.cs_</br>  Abstract child class inheriting from _MazeFrameCreator.cs_ containing methods specific to the generation of mazes from mesh vertices.
  * _Assets/Scripts/MazeCreation/MazeFrameCreatorMeshIcosahedron.cs_</br> Child class inheriting from _MazeFrameCreatorMesh_ that creates the MazeFrame to generate a maze in, creating maze nodes and their connections using the vertices from a specific set icosahedron meshes. 

#### Game effects intended to make navigating the game world more challenging, relying on class inheritance for easy application inside the game world
  * _Assets/Scripts/MindWarp/MindWarp.cs_</br> Abstract parent class creating individual effects during the game at maze junctions, intended to make the game more challenging. Contains set of abstract method signatures ensuring easy application of effect inside the game controller classes.
  * _Assets/Scripts/MindWarp/XXX.cs_</br> Child class inheriting from _MindWarp_ that applies specific game affecting effects (such as generating multiple cameras with semi-random orientations)
  
#### Tools for creating procedural meshes warped around Bezier curves (using rotation minimizing frames for minimal mesh rotation in XYZ axes)
  * _Assets/Scripts/SplineMesh/NDegreeBezierCurve.cs_</br> Class containing N-degree Bezier curve representation and methods to obtain derivatives/normals/positions at a position along the curve.
  * _Assets/Scripts/SplineMesh/Spline.cs_</br> Class forming a spline, which is a set of connected Bezier segments (_NDegreeBezierCurve_ objects), and methods to obtain derivatives/normals/positions/etc at a position along the spline, connected seperate Bezier segments, etc.
  * _Assets/Scripts/SplineMesh/SplineFittedMesh.cs_</br> Class containing both the mesh to warp around the spline and the to-be-warped-mesh.


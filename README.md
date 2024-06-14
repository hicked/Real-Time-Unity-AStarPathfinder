# Unity-AStarPathfinder
A basic example of A* pathfinding for Unity games (C#)

See `NPC.cs` for example of usage, or see below for explanation of `AStarPathfinder.cs` code.

## What is it?
This is a 2D A* pathfinder which enables game objects (ex: NPCs) to find (usually) the shortest path between a `star` and `end` point. However, a 3D iteration would be simple enough to implement.

This script uses `Physics.CapsuleCast()` to enable near realtime processing of the environment for pathfinding.
>*I say near realtime since if the environment changes after or during the `FindPathCoroutine` coroutine, there will be issues.*

As mentionned, this utilizes Unity's *Coroutine* system as well as `yield` statements since the pathfinding can be quite laborious, and require **thousands** of iterations over a while loop. This way, the pathfinding can be done over **multiple frames**. 
Multithreading can be implemented for additional performance. 

>*See more about `Coroutines` and `yield` statements: 
https://docs.unity3d.com/Manual/Coroutines.html*


## How It Works:
The play area is set up as a grid with tiles. Each tile, or `Location` object will have these fields: **x, y, z, g, h, f**. These will be discussed shortly. The pathfinder will look around at its neighboring tiles, and will assume the next best tile will be the one with the lowest **F** cost. It will continue to do so until it has found the end location, or fails to do so.
*   **X:** The x coordinate of the current tile.
*   **Y:** The y coordinate of the current tile.
*   **Z:** The z coordinate of the current tile.
*   **G:** The movement cost to get to the current tile from the start tile. This takes into account previous obstacles. In this example, it is simply the distance covered, but in other cases it might be higher if navigating through *water* or *mud* as an example.
*   **H:** The estimated cost from the current tile to the end tile. This is estimated as the pathfinder does not know what obstacle are in the future. Thus, it is simply the `ΔX + ΔY` of the two tiles or the "Manhattan distance" between them.
*   **F:** The final score of the tile. This is a combination of the G value and the H value.

> *Here is a great article that does a fantastic job of explaining it: <br>
https://www.kodeco.com/3016-introduction-to-a-pathfinding*


## Performance Issues:
### Pathfinder is Ignoring Colliders:
*   Ensure that your walls/obstacles have:
    *   Colliders (mesh or otherwise)
    *   RigidBodies
    *   Are set to a layer (`walls`/`obstacles` etc)
*   Ensure to include that layer in the A* inspector

### Suffering Performance Issues -ie Pathfinding Takes too Long:
*   Decrease the amount of directions the pathfinder checks
*   Increase the `tileSize`
*   Change `Physiscs` cast to something less complex (Raycast)
> *NOTE: All these will reduce accuracy of the pathfinder.*

If you are still experiencing issue, another search algorithm (*D\* lite*), or multithreading might be worth looking into.

### Pathfinder can't Find a Path:
*   Decrease `tileSize` *(some small openings may not be found if tiles are too big)*
*   Ensure the end point is truly attainable
*   Increase `maxTilesAllowedToCheck`
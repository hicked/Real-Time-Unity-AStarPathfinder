using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;


public class AStarPathfinder : MonoBehaviour {
    // Size of each tile, in this case, 1x1 meters
    [SerializeField] public float tileSize = 1f; 
    
    // Unity Layers for which the pathfinder will be blocked. This should be changed from the Unity Inspector
    [SerializeField] private LayerMask collisionLayers; 
    
    // Raises error if certain amount of tiles has been checked
    // This can happen if the point is too far away. Adjust as needed
    [SerializeField] private int maxTilesAllowedToCheck = 5000; 
    
    // Variabled used for the collision detector (Capsule Cast)
    private float NPCHeight;
    private float NPCRadius;
    
    // Simple variable that keeps track of whether or not the gameobject is pathfinding
    public bool isPathfinding;

    // Contains tiles that have not been checked yet, potential candidates that could lead to the end location 
    private List<Location> openList = new List<Location>();

    // Contains tiles that have already been checked and do not lead to the end location
    private HashSet<Location> closedList = new HashSet<Location>();

    // Dict that points from currentLocation to previousLocation. Used to trace back steps once the final location is reached
    private Dictionary<Location, Location> cameFrom = new Dictionary<Location, Location>();

    // List of locations containing valid neighbors of currentLocation. These will be added to the openList
    private List<Location> validNeighbors = new List<Location>();
    
    // Initializes path to the end point as null
    private List<Location> path = null;

    Location currentLocation;

    // Directions the script will work. Notice the vectors are 2D (no y components). This can be added if you wish
    Vector3[] directions = {
        new Vector3(1f, 0f, 0f),
        new Vector3(-1f, 0f, 0f),
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, 0f, -1f),
        new Vector3(1f, 0f, 1f),
        new Vector3(-1f, 0f, 1f),
        new Vector3(-1f, 0f, -1f),
        new Vector3(1f, 0f, -1f)
        };

    private void Start() {
        // Here you could GetComponent of the gameobject collider and pull the radius + height to store it here instead of hardcoding it
    }
    
    private void Update() {
        // Will show the path in yellow if there is one
        if (path != null) {
            for (int i = 1; i < path.Count; i++) {
                Debug.DrawLine(path[i].vector + new Vector3(0, 0.5f, 0), path[i-1].vector + new Vector3(0, 0.5f, 0), Color.yellow);
            }
        }
    }


    // Note this function is a IEnumerator and therefore can be started as a coroutine.
    // It may be wise to change to full multithreading, depending on the scope of the project

    // For better understanding of scoring, check Location.cs
    public IEnumerator FindPathCoroutine(Vector3 start, Vector3 end) {
        
        // Initializations
        isPathfinding = true;

        path = null;
        openList.Clear();
        closedList.Clear();
        cameFrom.Clear();

        Location startLocation = new Location(start, 0, Heuristic(start, end));
        Location endLocation = new Location(end, 0, 0);  // End location's heuristic is not needed here, since we know it to be 0

        openList.Add(startLocation);

        while (openList.Count > 0) { // if weve checked EVERY possible tile, and still havent found the end, break out of the while
            // Checks if too many tiles have been checked, and simply breaks
            if (openList.Count > maxTilesAllowedToCheck) { 
                Debug.Log($"Checked too many tiles, assuming no path possible to get to location {end} from {start} with NPC {NPC}");
                isPathfinding = false;
                yield break;
            }

            // Sets random (first) initial location, before checking all open/candidate tiles and finding the one with the best score
            Location currentLocation = openList[0]; 
            for (int i = 1; i < openList.Count; i++) {
                if (openList[i].f < currentLocation.f) {
                    currentLocation = openList[i]; // Makes the best location the current location
                }
            }
            // Removes the location since it has been checked
            openList.Remove(currentLocation); 
            closedList.Add(currentLocation);


            // If its close enough to the end location and there isnt a wall in the way, simply make the final location = end location
            if (Vector3.Distance(currentLocation.vector, end) <= tileSize && 
                !IsBlocked(currentLocation.vector, currentLocation.vector - end, (currentLocation.vector - end).magnitude, collisionLayers)) {

                cameFrom[endLocation] = currentLocation; // Ensure cameFrom is updated
                currentLocation = endLocation;
                path = ReconstructPath(cameFrom, currentLocation); // path is set to the field
                isPathfinding = false;

                yield break;
            }

            // Checks locations nearby for other tile candidates that will be added to the open list
            foreach (Location neighbor in GetValidNeighbors(currentLocation, end, currentLocation.g)) {
                // If the neighbor has already been checked and isn't valid, skip
                if (closedList.Contains(neighbor)) {
                    continue;
                }

                // Sets the mouvement cost to the previous movement cost + the distacne from that point
                // Note that movement weight logic will have to be here, I assumed all movement was of equal weight (based only on distance)
                float tentativeG = currentLocation.g + Vector3.Distance(new Vector3(currentLocation.x, currentLocation.y, currentLocation.z), new Vector3(neighbor.x, neighbor.y, neighbor.z));

                // If it isnt already in the candidate list, add it
                if (!openList.Contains(neighbor)) {
                    openList.Add(neighbor);
                }
                // If it is already in the list BUT the movement cost (g) that was calculated was less then what was originally stored, update it
                else if (tentativeG >= neighbor.g) {
                    continue;
                }

                cameFrom[neighbor] = currentLocation;
                neighbor.g = tentativeG;
                neighbor.h = Heuristic(new Vector3(neighbor.x, neighbor.y, neighbor.z), end);
                neighbor.f = neighbor.g + neighbor.h;
            }

            // checks best location and adds neighbors to open list ONCE every frame to improve performance
            yield return null;
            // NOTE: Can't use yield return new WaitUntilEndOfFrame because the update functions in this class isn't being used

        }
        // If it breaks from the while loop aka, out of open tiles to check
        Debug.Log($"No path could be found from {start} to {end}");
        isPathfinding = false;
        yield break;
    }

    public List<Location> GetPath() {
        return path;
    }
    public void SetPath(List<Location> paramPath) {
        this.path = paramPath;
    }
  
    List<Location> GetValidNeighbors(Location currentLocation, Vector3 end, float currentG) {
        validNeighbors.Clear();
        for(int i = 0; i < directions.Count(); i++) {      
            if (!IsBlocked(new Vector3(currentLocation.x, currentLocation.y, currentLocation.z), directions[i], tileSize, collisionLayers)) {
                Vector3 neighborPos = currentLocation.vector + directions[i].normalized * tileSize;
                float tentativeG = currentG + Vector3.Distance(currentLocation.vector, neighborPos);
                validNeighbors.Add(new Location(neighborPos, tentativeG, Heuristic(neighborPos, end)));
            }
        }
        return validNeighbors;
    }

    // Casts a a capsule in X direction to check if there is something in the way
    private bool PerformCapsuleCast(Vector3 location, Vector3 dir, float distance, int layerMask, out RaycastHit hitInfo) {
        return Physics.CapsuleCast(
            new Vector3(location.x, location.y + NPCHeight / 2f, location.z),
            new Vector3(location.x, location.y - NPCHeight / 2f, location.z),
            NPCRadius * 1.2f,
            dir,
            out hitInfo,
            distance,
            layerMask
        );
    }

    private bool IsBlocked(Vector3 location, Vector3 dir, float distance, int layer) {
        RaycastHit hitInfo;

        // Perform capsule cast
        bool hit = PerformCapsuleCast(location, dir, distance, collisionLayers, out hitInfo);

        // Logic can be implemented like this if we only want to be blocked by locked doors:
        // if (hit && LayerMask.LayerToName(hitInfo.collider.gameObject.layer) == "Doors") {
        //     Doors door = hitInfo.collider.gameObject.GetComponent<Doors>();
        //     // Only return true if the door.isLocked == true
        //     if (!door.isLocked) {
        //         int layersWithoutDoors = collisionLayers;
        //         layersWithoutDoors &= ~(1 << LayerMask.NameToLayer("Doors"));
        //         return IsBlocked(location, dir, distance, layersWithoutDoors);
        //     }
        // }

        return hit; // Return the result of the capsule cast
    }

    private List<Location> ReconstructPath(Dictionary<Location, Location> cameFrom, Location current) {
        List<Location> totalPath = new List<Location> { current };
        while (cameFrom.ContainsKey(current)) {
            current = cameFrom[current];
            totalPath.Add(current);
        }
        totalPath.Reverse();
        return totalPath;
    }

    private float Heuristic(Vector3 current, Vector3 end) {
        return (Mathf.Abs(current.x - end.x) + Mathf.Abs(current.z - end.z)) / tileSize;
    }
}
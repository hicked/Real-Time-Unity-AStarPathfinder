public class NPC : MonoBehaviour {
    private AStarPathfinder pathfinder;
    [SerializeField] private float NPCWalkSpeedMultiplier = 1.5f;
    [SerializeField] private float NPCRunningSpeedMultiplier = 2.5f;
    [SerializeField] private float NPCTurnSpeed = 6f;

    public bool isIdle = true;
    public bool isWalking = false;
    public bool isRunning = false;
    private GameObject lastDoorBlocked;

    [SerializeField] private int pointsBeforeOpenDoor = 3; // how many tiles away will the door open on an NPCs path
    [SerializeField] private float timeSpentOpenByNPC = 1.5f;
    [SerializeField] private LayerMask doorLayer;

    private int currentPathIndex;

    private void Start() {
        setPathTo(new Vector3(0f, 0f, 0f));
    }

    private void Update() {
        MoveAlongPath();
    }

    private void setPathTo(Vector3 location) {
        StartCoroutine(pathfinder.FindPathCoroutine(transform.position, location));
        currentPathIndex = 0;
    }

    private void MoveAlongPath(bool run = false) { // default param of walking not running
        if (path == null || path.Count == 0 || currentPathIndex > path.Count - 1) {
            return;
        }

        if (!run) {
            isWalking = true;
            isIdle = false;
            isRunning = false;
        }
        else if (run) {
            isWalking = false;
            isIdle = false;
            isRunning = true;
        }

        Vector3 targetPosition = new Vector3(path[currentPathIndex].x, transform.position.y, path[currentPathIndex].z); // next point along path to reach

        Vector3 direction = targetPosition - transform.position; // directional vector towards the point
        float distanceToTarget = direction.magnitude;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smoothly rotate towards the target position
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * NPCTurnSpeed);


        // Move the NPC
        transform.position += direction.normalized * Time.deltaTime * (isRunning ? NPCRunningSpeedMultiplier : NPCWalkSpeedMultiplier);

        // Check if the NPC is close enough to the target position
        if (distanceToTarget < 0.1f) {
            if (currentPathIndex == path.Count - 1) {
                // Smooth rotation of npc once reached the end location
                targetRotation = Quaternion.LookRotation(lookatVector - transform.position);
                StartCoroutine(RotateToFinalDirectionCoroutine(targetRotation));

                pathfinder.SetPath(null);
                isWalking = false;
                isIdle = true;
                isRunning = false;
                return;
            }
            currentPathIndex++;
        }

        // Door mechanics, not necessary
        for (int i = pointsBeforeOpenDoor; i >= 0; i--) {
            // Tries to open a door sum number of points on the path away, if it fails, reduces the number of point away by 1 and tried again
            // Does this until it reaches 0 which will be between the next point, and the previous point
            // Might be smart to change "pointsBeforeOpenDoor" actively with tile size, but for now it can be manual
            try {
                if (isBlockedByDoor(path[currentPathIndex + i - 1].vector + new Vector3(0, 1, 0), path[currentPathIndex + i].vector + new Vector3(0, 1, 0))) {
                    Doors door = lastDoorBlocked.GetComponent<Doors>();
                    door.OpenDoorTemporarily(timeSpentOpenByNPC);
                    break;
                }
            }
            catch {
                continue;
            }
        }
    }

    public bool isBlockedByDoor(Vector3 start, Vector3 end) {
        RaycastHit hitInfo = default(RaycastHit);
        bool hit = Physics.Linecast(start, end, out hitInfo, doorLayer);
        if (hit) {
            lastDoorBlocked = hitInfo.collider.gameObject;
        }
        return hit;
    }

    private IEnumerator RotateToFinalDirectionCoroutine(Quaternion finalTargetRotation) {
        while (true) {
            if (Quaternion.Angle(transform.rotation, finalTargetRotation) < 0.1f) {
                yield break;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, finalTargetRotation, Time.deltaTime * NPCTurnSpeed);
            yield return null; // Wait for the end of the frame
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class EyebatTrapAI : MonoBehaviour
{
    public enum TrapState
    {
        Patrol,
        Chase,
        Return,
        Dead
    }

    [Header("Scene References")]
    public Transform waypointParent;   // Drag Watcher1 here

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 6f;
    public float waypointReachDistance = 0.15f;
    public float chaseRefreshTime = 0.4f;
    public float hoverAmplitude = 0.15f;
    public float hoverFrequency = 2f;

    [Header("Detection")]
    public float detectionRadius = 8f;
    public float losePlayerRadius = 12f;

    [Header("Damage")]
    public float contactDamage = 10f;
    public float damageCooldown = 1f;

    [Header("Health")]
    public int maxHealth = 1;

    private int currentHealth;
    private TrapState currentState = TrapState.Patrol;

    private Transform player;
    private PlayerHealth playerHealth;

    private List<EyebatPoint> points = new List<EyebatPoint>();
    private List<int> currentPath = new List<int>();

    private int patrolIndex = 0;
    private int currentPathIndex = 0;
    private int homeIndex = 0;
    private int currentNodeIndex = 0;
    private int playerClosestNodeIndex = 0;

    private float nextPathRefreshTime = 0f;
    private float nextDamageTime = 0f;
    private float baseY;

    void Start()
    {
        currentHealth = maxHealth;
        FindPlayerAutomatically();
        LoadPoints();

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        baseY = transform.position.y;

        if (points.Count == 0)
        {
            Debug.LogError("EyebatTrapAI: No EyebatPoint children found under waypointParent.", this);
            enabled = false;
            return;
        }

        homeIndex = 0;
        patrolIndex = 0;
        currentNodeIndex = GetClosestPointIndex(transform.position);
        if (currentNodeIndex < 0) currentNodeIndex = 0;

        SnapToNode(currentNodeIndex);
    }

    void Update()
    {
        if (currentState == TrapState.Dead) return;

        if (player == null)
        {
            FindPlayerAutomatically();
            if (player == null) return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case TrapState.Patrol:
                HandlePatrol();
                if (distToPlayer <= detectionRadius)
                {
                    StartChase();
                }
                break;

            case TrapState.Chase:
                HandleChase();
                if (distToPlayer > losePlayerRadius)
                {
                    StartReturn();
                }
                break;

            case TrapState.Return:
                HandleReturn();
                if (distToPlayer <= detectionRadius)
                {
                    StartChase();
                }
                break;
        }

        ApplyHover();
    }

    private void FindPlayerAutomatically()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    private void LoadPoints()
    {
        points.Clear();

        if (waypointParent == null)
        {
            Debug.LogError("EyebatTrapAI: waypointParent is not assigned.", this);
            return;
        }

        EyebatPoint[] foundPoints = waypointParent.GetComponentsInChildren<EyebatPoint>();
        points.AddRange(foundPoints);

        points.Sort((a, b) => a.name.CompareTo(b.name));
    }

    private void HandlePatrol()
    {
        if (points.Count == 0) return;

        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            int nextPatrolIndex = patrolIndex;
            BuildPathTo(nextPatrolIndex);
        }

        FollowCurrentPath();

        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            currentNodeIndex = patrolIndex;
            patrolIndex = (patrolIndex + 1) % points.Count;
        }
    }

    private void StartChase()
    {
        currentState = TrapState.Chase;
        RefreshPathToPlayerClosestWaypoint();
        nextPathRefreshTime = Time.time + chaseRefreshTime;
    }

    private void HandleChase()
    {
        if (Time.time >= nextPathRefreshTime)
        {
            RefreshPathToPlayerClosestWaypoint();
            nextPathRefreshTime = Time.time + chaseRefreshTime;
        }

        FollowCurrentPath();
    }

    private void StartReturn()
    {
        currentState = TrapState.Return;
        BuildPathTo(homeIndex);
    }

    private void HandleReturn()
    {
        FollowCurrentPath();

        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            currentNodeIndex = homeIndex;
            patrolIndex = homeIndex;
            currentState = TrapState.Patrol;
        }
    }

    private void RefreshPathToPlayerClosestWaypoint()
    {
        if (player == null || points.Count == 0) return;

        playerClosestNodeIndex = GetClosestPointIndex(player.position);
        if (playerClosestNodeIndex < 0) return;

        currentNodeIndex = GetClosestPointIndex(transform.position);
        if (currentNodeIndex < 0) return;

        currentPath = FindShortestPathDijkstra(currentNodeIndex, playerClosestNodeIndex);
        currentPathIndex = 0;
    }

    private void BuildPathTo(int goalIndex)
    {
        currentNodeIndex = GetClosestPointIndex(transform.position);
        if (currentNodeIndex < 0 || goalIndex < 0 || goalIndex >= points.Count) return;

        currentPath = FindShortestPathDijkstra(currentNodeIndex, goalIndex);
        currentPathIndex = 0;
    }

    private void FollowCurrentPath()
    {
        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count) return;

        int pointIndex = currentPath[currentPathIndex];
        Vector3 target = points[pointIndex].transform.position;
        MoveTowardsWaypoint(target);

        if (Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(target.x, 0f, target.z)) <= waypointReachDistance)
        {
            currentNodeIndex = pointIndex;
            currentPathIndex++;
        }
    }

    private void MoveTowardsWaypoint(Vector3 targetPos)
    {
        Vector3 currentFlat = new Vector3(transform.position.x, baseY, transform.position.z);
        Vector3 targetFlat = new Vector3(targetPos.x, baseY, targetPos.z);
        Vector3 direction = targetFlat - currentFlat;

        if (direction.magnitude > 0.001f)
        {
            Vector3 moveDir = direction.normalized;
            currentFlat += moveDir * moveSpeed * Time.deltaTime;

            transform.position = new Vector3(currentFlat.x, transform.position.y, currentFlat.z);

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void SnapToNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= points.Count) return;

        Vector3 p = points[nodeIndex].transform.position;
        transform.position = new Vector3(p.x, baseY, p.z);
    }

    private void ApplyHover()
    {
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        transform.position = pos;
    }

    private int GetClosestPointIndex(Vector3 pos)
    {
        if (points.Count == 0) return -1;

        int closestIndex = 0;
        float closestDist = Mathf.Infinity;

        for (int i = 0; i < points.Count; i++)
        {
            float dist = Vector3.Distance(pos, points[i].transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private List<int> FindShortestPathDijkstra(int startIndex, int goalIndex)
    {
        List<int> path = new List<int>();
        if (startIndex == goalIndex) return path;

        Dictionary<int, float> distance = new Dictionary<int, float>();
        Dictionary<int, int> previous = new Dictionary<int, int>();
        List<int> unvisited = new List<int>();

        for (int i = 0; i < points.Count; i++)
        {
            distance[i] = Mathf.Infinity;
            previous[i] = -1;
            unvisited.Add(i);
        }

        distance[startIndex] = 0f;

        while (unvisited.Count > 0)
        {
            int current = -1;
            float bestDistance = Mathf.Infinity;

            foreach (int i in unvisited)
            {
                if (distance[i] < bestDistance)
                {
                    bestDistance = distance[i];
                    current = i;
                }
            }

            if (current == -1) break;
            unvisited.Remove(current);

            if (current == goalIndex) break;

            foreach (int neighbor in points[current].neighborIndices)
            {
                if (neighbor < 0 || neighbor >= points.Count) continue;
                if (!unvisited.Contains(neighbor)) continue;

                float edgeCost = Vector3.Distance(points[current].transform.position, points[neighbor].transform.position);
                float alt = distance[current] + edgeCost;

                if (alt < distance[neighbor])
                {
                    distance[neighbor] = alt;
                    previous[neighbor] = current;
                }
            }
        }

        if (previous[goalIndex] == -1) return path;

        int node = goalIndex;
        while (node != -1)
        {
            path.Add(node);
            node = previous[node];
        }

        path.Reverse();

        if (path.Count > 0 && path[0] == startIndex)
        {
            path.RemoveAt(0);
        }

        return path;
    }

    public void TakeDamage(int damage = 1)
    {
        if (currentState == TrapState.Dead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        currentState = TrapState.Dead;
        currentPath.Clear();

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        gameObject.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentState == TrapState.Dead) return;
        if (!other.CompareTag("Player")) return;
        if (Time.time < nextDamageTime) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(contactDamage);
            nextDamageTime = Time.time + damageCooldown;
        }
    }
}
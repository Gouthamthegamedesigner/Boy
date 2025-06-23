using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 3f;
    private int _currentWaypointIndex = 0;

    [Header("Chase")]
    public float chaseSpeed = 6f;
    public float detectionAngle = 60f;
    public float detectionDistance = 10f;
    public LayerMask playerLayer;
    public LayerMask obstructionLayers;

    private NavMeshAgent _agent;
    private Transform _player;
    private bool _hasSeenPlayer = false; // Renamed to better reflect permanent chase

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        if (_player == null) Debug.LogError("Assign 'Player' tag to player object!");
        _agent.SetDestination(waypoints[_currentWaypointIndex].position);
    }

    void Update()
    {
        if (_player == null) return;

        // Only check for player if we haven't seen them yet
        if (!_hasSeenPlayer && CanSeePlayer())
        {
            _hasSeenPlayer = true;
        }

        if (_hasSeenPlayer)
        {
            _agent.speed = chaseSpeed;
            _agent.SetDestination(_player.position);
        }
        else if (_agent.remainingDistance < 0.5f)
        {
            CycleWaypoint();
        }
    }

    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (_player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        Debug.DrawRay(transform.position, directionToPlayer * detectionDistance, 
                     angleToPlayer < detectionAngle/2f ? Color.green : Color.red);

        if (angleToPlayer < detectionAngle/2f && 
            Vector3.Distance(transform.position, _player.position) <= detectionDistance)
        {
            if (!Physics.Linecast(transform.position, _player.position, obstructionLayers))
            {
                return true;
            }
        }
        return false;
    }

    void CycleWaypoint()
    {
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        _agent.SetDestination(waypoints[_currentWaypointIndex].position);
    }

    // Removed ResumePatrol() since we won't return to patrol

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 leftRay = Quaternion.Euler(0, -detectionAngle/2, 0) * transform.forward * detectionDistance;
        Vector3 rightRay = Quaternion.Euler(0, detectionAngle/2, 0) * transform.forward * detectionDistance;
        Gizmos.DrawRay(transform.position, leftRay);
        Gizmos.DrawRay(transform.position, rightRay);
        Gizmos.DrawLine(transform.position + leftRay, transform.position + rightRay);
    }
}
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 3f;
    private int _currentWaypointIndex = 0;

    [Header("Detection")]
    public float detectionAngle = 60f;
    public float detectionDistance = 10f;
    public LayerMask blockingLayers; // Assign layers that should block vision

    [Header("Chase")]
    public float chaseSpeed = 6f;

    [Header("Visuals")]
    public Material chaseMaterial;
    private Material _originalMaterial;
    private Renderer _enemyRenderer;

    private NavMeshAgent _agent;
    private Transform _player;
    private bool _hasSeenPlayer = false;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        if (_player == null) Debug.LogError("Player not found! Assign 'Player' tag.");
        
        _agent.SetDestination(waypoints[_currentWaypointIndex].position);
        
        _enemyRenderer = GetComponent<Renderer>();
        if (_enemyRenderer != null) _originalMaterial = _enemyRenderer.material;
    }

    void Update()
    {
        if (_player == null) return;

        if (!_hasSeenPlayer && CanSeePlayer())
        {
            _hasSeenPlayer = true;
            ChangeToChaseColor();
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
        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        // Visual debug (green = in cone, red = out of cone)
        Debug.DrawRay(transform.position, directionToPlayer * detectionDistance, 
                     angleToPlayer < detectionAngle/2f ? Color.green : Color.red);

        // Check if player is within vision cone and range
        if (angleToPlayer < detectionAngle/2f && distanceToPlayer <= detectionDistance)
        {
            // Calculate the actual distance to player
            float playerDistance = Vector3.Distance(transform.position, _player.position);
            
            // Check for blocking objects
            RaycastHit hit;
            if (Physics.Raycast(
                transform.position,
                directionToPlayer,
                out hit,
                playerDistance, // Use actual distance to player
                blockingLayers))
            {
                // If we hit something in the blocking layers, vision is blocked
                Debug.DrawLine(transform.position, hit.point, Color.blue, 0.1f);
                return false;
            }
            
            // Nothing blocking - can see player
            Debug.DrawLine(transform.position, _player.position, Color.yellow, 0.1f);
            return true;
        }
        return false;
    }

    void ChangeToChaseColor()
    {
        if (_enemyRenderer != null && chaseMaterial != null)
        {
            _enemyRenderer.material = chaseMaterial;
        }
    }

    void CycleWaypoint()
    {
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        _agent.SetDestination(waypoints[_currentWaypointIndex].position);
    }

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
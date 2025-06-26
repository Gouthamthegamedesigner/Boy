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

    [Header("Chase")]
    public float chaseSpeed = 6f;
    public float stopDelay = 2f; // Time to wait when player stops

    [Header("Visuals")]
    public Material chaseMaterial;
    private Material _originalMaterial;
    private Renderer _enemyRenderer;

    private NavMeshAgent _agent;
    private Transform _player;
    private bool _isPlayerMoving;
    private float _playerMovementThreshold = 0.1f; // Minimum movement to be considered "moving"
    private float _playerStopTimer;
    private Vector3 _lastPlayerPosition;
    
    // Enemy states
    private enum EnemyState { Patrolling, Chasing, Waiting }
    private EnemyState _currentState = EnemyState.Patrolling;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        if (_player == null) Debug.LogError("Player not found! Assign 'Player' tag.");
        
        _agent.SetDestination(waypoints[_currentWaypointIndex].position);
        _agent.speed = patrolSpeed;
        
        _enemyRenderer = GetComponent<Renderer>();
        if (_enemyRenderer != null) _originalMaterial = _enemyRenderer.material;
        
        _lastPlayerPosition = _player.position;
    }

    void Update()
    {
        if (_player == null) return;

        // Track player movement
        UpdatePlayerMovementStatus();

        // State machine
        switch (_currentState)
        {
            case EnemyState.Patrolling:
                HandlePatrolState();
                break;
                
            case EnemyState.Chasing:
                HandleChaseState();
                break;
                
            case EnemyState.Waiting:
                HandleWaitingState();
                break;
        }
    }

    void UpdatePlayerMovementStatus()
    {
        // Calculate distance player moved since last frame
        float distanceMoved = Vector3.Distance(_player.position, _lastPlayerPosition);
        _isPlayerMoving = distanceMoved > _playerMovementThreshold;
        _lastPlayerPosition = _player.position;
    }

    void HandlePatrolState()
    {
        // Check if player is visible and moving
        if (CanSeePlayer() && _isPlayerMoving)
        {
            // Start chasing
            _currentState = EnemyState.Chasing;
            ChangeToChaseColor();
            _agent.speed = chaseSpeed;
        }
        
        // Move to next waypoint if reached current
        if (_agent.remainingDistance < 0.5f)
        {
            CycleWaypoint();
        }
    }

    void HandleChaseState()
    {
        // Continue chasing while player is moving
        if (_isPlayerMoving)
        {
            _agent.SetDestination(_player.position);
        }
        else
        {
            // Player stopped - begin waiting period
            _currentState = EnemyState.Waiting;
            _agent.isStopped = true; // Stop moving
            _playerStopTimer = stopDelay;
        }
    }

    void HandleWaitingState()
    {
        // Look at player while waiting
        Vector3 lookDirection = _player.position - transform.position;
        lookDirection.y = 0; // Keep upright
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDirection),
                Time.deltaTime * 5f
            );
        }
        
        // Count down timer
        _playerStopTimer -= Time.deltaTime;
        
        // Check if player started moving again
        if (_isPlayerMoving)
        {
            // Resume chasing
            _currentState = EnemyState.Chasing;
            _agent.isStopped = false;
        }
        // If timer expires and player still not moving
        else if (_playerStopTimer <= 0)
        {
            // Return to patrol
            _currentState = EnemyState.Patrolling;
            _agent.isStopped = false;
            ChangeToOriginalColor();
            _agent.speed = patrolSpeed;
            ResumePatrol();
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

        return angleToPlayer < detectionAngle/2f && distanceToPlayer <= detectionDistance;
    }

    void ChangeToChaseColor()
    {
        if (_enemyRenderer != null && chaseMaterial != null)
        {
            _enemyRenderer.material = chaseMaterial;
        }
    }
    
    void ChangeToOriginalColor()
    {
        if (_enemyRenderer != null && _originalMaterial != null)
        {
            _enemyRenderer.material = _originalMaterial;
        }
    }

    void CycleWaypoint()
    {
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        _agent.SetDestination(waypoints[_currentWaypointIndex].position);
    }
    
    void ResumePatrol()
    {
        // Find closest waypoint to resume patrol
        float minDistance = float.MaxValue;
        int closestIndex = 0;
        
        for (int i = 0; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, waypoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        
        _currentWaypointIndex = closestIndex;
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
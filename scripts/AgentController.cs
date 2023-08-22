using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.Events;

public class AgentController : MonoBehaviour, IHear
{
    public NavMeshAgent navMeshAgent { get; private set; }
    public Animator animator { get; private set; }
    public Health health { get; private set; }

    [Header("Agent States")]
    public PatrolState patrolState { get; private set; }
    public AlertState alertState { get; private set; }
    public ChaseTargetState chaseState { get; private set; }
    public AttackState attackState { get; private set; }
    public InvestigationState investigationState { get; private set; }
    public ReturnToPostState returnToPostState { get; private set; }
    private State currentState;

    [Header("Soldier")]
    public Transform[] patrolWaypoints;
    public Vector3 initialPosition; // because soldier might need to return to previous position
    public Quaternion initialRotation;
    public bool isDead;

    [Header("Target")]
    public Transform currentTarget;
    public float distanceFromCurrentTarget; // distance from current target to agent
    public Vector3 lastKnownTargetPosition; // to investigate

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Start()
    {
        InitializeStates();
        ChangeState(patrolState);
        StartCoroutine(SearchForTarget());
    }

    private void InitializeStates()
    {
        patrolState = new PatrolState(this);
        alertState = new AlertState(this);
        chaseState = new ChaseTargetState(this);
        attackState = new AttackState(this);
        investigationState = new InvestigationState(this);
        returnToPostState = new ReturnToPostState(this);
    }

    private void Update() 
    {
        if (isDead)
        {
            navMeshAgent.enabled = false;
            animator.SetFloat("Speed", 0f);
            currentTarget = null;
            return;
        }

        animator.SetFloat("Speed", navMeshAgent.hasPath ? navMeshAgent.velocity.magnitude : 0f);

        currentState?.Tick();

        if (currentTarget != null) {
            distanceFromCurrentTarget = Vector3.Distance(currentTarget.transform.position, transform.position);
        }
    }

    public void ChangeState(State nextState)
    {
        currentState?.Exit();
        currentState = nextState;
        currentState?.Enter();
    }

    public void RotateTowardsTarget()
    {
        if (currentTarget == null) {
            return;
        }

        Vector3 direction = currentTarget.position - transform.position; // Calculate the direction from the agent's position to the target's position
        direction.y = 0; // Ignore the vertical component of the direction
        transform.LookAt(transform.position + direction); // Rotate the agent towards the target
    }

    private IEnumerator SearchForTarget()
    {
        WaitForSeconds waitTime = new WaitForSeconds(1f);
        
        while (!isDead)
        {
            yield return waitTime;
    
            // Find all objects around agent's position
            // Detection layer is a Layer Mash set to "Player", because we only want to detect the player (ignore other layers)
            Collider[] colliders = Physics.OverlapSphere(transform.position, 40f, config.detectionLayer);
    
            if (colliders.Length <= 0)
            {
                currentTarget = null;
                yield return waitTime;
            }
    
            foreach (Collider collider in colliders)
            {
                // If object has component PlayerController (script on player's game object), we detected the player
                if (collider.TryGetComponent(out PlayerController player))
                {
                    Vector3 directionToTarget = (player.transform.position - transform.position).normalized;
    
                    // Check FOV
                    if (Vector3.Angle(transform.forward, directionToTarget) < 100)
                    {
                        float distanceToTarget = Vector3.Distance(transform.position, player.transform.position);
                        Vector3 startPoint = new Vector3(transform.position.x, config.characterEyeLevel, transform.position.z);
    
                        // Check if there is a obstacle between agent and player
                        // Obstacle layer is layer with everything instead of Player
                        if (Physics.Raycast(startPoint, directionToTarget, distanceToTarget, config.obstacleLayer))
                        {
                            currentTarget = null;
                            break;
                        } 
    
                        currentTarget = player.transform;
                        lastKnownTargetPosition = currentTarget.position;
                        break;
                    } else {
                         currentTarget = null;
                         break;
                    }
                } else {
                    currentTarget = null;
                }
            }
        }
    }

    public void RespondToSound(MySound sound)
    {
        if (currentTarget == null)
        {
            investigationState.investigationPosition = sound.position;
            ChangeState(investigationState);
        }
    }

    public void OnDamage()
    {
        if (currentState != null && currentState.GetType() == typeof(AttackState)) {
            return;
        }

        if (currentTarget == null)
        {
            if (lastKnownTargetPosition != null) {
                investigationState.investigationPosition = lastKnownTargetPosition;
            } else {
                investigationState.investigationPosition = transform.position;
            }

            ChangeState(investigationState);
        }
        else
        {
            ChangeState(chaseState);
        }
    }

    public void OnDeath()
    {
        if (isDead) return;
        ChangeState(deathState);
    }

}

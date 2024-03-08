using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Collections;

public class EnemyStateAI : MonoBehaviour
{
    //States
    public enum EnemyStates
    {
        patrol,
        chase,
        search,
        attack,
        retreat
    }
    
    
    // Tracking each state
    
    private EnemyStates currentState;
    public NavMeshAgent agent;

    // Enemy patrol points and tracking player movement

    [Header("Player Reference")]
    public Transform player;
    private Transform target;

    [Header("Enemy Patrol Points")]
    public Transform[] patrolLocations;
    private int currentPatrolPoint;
    
    private Vector3 lastSeenLocation = Vector3.zero;
    
    private bool enemySearching;

    // Enemy Values

    [Header("Distance Values")]
    
    [SerializeField] private float attackDistance = 4f;
    [SerializeField] private float chaseDistance = 12f;
    private float distanceToPoint;
    
    
    [Header("Change search time")]
    
    [SerializeField] private float enemySearchTime = 10f;

    // Delay between patrol points
    [Header("Patrol wait time")]
    [SerializeField] private float patrolDelay = 2f;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    // Change enemies color based on state   

    Renderer enemyColor;
  
    // HUD Text

    [Header("HUD Text")]

    public TextMeshProUGUI stateText;

    [Header("Retreat time")]
    private float retreatTimer;
    [SerializeField] private float retreatDuration = 5f; // Adjust the retreat duration as needed

    void Start()
    {
        
        currentState = EnemyStates.patrol;
        currentPatrolPoint = 0;
        target = patrolLocations[currentPatrolPoint];
        
        
        Vector3 distance = gameObject.transform.position - target.transform.position;
        enemyColor = GetComponent<Renderer>();
        
        
    }
    
    void Update()
    {
        ChangeState();
        switch (currentState)
        {
            case EnemyStates.patrol:
                PatrolState();
                break;
            case EnemyStates.chase:
                ChasePlayer();
                break;
            case EnemyStates.attack:
                AttackPlayer();
                break;
            case EnemyStates.search:
                SearchArea();
                break;
            case EnemyStates.retreat:
                Retreat();
                break;

        }


       
        // This changes the HUD text to show what state the enemy is in
        stateText.text = "Enemy State: " + currentState.ToString();
        
    }
    public void ChangeState()
    {
        // This will change the enemy state based on the players location
        
        if (Vector3.Distance(transform.position, player.position) <= chaseDistance)
        {
            currentState = EnemyStates.chase;
            if (Vector3.Distance(transform.position, player.position) > chaseDistance)
            {
                currentState = EnemyStates.search;
            }
        }
        
        // if player is close enough, enemy attacks

        if (Vector3.Distance(transform.position, player.position) <= attackDistance)
        {
            currentState = EnemyStates.attack;
        }
    }

    // patrol method
    public void PatrolState()
    {
       
        if (!isWaiting)
        {
            enemyColor.material.color = Color.cyan;
            
            agent.SetDestination(target.position);
            distanceToPoint = Vector3.Distance(transform.position, target.position);

            // If the enemy reaches the patrol point
            if (distanceToPoint <= 3f)
            {
                // Start waiting
                isWaiting = true;
                waitTimer = patrolDelay;
                Debug.Log("Started waiting");
            }
        }
        else
        {
            // If waiting, decrease the timer
            Debug.Log("Countdown started");
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                // Move to the next patrol point
                Debug.Log("Next patrol point");
                currentPatrolPoint++;
                if (currentPatrolPoint == patrolLocations.Length)
                {
                    currentPatrolPoint = 0;
                }
                target = patrolLocations[currentPatrolPoint];

                // Stop waiting and resume patrolling
                isWaiting = false;
                agent.SetDestination(target.position);
            }
        }
    }
    


    public void ChasePlayer()
    {
        enemyColor.material.color = Color.red;
        
        agent.SetDestination(player.position);
        if (Vector3.Distance(transform.position, player.position) > chaseDistance)
        {
            currentState = EnemyStates.search;
        }
    }
    
    public void AttackPlayer()
    {
        enemyColor.material.color = Color.black;
        agent.SetDestination(transform.position);
        if (Vector3.Distance(transform.position, player.position) > attackDistance)
        {
            currentState = EnemyStates.chase;
        }
    }

    public void SearchArea()
    {
        enemyColor.material.color = Color.green;

        if (!enemySearching)
        {
            lastSeenLocation = player.position;
            enemySearching = true;
        }

        float DistToPlayer = Vector3.Distance(transform.position, lastSeenLocation);
        if (DistToPlayer > 0.1f)
        {
            agent.SetDestination(lastSeenLocation);
        }

        // Reset search time when transitioning from another state to search state
        if (currentState != EnemyStates.search)
        {
            enemySearchTime = 10f; // Reset the search time
        }

        enemySearchTime -= Time.deltaTime;

        if (enemySearchTime <= 0)
        {
            // Retreat before patrolling again
            currentState = EnemyStates.retreat;
            target = patrolLocations[currentPatrolPoint]; // Retreat to the patrol point
            retreatTimer = retreatDuration; // Start retreat timer
            enemySearching = false; // Reset searching flag
            enemySearchTime = 10f; // Reset search time
        }

        // Handle retreat logic
        if (currentState == EnemyStates.retreat)
        {
            Retreat();
            retreatTimer -= Time.deltaTime;
            if (retreatTimer <= 0)
            {
                currentState = EnemyStates.patrol; // Change state to patrol after retreat
                currentPatrolPoint++; // Move to the next patrol point
                if (currentPatrolPoint >= patrolLocations.Length)
                {
                    currentPatrolPoint = 0; // Loop back to the first patrol point if reached the end
                }
                target = patrolLocations[currentPatrolPoint]; // Set target to the next patrol point
            }
        }
    }

    public void Retreat()
    {
        
        enemyColor.material.color = Color.gray;
        agent.SetDestination(target.position);
        distanceToPoint = Vector3.Distance(transform.position, target.position);
        if (distanceToPoint <= 3f)
        {
            agent.SetDestination(transform.position);
            currentState = EnemyStates.patrol; // Change state to patrol once arrived at patrol point
        }
    }
}

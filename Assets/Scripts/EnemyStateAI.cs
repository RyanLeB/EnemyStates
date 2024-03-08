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
            
            float closestDistance = float.MaxValue;
            int closestPatrolPointIndex = 0;

            for (int i = 0; i < patrolLocations.Length; i++)
            {
                float distanceToPatrolPoint = Vector3.Distance(transform.position, patrolLocations[i].position);
                if (distanceToPatrolPoint < closestDistance)
                {
                    closestDistance = distanceToPatrolPoint;
                    closestPatrolPointIndex = i;
                }
            }

            
            target = patrolLocations[closestPatrolPointIndex];

            // Change the state to patrol
            currentState = EnemyStates.patrol;

            enemySearching = false;
            enemySearchTime = 10f;
        }
    }

    public void Retreat()
    {
        
        enemyColor.material.color = Color.gray;
        agent.SetDestination(target.position);
        distanceToPoint = Vector3.Distance(transform.position, target.position);
        if (distanceToPoint <= 1f)
        {
            agent.SetDestination(transform.position);
        }
    }
}

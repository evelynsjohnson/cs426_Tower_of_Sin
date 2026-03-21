using UnityEngine;
using UnityEngine.AI;

public class SoldierEnemy : Enemy
{
    [Header("AI")]
    public float aggroRange = 10f;
    public float attackCooldown = 1.5f;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private Animator animator;
    private float nextAttackTime = 0f;

    new void Start()
    {
        base.Start();

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (IsDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (agent != null)
        {
            if (distanceToPlayer <= aggroRange)
                agent.SetDestination(player.position);
            else
                agent.ResetPath();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (IsDead) return;

        if (collision.gameObject.CompareTag("Player") && Time.time >= nextAttackTime)
        {
            playerHealth?.TakeDamage(atk);

            // Play attack animation if one is set up
            if (animator != null)
                animator.SetTrigger("Attack");

            nextAttackTime = Time.time + attackCooldown;
        }
    }
}

using UnityEngine;
using UnityEngine.AI;

public class SoldierEnemy : Enemy
{
    [Header("AI")]
    public float aggroRange = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public float turnSpeed = 10f;

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

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.stoppingDistance = attackRange;
        }
    }

    void Update()
    {
        if (IsDead || player == null || agent == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= aggroRange)
        {
            FacePlayer();

            if (distanceToPlayer > attackRange + 0.1f)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);

                if (animator != null)
                    animator.SetBool("IsMoving", true);
            }
            else
            {
                // Fully stop and hold position
                agent.ResetPath();
                agent.isStopped = true;
                agent.velocity = Vector3.zero;

                if (animator != null)
                    animator.SetBool("IsMoving", false);

                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown;

                    if (animator != null)
                        animator.SetTrigger("Attack");

                    playerHealth?.TakeDamage(atk);
                }
            }
        }
        else
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            if (animator != null)
                animator.SetBool("IsMoving", false);
        }
    }

    void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }
}
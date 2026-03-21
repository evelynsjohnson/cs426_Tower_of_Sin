using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drives the Animator on the enemy model.
/// Attach this to the same root GameObject as SoldierEnemy.
///
/// Animator parameters required:
///   - IsMoving  (Bool)    — true while the agent is walking
///   - IsDead    (Bool)    — true when HP reaches 0
///   - Attack    (Trigger) — fires on each melee hit
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyAnimator : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private Enemy enemy;

    // Cache parameter hashes for efficiency
    private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int HashIsDead   = Animator.StringToHash("IsDead");

    void Awake()
    {
        // Animator lives on the model child; Enemy and NavMeshAgent are on the root
        animator = GetComponentInChildren<Animator>();
        agent    = GetComponent<NavMeshAgent>();
        enemy    = GetComponent<Enemy>();
    }

    void OnEnable()
    {
        if (enemy != null)
            enemy.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        if (enemy != null)
            enemy.OnDeath -= HandleDeath;
    }

    void Update()
    {
        if (animator == null || enemy.IsDead) return;

        // IsMoving = agent is moving with meaningful velocity
        bool moving = agent != null && agent.velocity.sqrMagnitude > 0.01f;
        animator.SetBool(HashIsMoving, moving);
    }

    void HandleDeath()
    {
        if (animator == null) return;

        animator.SetBool(HashIsMoving, false);
        animator.SetBool(HashIsDead, true);
    }
}

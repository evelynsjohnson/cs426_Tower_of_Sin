using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class PirateAI : MonoBehaviour
{
    public float baseHP = 50f;
    public float baseATK = 10f;

    [HideInInspector] public float currentHP;
    [HideInInspector] public float currentATK;
    [HideInInspector] public float maxHP;

    public float walkSpeed = 2f;
    public float runSpeed = 4.5f;
    public float attackRange = 2.5f;
    public float runDistance = 8f;   // if farther than this, run
    public float walkDistance = 4f;   // if between attackRange and this/runDistance, walk
    public float rotationSpeed = 8f;
    public float stopDistanceBuffer = 0.15f;

    public float attackCooldown = 1.75f;
    public bool canDamagePlayer = true;

    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    public GameObject hpCanvas;
    public Image healthFillImage;
    public TMP_Text healthText;

    private float lastAttackTime = -999f;
    private bool isDead = false;
    private bool isAttacking = false;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        ApplyFloorScaling();

        currentHP = maxHP;
        UpdateHPUI();

        if (hpCanvas != null)
            hpCanvas.SetActive(true);

        if (agent != null)
        {
            agent.stoppingDistance = attackRange - stopDistanceBuffer;
            agent.speed = walkSpeed;
            agent.updateRotation = false;
        }
    }

    void Update()
    {
        if (isDead || player == null || agent == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        FacePlayer();

        if (isAttacking)
        {
            agent.isStopped = true;
            return;
        }

        if (distance <= attackRange)
        {
            agent.isStopped = true;
            agent.ResetPath();

            SetMovementAnimation(false, false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartAttack();
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (distance > runDistance)
            {
                agent.speed = runSpeed;
                SetMovementAnimation(false, true);
            }
            else
            {
                agent.speed = walkSpeed;
                SetMovementAnimation(true, false);
            }
        }
    }

    void ApplyFloorScaling()
    {
        int floor = FloorTextController.floorNumber;

        int bonusSteps = Mathf.Max(0, (floor / 5) - 1);
        float multiplier = 1f + (0.10f * bonusSteps);

        maxHP = baseHP * multiplier;
        currentATK = baseATK * multiplier;
    }


    private int currentRunVariant = -1;
    void SetMovementAnimation(bool walking, bool running)
    {
        if (animator == null) return;

        animator.SetBool("isRunning", running);

        if (running && currentRunVariant == -1)
        {
            currentRunVariant = Random.Range(0, 2);
            animator.SetInteger("runVariant", currentRunVariant);
        }

        if (!running)
        {
            currentRunVariant = -1;
        }
    }

    void FacePlayer()
    {
        if (player == null) return;

        Vector3 lookPos = player.position - transform.position;
        lookPos.y = 0f;

        if (lookPos.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    void StartAttack()
    {
        if (animator == null) return;

        isAttacking = true;
        lastAttackTime = Time.time;

        int choice = Random.Range(0, 2);

        if (choice == 0)
            animator.SetTrigger("attack1");
        else
            animator.SetTrigger("attack2");
    }

    public void EndAttack()
    {
        if (isDead) return;
        isAttacking = false;
    }

    public void DealDamageToPlayer()
    {
        if (isDead || !canDamagePlayer || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange + 0.75f) return;

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(Mathf.RoundToInt(currentATK));
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, 0);
    }

    public void TakeDamage(float damage, int slashChoice)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        UpdateHPUI();

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    void UpdateHPUI()
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = (maxHP <= 0f) ? 0f : currentHP / maxHP;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHP)}/{Mathf.CeilToInt(maxHP)}";
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        currentATK = 0f;
        canDamagePlayer = false;
        isAttacking = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (hpCanvas != null)
            hpCanvas.SetActive(false);

        if (animator != null)
        {
            int deathChoice = Random.Range(0, 2);
            if (deathChoice == 0)
                animator.SetTrigger("death1");
            else
                animator.SetTrigger("death2");
        }

        // destroy later after death animation finishes
        Destroy(gameObject, 10f);
    }
}
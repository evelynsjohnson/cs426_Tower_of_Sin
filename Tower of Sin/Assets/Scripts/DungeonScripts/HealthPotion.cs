using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    private Transform player;
    private PlayerHealth playerHealth;

    private float healAmount;
    public float magnetismRadius = 2f;
    public float flySpeed = 12f;
    public float hoverSpeed = 2f;
    public float hoverHeight = 0.2f;
    public float waitDelay = 2f;

    private bool isWaiting = false;
    private bool isFlyingToPlayer = false;
    private float currentWaitTimer = 0f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;

            playerHealth = pObj.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = pObj.GetComponentInChildren<PlayerHealth>();
            if (playerHealth == null) playerHealth = pObj.GetComponentInParent<PlayerHealth>();
        }

        // heal amount should be 1/X of the player's max health
        healAmount = playerHealth != null ? playerHealth.maxHealth / 6f : 50f;
        //healAmount = 50f;
    }

    void Update()
    {
        if (player == null) return;

        if (!isFlyingToPlayer)
        {
            transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * hoverSpeed) * hoverHeight, 0f);

            if (!isWaiting)
            {
                if (Vector3.Distance(transform.position, player.position) <= magnetismRadius)
                {
                    if (playerHealth != null && !playerHealth.IsFullHealth())
                    {
                        isWaiting = true;
                        currentWaitTimer = waitDelay;
                    }
                }
            }
            else
            {
                if (playerHealth != null && playerHealth.IsFullHealth())
                {
                    isWaiting = false;
                    return;
                }

                currentWaitTimer -= Time.deltaTime;
                if (currentWaitTimer <= 0f)
                {
                    isFlyingToPlayer = true;
                }
            }
        }
        else
        {
            Vector3 targetPos = player.position + Vector3.up * 1f;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, flySpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                if (playerHealth != null)
                {
                    playerHealth.Heal(healAmount);
                }

                Destroy(gameObject);
            }
        }
    }
}
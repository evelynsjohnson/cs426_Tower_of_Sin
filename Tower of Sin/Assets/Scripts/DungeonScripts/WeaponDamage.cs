using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    public float damage = 15f;

    public bool isAttacking = false;

    void OnTriggerEnter(Collider other)
    {
        if (isAttacking && other.CompareTag("Enemy"))
        {
            PrisonZombieAI zombie = other.GetComponent<PrisonZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage);

                // Optional: Turn off attacking immediately after the hit 
                // so the sword doesn't hit the same zombie twice in one swing
                isAttacking = false;
            }
        }
    }
}
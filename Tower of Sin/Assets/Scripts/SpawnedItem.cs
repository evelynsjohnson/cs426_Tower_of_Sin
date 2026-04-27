using UnityEngine;

public class SpawnedItem : MonoBehaviour
{
    public ItemData item;
    public UIManager playerUI;
    private InvManager inventory;
    private Transform playerTransform;

    private MeshRenderer sphere;
    private ParticleSystem particle;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerTransform = player.transform;

        sphere = GetComponentInChildren<MeshRenderer>();
        particle = GetComponentInChildren<ParticleSystem>();

        UnityEngine.Color hue = getColor(item.rank);
        sphere.material.color = hue;
        var par = particle.main;
        par.startColor = hue;
    }

    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inventory = playerTransform.GetComponent<InvManager>();

            inventory.AddItem(item);


            playerUI = FindFirstObjectByType<UIManager>();


            playerUI.ShowPickup(item.name);

            Destroy(gameObject);
        }
    }

    public void SetItem(ItemData set)
    {
        item = set;
    }

    UnityEngine.Color getColor(Rarity rank)
    {
        switch (rank)
        {
            case Rarity.Common:
                return UnityEngine.Color.white;
            case Rarity.Uncommon:
                return UnityEngine.Color.green;
            case Rarity.Rare:
                return UnityEngine.Color.blue;
            case Rarity.Epic:
                return UnityEngine.Color.magenta;
            case Rarity.Legendary:
                return UnityEngine.Color.yellow;
            default:
                return UnityEngine.Color.white;
        }
    }
}

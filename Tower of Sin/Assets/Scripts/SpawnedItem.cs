using UnityEngine;

public class SpawnedItem : MonoBehaviour
{
    public ItemData item;
    public UIManager playerUI;
    private InvManager inventory;
    private Transform playerTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerTransform = player.transform;
    }

    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inventory = playerTransform.GetComponent<InvManager>();

            inventory.AddItem(item);


            playerUI = FindObjectOfType<UIManager>();


            playerUI.ShowPickup("Found: " + item.name);

            Destroy(gameObject);
        }
    }

    public void SetItem(ItemData set)
    {
        item = set;
    }
}

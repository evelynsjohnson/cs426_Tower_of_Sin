using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TentacleBossUnit : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Animator animator;
    [SerializeField] private Image hpFill;
    [SerializeField] private Transform player;

    [Header("Stats")]
    [SerializeField] private float baseMaxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float baseDamage = 15f;

    [Header("Attack Timing")]
    [SerializeField] private float minAttackInterval = 2.5f;
    [SerializeField] private float maxAttackInterval = 4.5f;
    [SerializeField] private float telegraphLeadTime = 0.5f;
    [SerializeField] private float damageDelayAfterAnimStart = 1f;
    [SerializeField] private float postAttackCooldown = 0.5f;

    [Header("Attack Shape")]
    [SerializeField] private float attackWidth = 5f;
    [SerializeField] private float attackLength = 10f;
    [SerializeField] private float telegraphYOffset = 0.05f;

    [Header("Telegraph Visual")]
    [SerializeField] private float telegraphLineWidth = 0.15f;
    [SerializeField] private Color telegraphFillColor = new Color(1f, 0.15f, 0.1f, 0.22f);
    [SerializeField] private Color telegraphOutlineColor = new Color(0.45f, 0.05f, 0.02f, 0.95f);

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private static readonly int AnimAttack = Animator.StringToHash("attack");

    private GreedAI owner;
    private float maxHP;
    private float scaledDamage;
    private int floor = 5;
    private bool isDead = false;
    private bool isAttacking = false;

    private Coroutine attackLoopRoutine;
    private readonly List<GameObject> spawnedTelegraphs = new List<GameObject>();

    public void Initialize(GreedAI greed, float hp, Transform targetPlayer)
    {
        owner = greed;
        player = targetPlayer;
        maxHP = hp;
        currentHP = hp;
        UpdateUI();
    }

    public void SetFloor(int currentFloor)
    {
        floor = Mathf.Max(5, currentFloor);

        int steps = Mathf.Max(0, (floor / 5) - 1);
        maxHP = baseMaxHP * (1f + 0.10f * steps);
        scaledDamage = baseDamage * (1f + 0.10f * steps);

        if (currentHP <= 0f || currentHP > maxHP)
            currentHP = maxHP;

        UpdateUI();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        int steps = Mathf.Max(0, (floor / 5) - 1);
        maxHP = baseMaxHP * (1f + 0.10f * steps);
        scaledDamage = baseDamage * (1f + 0.10f * steps);

        if (currentHP <= 0f)
            currentHP = maxHP;
    }

    private void Start()
    {
        UpdateUI();

        if (attackLoopRoutine != null)
            StopCoroutine(attackLoopRoutine);

        attackLoopRoutine = StartCoroutine(AttackLoop());
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f) return;

        currentHP -= amount;
        currentHP = Mathf.Max(0f, currentHP);
        UpdateUI();

        if (currentHP <= 0f)
            Die();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage((float)amount);
    }

    private IEnumerator AttackLoop()
    {
        while (!isDead)
        {
            float wait = Random.Range(minAttackInterval, maxAttackInterval);
            yield return new WaitForSeconds(wait);

            if (isDead || isAttacking || player == null)
                continue;

            yield return StartCoroutine(DoAttack());
        }
    }

    private IEnumerator DoAttack()
    {
        isAttacking = true;

        AttackRect rect = BuildAttackRectFromCurrentPlayerPosition();

        GameObject telegraph = SpawnRectangleTelegraph(rect.center, rect.rotation, attackWidth, attackLength, "TentacleTelegraph");
        yield return new WaitForSeconds(telegraphLeadTime);

        if (animator != null)
        {
            animator.ResetTrigger(AnimAttack);
            animator.SetTrigger(AnimAttack);
        }

        yield return new WaitForSeconds(damageDelayAfterAnimStart);

        if (PlayerInsideAttackRect(rect))
        {
            TryDamagePlayer(player != null ? player.gameObject : null, scaledDamage);
        }

        if (telegraph != null)
            Destroy(telegraph);

        yield return new WaitForSeconds(postAttackCooldown);

        isAttacking = false;
    }

    private AttackRect BuildAttackRectFromCurrentPlayerPosition()
    {
        Vector3 start = transform.position;
        start.y = 0f;

        Vector3 target = player != null ? player.position : transform.position + transform.forward * attackLength;
        target.y = 0f;

        Vector3 dir = (target - start);
        if (dir.sqrMagnitude < 0.001f)
            dir = transform.forward;

        dir.Normalize();

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 center = transform.position + dir * (attackLength * 0.5f);
        center.y = transform.position.y + telegraphYOffset;

        return new AttackRect
        {
            center = center,
            rotation = rot,
            forward = dir
        };
    }

    private bool PlayerInsideAttackRect(AttackRect rect)
    {
        if (player == null) return false;

        Vector3 local = Quaternion.Inverse(rect.rotation) * (player.position - rect.center);
        return Mathf.Abs(local.x) <= attackWidth * 0.5f &&
               Mathf.Abs(local.z) <= attackLength * 0.5f;
    }

    private void TryDamagePlayer(GameObject playerObj, float damage)
    {
        if (playerObj == null || damage <= 0f) return;
        playerObj.SendMessage("TakeDamage", Mathf.RoundToInt(damage), SendMessageOptions.DontRequireReceiver);
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        if (attackLoopRoutine != null)
            StopCoroutine(attackLoopRoutine);

        ClearTelegraphs();

        if (owner != null)
            owner.NotifyTentacleDied(gameObject);

        Destroy(gameObject);
    }

    private void UpdateUI()
    {
        if (hpFill != null)
            hpFill.fillAmount = maxHP > 0f ? currentHP / maxHP : 0f;
    }

    private GameObject SpawnRectangleTelegraph(Vector3 center, Quaternion rotation, float width, float length, string name)
    {
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        Vector3[] localPoints =
        {
            new Vector3(-halfW, 0f, -halfL),
            new Vector3(-halfW, 0f,  halfL),
            new Vector3( halfW, 0f,  halfL),
            new Vector3( halfW, 0f, -halfL)
        };

        Vector3[] worldPoints = new Vector3[localPoints.Length];
        for (int i = 0; i < localPoints.Length; i++)
            worldPoints[i] = center + rotation * localPoints[i];

        return CreateFilledPolygonTelegraph(name, worldPoints);
    }

    private GameObject CreateFilledPolygonTelegraph(string name, Vector3[] worldPoints)
    {
        GameObject root = new GameObject(name);
        root.transform.position = Vector3.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(root.transform, false);

        MeshFilter mf = fillObj.AddComponent<MeshFilter>();
        MeshRenderer mr = fillObj.AddComponent<MeshRenderer>();

        Mesh mesh = BuildFlatPolygonMesh(worldPoints);
        mf.mesh = mesh;
        mr.material = CreateRuntimeColorMaterial(telegraphFillColor);

        CreateLineRenderer(root.transform, "Outline", CloseLoop(worldPoints), telegraphOutlineColor);

        spawnedTelegraphs.Add(root);
        return root;
    }

    private Mesh BuildFlatPolygonMesh(Vector3[] worldPoints)
    {
        Mesh mesh = new Mesh();

        Vector3[] verts = new Vector3[worldPoints.Length];
        for (int i = 0; i < worldPoints.Length; i++)
            verts[i] = worldPoints[i];

        List<int> tris = new List<int>();
        for (int i = 1; i < worldPoints.Length - 1; i++)
        {
            tris.Add(0);
            tris.Add(i);
            tris.Add(i + 1);
        }

        Vector2[] uv = new Vector2[verts.Length];
        for (int i = 0; i < uv.Length; i++)
            uv[i] = new Vector2(verts[i].x, verts[i].z);

        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private LineRenderer CreateLineRenderer(Transform parent, string objName, Vector3[] points, Color color)
    {
        GameObject lineObj = new GameObject(objName);
        lineObj.transform.SetParent(parent);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = points.Length;
        lr.SetPositions(points);

        lr.startWidth = telegraphLineWidth;
        lr.endWidth = telegraphLineWidth;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        lr.material = mat;

        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;

        return lr;
    }

    private Material CreateRuntimeColorMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        return mat;
    }

    private Vector3[] CloseLoop(Vector3[] points)
    {
        Vector3[] closed = new Vector3[points.Length + 1];
        for (int i = 0; i < points.Length; i++)
            closed[i] = points[i];
        closed[points.Length] = points[0];
        return closed;
    }

    private void ClearTelegraphs()
    {
        for (int i = 0; i < spawnedTelegraphs.Count; i++)
        {
            if (spawnedTelegraphs[i] != null)
                Destroy(spawnedTelegraphs[i]);
        }

        spawnedTelegraphs.Clear();
    }

    private struct AttackRect
    {
        public Vector3 center;
        public Quaternion rotation;
        public Vector3 forward;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.magenta;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(new Vector3(0f, 0f, attackLength * 0.5f), new Vector3(attackWidth, 0.1f, attackLength));
    }
}
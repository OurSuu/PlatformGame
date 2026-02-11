using UnityEngine;

public class EnermyAi : MonoBehaviour
{
    [Header("Pattern เดิน")]
    public Transform leftPoint;
    public Transform rightPoint;
    public float patrolSpeed = 2f;

    [Header("สายตา - มองเห็น Player")]
    public float sightRange = 8f;
    public float chaseSpeed = 4f;
    public float damagePerSecond = 15f;

    [Header("แสดงกรวยสายตา (ไฟเตือนเป็นสามเหลี่ยม)")]
    public bool showSightCone = true;
    [Tooltip("องศาครึ่งกรวย เช่น 25 = กว้างรวม ~50 องศา")]
    public float sightHalfAngle = 25f;
    public Color sightColor = new Color(1f, 1f, 0f, 0.25f);

    [Header("เอฟเฟกต์กรวยสายตาตามจุดสิ้นสุด Pattern")]
    [Tooltip("ให้กรวยสายตาแคบลงเมื่อใกล้ถึงจุดปลายทางเดิน")]
    public bool useDynamicSightWidth = true;
    [Range(0.05f, 1f)]
    [Tooltip("สัดส่วนความกว้างขั้นต่ำตอนใกล้สุดปลาย Pattern (1 = กว้างเท่าเดิม)")]
    public float minWidthScaleWhenClose = 0.3f;
    [Tooltip("**ระยะก่อนถึงปลาย Pattern ที่เริ่มค่อย ๆ ลดความกว้างของแสง (จะถูกละเลยในเวอร์ชันนี้, ไม่ต้องแก้ Inspector)**")]
    public float fadeDistanceFromEnd = 1.5f;

    [Header("Screen Shake (ไกล=เบา, ใกล้=แรง)")]
    public float shakeFarDistance = 6f;
    public float shakeNearDistance = 2f;
    public float shakeFarIntensity = 0.03f;
    public float shakeNearIntensity = 0.15f;

    [Header("เว้นระยะไม่ให้ศัตรูเข้าทับจุดปลาย Pattern")]
    public float stopOffsetFromPatternPoint = 0.15f;

    private Transform player;
    private Health playerHealth;
    private HidingSystem playerHiding;
    private SceenShake screenShake;
    private SpriteRenderer sprite;
    private Rigidbody2D rb;

    // กรวยสายตา (Mesh สามเหลี่ยม)
    private Transform sightConeTransform;
    private MeshFilter sightMeshFilter;
    private MeshRenderer sightMeshRenderer;
    private bool facingRight = true;

    private bool movingRight = true;
    private bool canSeePlayer;
    private float damageTimer;

    // ความยาวที่แท้จริงของกรวยสายตา (จะถูกอัปเดตทุก frame)
    private float visibleConeLength = -1f;
    // ความกว้างปลายกรวยแบบ dynamic (จะนำไปใช้ขณะวาด mesh)
    private float dynamicWidthForCone = -1f;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            playerHiding = player.GetComponent<HidingSystem>();
        }

        screenShake = Camera.main?.GetComponent<SceenShake>();
        sprite = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        CreateSightCone();

        if (leftPoint == null) leftPoint = transform;
        if (rightPoint == null) rightPoint = transform;
        if (leftPoint == rightPoint)
        {
            GameObject left = new GameObject("Enemy_LeftPoint");
            GameObject right = new GameObject("Enemy_RightPoint");
            left.transform.SetParent(transform.parent);
            right.transform.SetParent(transform.parent);
            left.transform.position = transform.position + Vector3.left * 3f;
            right.transform.position = transform.position + Vector3.right * 3f;
            leftPoint = left.transform;
            rightPoint = right.transform;
        }
    }

    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindWithTag("Player")?.transform;
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                playerHiding = player.GetComponent<HidingSystem>();
            }
            return;
        }
        if (playerHealth != null && playerHealth.IsDead) { canSeePlayer = false; EnemyAlertUI.IsPlayerSpotted = false; return; }

        CheckSight();
        EnemyAlertUI.IsPlayerSpotted = canSeePlayer;

        if (canSeePlayer)
        {
            ChasePlayer();
            DamagePlayerOverTime();
            UpdateScreenShake();
        }
        else
        {
            Patrol();
        }

        FlipSprite();
        UpdateSightConeVisual();
    }

    void CheckSight()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > sightRange)
        {
            canSeePlayer = false;
            return;
        }
        float minX = GetPatternMinX();
        float maxX = GetPatternMaxX();
        if (player.position.x < minX || player.position.x > maxX)
        {
            canSeePlayer = false;
            return;
        }
        bool facingRight = canSeePlayer ? (player.position.x > transform.position.x) : movingRight;
        bool playerInFront = (facingRight && player.position.x > transform.position.x) ||
                            (!facingRight && player.position.x < transform.position.x);
        if (!playerInFront)
        {
            canSeePlayer = false;
            return;
        }
        if (playerHiding != null && playerHiding.isHiding)
        {
            canSeePlayer = false;
            return;
        }
        canSeePlayer = true;
    }

    float GetPatternMinX() => Mathf.Min(leftPoint.position.x, rightPoint.position.x);
    float GetPatternMaxX() => Mathf.Max(leftPoint.position.x, rightPoint.position.x);

    float GetMinPatrolX() => GetPatternMinX() + stopOffsetFromPatternPoint;
    float GetMaxPatrolX() => GetPatternMaxX() - stopOffsetFromPatternPoint;

    void ClampToPattern()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, GetMinPatrolX(), GetMaxPatrolX());
        transform.position = pos;
    }

    void Patrol()
    {
        float minX = GetMinPatrolX();
        float maxX = GetMaxPatrolX();
        float myX = transform.position.x;

        // ปรับทิศทางเมื่อถึงระยะใกล้ pattern โดยหยุดก่อนถึงจุดปลาย pattern
        if (movingRight && myX >= maxX - 0.001f)
            movingRight = false;
        else if (!movingRight && myX <= minX + 0.001f)
            movingRight = true;

        float moveX = (movingRight ? patrolSpeed : -patrolSpeed) * Time.deltaTime;
        Vector3 targetMove = new Vector3(moveX, 0, 0);

        // ตรวจสอบถ้าขยับแล้วจะเกินขอบ ให้ตัดไม่ให้เลยออกไป (ไว้หยุดก่อนทับ)
        if (movingRight && myX + moveX > maxX)
            targetMove.x = maxX - myX;
        else if (!movingRight && myX + moveX < minX)
            targetMove.x = minX - myX;

        transform.Translate(targetMove, Space.World);
        ClampToPattern();
    }

    void ChasePlayer()
    {
        float minX = GetMinPatrolX();
        float maxX = GetMaxPatrolX();
        float dir = Mathf.Sign(player.position.x - transform.position.x);

        float nextX = transform.position.x + dir * chaseSpeed * Time.deltaTime;

        // Limit target position so enemy will not pass min/max safe patrol zone
        if (dir > 0 && nextX > maxX)
            nextX = maxX;
        else if (dir < 0 && nextX < minX)
            nextX = minX;

        transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
        // Clamp To Pattern แบบระยะปลอดภัย (ซ้ำเพื่อความแน่นอน)
        ClampToPattern();
    }

    void DamagePlayerOverTime()
    {
        if (playerHealth == null) return;
        damageTimer += Time.deltaTime;
        if (damageTimer >= 0.2f)
        {
            damageTimer = 0f;
            playerHealth.TakeDamage(damagePerSecond * 0.2f);
        }
    }

    void UpdateScreenShake()
    {
        if (screenShake == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        float t = Mathf.InverseLerp(shakeFarDistance, shakeNearDistance, dist);
        float intensity = Mathf.Lerp(shakeFarIntensity, shakeNearIntensity, t);
        screenShake.SetShakeIntensity(intensity);
    }

    void FlipSprite()
    {
        bool faceRight = canSeePlayer
            ? player.position.x > transform.position.x
            : movingRight;

        facingRight = faceRight;

        if (sprite != null)
            sprite.flipX = !faceRight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Health h = other.GetComponentInParent<Health>();
        if (h != null && !h.IsDead && !h.IsInvincible)
            if (playerHiding == null || !playerHiding.isHiding)
                h.Die();
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        Health h = other.gameObject.GetComponentInParent<Health>();
        if (h != null && !h.IsDead && !h.IsInvincible)
            if (playerHiding == null || !playerHiding.isHiding)
                h.Die();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Show safe zone for enemy movement (yellow lines at the offset zone)
        if (leftPoint != null && rightPoint != null)
        {
            float minX = (leftPoint.position.x < rightPoint.position.x ? leftPoint.position.x : rightPoint.position.x) + stopOffsetFromPatternPoint;
            float maxX = (leftPoint.position.x > rightPoint.position.x ? leftPoint.position.x : rightPoint.position.x) - stopOffsetFromPatternPoint;
            Vector3 p1 = new Vector3(minX, transform.position.y, transform.position.z);
            Vector3 p2 = new Vector3(maxX, transform.position.y, transform.position.z);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(p1 + Vector3.up * 0.2f, p1 + Vector3.down * 0.2f);
            Gizmos.DrawLine(p2 + Vector3.up * 0.2f, p2 + Vector3.down * 0.2f);
        }
    }

    #region Sight Cone (Triangle)
    void CreateSightCone()
    {
        if (!showSightCone) return;

        GameObject cone = new GameObject("Enemy_SightCone");
        cone.transform.SetParent(transform);
        cone.transform.localPosition = Vector3.zero;
        cone.transform.localRotation = Quaternion.identity;
        cone.transform.localScale = Vector3.one;

        sightConeTransform = cone.transform;
        sightMeshFilter = cone.AddComponent<MeshFilter>();
        sightMeshRenderer = cone.AddComponent<MeshRenderer>();

        if (sprite != null)
        {
            sightMeshRenderer.sortingLayerID = sprite.sortingLayerID;
            sightMeshRenderer.sortingOrder = sprite.sortingOrder - 1;
        }

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = sightColor;
        sightMeshRenderer.material = mat;

        UpdateSightConeMesh();
        UpdateSightConeVisual();
    }

    // === ปรับให้กรวยตัดเฉพาะขอบ pattern และฐานกรวยสิ้นสุดจริงตามตำแหน่ง pattern ===
    void UpdateSightConeMesh()
    {
        if (sightMeshFilter == null) return;

        float len = (visibleConeLength > 0f) ? visibleConeLength : sightRange;
        float tipWidthScale = (dynamicWidthForCone > 0f) ? dynamicWidthForCone : 1f;

        Mesh mesh = new Mesh();

        // กำหนดความกว้างฐานที่ปลายกรวย
        float tipWidth = len * Mathf.Tan(sightHalfAngle * Mathf.Deg2Rad) * tipWidthScale;

        Vector3[] vertices = new Vector3[3];
        vertices[0] = Vector3.zero;                // จุดศูนย์กลางที่ enemy
        vertices[1] = new Vector3(len, +tipWidth, 0);  // ขอบปลายบน (ปลาย pattern)
        vertices[2] = new Vector3(len, -tipWidth, 0);  // ขอบปลายล่าง (ปลาย pattern)

        int[] triangles = { 0, 1, 2 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        sightMeshFilter.mesh = mesh;
    }

    void UpdateSightConeVisual()
    {
        if (sightConeTransform == null || sightMeshRenderer == null) return;

        // ถ้าปิดการแสดงผล ก็ปิด renderer ไปเลย
        if (!showSightCone)
        {
            sightMeshRenderer.enabled = false;
            return;
        }

        sightMeshRenderer.enabled = true;

        // ให้กรวยพุ่งไปด้านที่ศัตรูกำลังมอง
        float dir = facingRight ? 1f : -1f;
        float minX = GetMinPatrolX();
        float maxX = GetMaxPatrolX();
        float myX = transform.position.x;
        float targetEndX = facingRight ? maxX : minX;

        float distToEnd = Mathf.Abs(targetEndX - myX);
        float coneDrawLength = Mathf.Min(sightRange, distToEnd);

        // ป้องกันกรวยหายเมื่อชิด pattern
        if (coneDrawLength <= 0.01f)
        {
            sightMeshRenderer.enabled = false;
            visibleConeLength = 0;
            return;
        }

        sightMeshRenderer.enabled = true;

        visibleConeLength = coneDrawLength;

        // t = สัดส่วนระยะปกติภายในกรวย
        float t = (sightRange <= 0f) ? 0f : Mathf.Clamp01(coneDrawLength / sightRange);
        dynamicWidthForCone = useDynamicSightWidth ? Mathf.Lerp(minWidthScaleWhenClose, 1f, t) : 1f;

        // ปักกรวยไว้ที่แกน x = 0 (local), ให้หมุนกลับตาม direction
        sightConeTransform.localScale = new Vector3(dir, 1f, 1f);

        // อัปเดต mesh ใหม่ (ความยาวและปลาย)
        UpdateSightConeMesh();
    }
    #endregion
}

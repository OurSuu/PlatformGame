using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("แสดงกรวยสายตา")]
    public bool showSightCone = true;
    [Tooltip("องศาครึ่งกรวย เช่น 25 = กว้างรวม 50 องศา")]
    public float sightHalfAngle = 25f;
    public Color sightColor = new Color(1f, 1f, 0f, 0.25f);

    [Header("ปรับตำแหน่งและองศาไฟ")]
    [Tooltip("ตำแหน่งไฟ (X: เดินหน้า/ถอยหลัง, Y: ความสูง) แนะนำ Y = 1.5")]
    public Vector3 sightOffset = new Vector3(0.5f, 1.5f, 0f);
    [Tooltip("องศาการก้มมอง (ค่าติดลบคือมองเฉียงลงพื้น เช่น -35)")]
    public float sightTiltAngle = -35f;

    [Header("ระบบแสงสมจริง (ยิง Raycast)")]
    [Tooltip("เลือก Layer ที่เป็นพื้นหรือกำแพง (เพื่อให้แสงส่องทาบไปบนพื้น)")]
    public LayerMask obstacleMask;
    [Range(5, 50)]
    [Tooltip("ความละเอียดของเส้นแสง (แนะนำ 20)")]
    public int rayCount = 20;

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

    private Transform sightConeTransform;
    private MeshFilter sightMeshFilter;
    private MeshRenderer sightMeshRenderer;
    private bool facingRight = true;
    private bool movingRight = true;
    private bool canSeePlayer;

    private float damageTimer = 0f;
    private float sightGraceTimer = 0f;

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
        if (playerHealth != null && playerHealth.IsDead)
        {
            canSeePlayer = false;
            EnemyAlertUI.IsPlayerSpotted = false;
            return;
        }

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
            damageTimer = 0f;
        }

        FlipSprite();
        UpdateSightConeVisual();
    }

    public void RegisterSightFromTrigger()
    {
        sightGraceTimer = 0.05f;
    }

    void CheckSight()
    {
        if (playerHiding != null && playerHiding.isHiding)
        {
            canSeePlayer = false;
            return;
        }

        if (sightGraceTimer > 0f)
        {
            canSeePlayer = true;
            sightGraceTimer -= Time.deltaTime;
        }
        else
        {
            canSeePlayer = false;
        }
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

        if (movingRight && myX >= maxX - 0.001f) movingRight = false;
        else if (!movingRight && myX <= minX + 0.001f) movingRight = true;

        float moveX = (movingRight ? patrolSpeed : -patrolSpeed) * Time.deltaTime;
        Vector3 targetMove = new Vector3(moveX, 0, 0);

        if (movingRight && myX + moveX > maxX) targetMove.x = maxX - myX;
        else if (!movingRight && myX + moveX < minX) targetMove.x = minX - myX;

        transform.Translate(targetMove, Space.World);
        ClampToPattern();
    }

    void ChasePlayer()
    {
        float minX = GetMinPatrolX();
        float maxX = GetMaxPatrolX();
        float dir = Mathf.Sign(player.position.x - transform.position.x);

        float nextX = transform.position.x + dir * chaseSpeed * Time.deltaTime;

        if (dir > 0 && nextX > maxX) nextX = maxX;
        else if (dir < 0 && nextX < minX) nextX = minX;

        transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
        ClampToPattern();
    }

    void DamagePlayerOverTime()
    {
        if (playerHealth == null) return;

        if (damageTimer <= 0f)
        {
            playerHealth.TakeDamage(damagePerSecond * 0.2f);
            EnemyAlertUI.NotifyPlayerDamaged();
            damageTimer = 0.2f;
        }
        else
        {
            damageTimer -= Time.deltaTime;
        }
    }

    void UpdateScreenShake()
    {
        if (screenShake == null) return;

        float dist = Vector2.Distance(sightConeTransform.position, player.position);
        float t = Mathf.InverseLerp(sightRange, shakeNearDistance, dist);
        float intensity = Mathf.Lerp(shakeFarIntensity, shakeNearIntensity, t);

        screenShake.SetShakeIntensity(intensity);
    }

    void FlipSprite()
    {
        bool faceRight = canSeePlayer ? player.position.x > transform.position.x : movingRight;
        facingRight = faceRight;
        if (sprite != null) sprite.flipX = !faceRight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Health h = other.GetComponentInParent<Health>();
        if (h != null && !h.IsDead && !h.IsInvincible)
            if (playerHiding == null || !playerHiding.isHiding)
                h.Die();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);

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

    #region Raycast Sight Cone (Realistic)
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

        Rigidbody2D coneRb = cone.AddComponent<Rigidbody2D>();
        coneRb.isKinematic = true;
        PolygonCollider2D polyCol = cone.AddComponent<PolygonCollider2D>();
        polyCol.isTrigger = true;

        SightConeTrigger trigger = cone.AddComponent<SightConeTrigger>();
        trigger.ai = this;

        UpdateSightConeVisual();
    }

    void UpdateSightConeVisual()
    {
        if (sightConeTransform == null || sightMeshRenderer == null) return;

        if (!showSightCone)
        {
            sightMeshRenderer.enabled = false;
            return;
        }

        sightMeshRenderer.enabled = true;
        float dir = facingRight ? 1f : -1f;

        // ขยับจุดกำเนิดแสง
        sightConeTransform.localPosition = new Vector3(sightOffset.x * dir, sightOffset.y, sightOffset.z);
        sightConeTransform.localRotation = Quaternion.Euler(0, 0, sightTiltAngle * dir);
        sightConeTransform.localScale = new Vector3(dir, 1f, 1f);

        UpdateSightConeMesh(dir);
    }

    void UpdateSightConeMesh(float dir)
    {
        if (sightMeshFilter == null) return;

        int rays = Mathf.Max(2, rayCount);
        Vector3[] vertices = new Vector3[rays + 2];
        vertices[0] = Vector3.zero; // จุดกำเนิดตา

        List<Vector2> colPoints = new List<Vector2>();
        colPoints.Add(Vector2.zero);

        // คำนวณองศา
        float baseWorldAngle = facingRight ? 0f : 180f;
        baseWorldAngle += sightTiltAngle * dir; 
        
        float currentAngle = baseWorldAngle - sightHalfAngle;
        float angleStep = (sightHalfAngle * 2f) / rays;

        float minPatrolX = GetMinPatrolX();
        float maxPatrolX = GetMaxPatrolX();
        float eyeX = sightConeTransform.position.x; // พิกัดตาปัจจุบัน

        for (int i = 0; i <= rays; i++)
        {
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 worldDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            
            // 1. คำนวณระยะทางสูงสุดที่จะไม่ทะลุ Patrol Zone (แก้ปัญหาแสงปลิ้น)
            float maxAllowedDist = sightRange;
            
            if (worldDir.x > 0) 
            {
                // ถ้ายิงไปขวา หาระยะไปถึงขอบขวา
                float distToMaxX = (maxPatrolX - eyeX) / worldDir.x;
                if (distToMaxX < maxAllowedDist) maxAllowedDist = Mathf.Max(0, distToMaxX);
            }
            else if (worldDir.x < 0) 
            {
                // ถ้ายิงไปซ้าย หาระยะไปถึงขอบซ้าย
                float distToMinX = (minPatrolX - eyeX) / worldDir.x;
                if (distToMinX < maxAllowedDist) maxAllowedDist = Mathf.Max(0, distToMinX);
            }

            // 2. ยิง Raycast ด้วยระยะที่ปลอดภัยแล้ว (ไม่ทะลุกำแพง และไม่ปลิ้นย้อนหลัง)
            RaycastHit2D hit = Physics2D.Raycast(sightConeTransform.position, worldDir, maxAllowedDist, obstacleMask);
            
            // 3. กำหนดจุดตกกระทบ
            Vector3 worldPos;
            if (hit.collider != null) 
            {
                worldPos = hit.point;
            }
            else 
            {
                worldPos = sightConeTransform.position + worldDir * maxAllowedDist;
            }

            // แปลงกลับเป็น Local สำหรับวาด Mesh
            Vector3 localPos = sightConeTransform.InverseTransformPoint(worldPos);
            localPos.z = 0;

            vertices[i + 1] = localPos;
            colPoints.Add(localPos);

            currentAngle += angleStep;
        }

        // สร้างและอัปเดต Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;

        int[] triangles = new int[rays * 3];
        for (int i = 0; i < rays; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        sightMeshFilter.mesh = mesh;

        // อัปเดต Collider ให้ตรงกับแสงเป๊ะๆ
        PolygonCollider2D polyCol = sightConeTransform.GetComponent<PolygonCollider2D>();
        if (polyCol != null)
        {
            polyCol.points = colPoints.ToArray();
        }
    }
    #endregion
}

// --- ไฟล์นี้ท้ายสุด: ใช้สำหรับรับ Trigger โซนของไฟ ---
public class SightConeTrigger : MonoBehaviour
{
    public EnermyAi ai;
    void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            ai.RegisterSightFromTrigger();
        }
    }
}

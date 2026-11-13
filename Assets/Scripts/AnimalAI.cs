using System.Collections;
using UnityEngine;

/// <summary>
/// AnimalAI - Trí tuệ nhân tạo (AI) điều khiển hành vi động vật trong game sinh tồn 3D:
/// - Tự do đi lang thang (Wander / Roam) quanh khu vực sống tự nhiên
/// - Nghỉ ngơi, ăn cỏ, quan sát xung quanh (Idle)
/// - Phát hiện người chơi và bỏ chạy (Flee) đối với loài thú ăn cỏ (Hươu, Thỏ, Sóc, Ngựa)
/// - Cảnh giác hoặc tuần tra đối với thú săn mồi / thú lớn (Sói, Gấu, Lợn rừng, Bò rừng)
/// - Thân thiện đi lại quanh căn cứ đối với Chó săn (Dog) & Ngựa (Horse)
/// - Bám sát địa hình (Terrain height sampling) mượt mà, không bị chìm hay lơ lửng
/// - Tự động tránh đi xuống lòng sông / hồ nước sâu
/// - Đồng bộ chuẩn xác Animation Controller (Move, Run, Idle)
/// </summary>
public class AnimalAI : MonoBehaviour
{
    public enum AnimalState
    {
        Idle,
        Wander,
        Flee,
        Alert
    }

    [Header("Thống số di chuyển")]
    public float walkSpeed = 1.8f;
    public float runSpeed = 5.0f;
    public float rotationSpeed = 4.5f;
    public float wanderRadius = 16.0f;
    public float minIdleTime = 3.0f;
    public float maxIdleTime = 7.0f;
    public float maxWanderTime = 10.0f;
    public float yOffset = 0.0f;

    [Header("Tập tính sinh học & AI")]
    public bool isPrey = true;          // Động vật ăn cỏ bỏ chạy khi người chơi lại gần
    public bool isCompanion = false;     // Thú nuôi / Bạn đồng hành trong căn cứ
    public float detectionRadius = 8.0f;
    public float fleeDistance = 18.0f;

    [Header("Trạng thái hiện tại")]
    public AnimalState currentState = AnimalState.Idle;

    private Vector3 homePosition;
    private Vector3 currentDestination;
    private float stateTimer = 0f;
    private float wanderTimer = 0f;
    private Transform playerTransform;
    private Terrain terrain;
    private Animator animator;

    // Hashes parameter Animator
    private int moveParamHash;
    private int runParamHash;
    private bool hasMoveParam = false;
    private bool hasRunParam = false;

    private const float MinWaterSafeElevation = 6.85f; // Độ cao an toàn trên mặt nước

    void Start()
    {
        terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain != null)
        {
            float startH = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
            Vector3 startP = transform.position;
            startP.y = startH + yOffset;
            transform.position = startP;
        }
        homePosition = transform.position;

        // Tắt ảnh hưởng của Rigidbody vật lý nếu có để di chuyển mượt mà
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Tìm Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null) playerTransform = player.transform;

        // Tìm Animator trên chính đối tượng hoặc con cháu
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            foreach (var p in animator.parameters)
            {
                if (p.name == "Move") { hasMoveParam = true; moveParamHash = Animator.StringToHash("Move"); }
                if (p.name == "Run")  { hasRunParam = true;  runParamHash  = Animator.StringToHash("Run"); }
            }
        }

        EnterIdleState();
    }

    void Update()
    {
        // 1. Kiểm tra phát hiện người chơi
        CheckPlayerProximity();

        // 2. Cập nhật State Machine
        switch (currentState)
        {
            case AnimalState.Idle:
                UpdateIdle();
                break;
            case AnimalState.Wander:
                UpdateWander();
                break;
            case AnimalState.Flee:
                UpdateFlee();
                break;
            case AnimalState.Alert:
                UpdateAlert();
                break;
        }

        // 3. Đảm bảo động vật luôn đứng vững trên mặt đất tự nhiên (Terrain)
        StickToTerrain();
    }

    private void CheckPlayerProximity()
    {
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (isCompanion)
        {
            // Chó săn thân thiện: nhìn về phía người chơi khi người chơi đến gần
            if (distToPlayer < 4.5f && currentState == AnimalState.Idle)
            {
                Vector3 lookDir = playerTransform.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), rotationSpeed * Time.deltaTime);
                }
            }
            return;
        }

        if (isPrey)
        {
            // Động vật ăn cỏ (Hươu, Thỏ, Sóc, Ngựa) bỏ chạy khi người chơi đến gần
            if (distToPlayer < detectionRadius && currentState != AnimalState.Flee)
            {
                EnterFleeState();
            }
        }
        else
        {
            // Thú dữ (Sói, Gấu) dừng lại quan sát người chơi trong cự ly gần
            if (distToPlayer < detectionRadius && currentState != AnimalState.Alert && currentState != AnimalState.Flee)
            {
                EnterAlertState();
            }
        }
    }

    private void EnterIdleState()
    {
        currentState = AnimalState.Idle;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);
        SetAnimParameters(false, false);
    }

    private void UpdateIdle()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            PickNewWanderDestination();
        }
    }

    private void PickNewWanderDestination()
    {
        for (int i = 0; i < 12; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 testPos = homePosition + new Vector3(randomCircle.x, 0, randomCircle.y);

            float elev = (terrain != null) ? terrain.SampleHeight(testPos) : testPos.y;

            // Không đi xuống lòng sông hoặc hồ nước sâu
            if (elev >= MinWaterSafeElevation)
            {
                testPos.y = elev;
                currentDestination = testPos;
                currentState = AnimalState.Wander;
                wanderTimer = maxWanderTime;
                SetAnimParameters(true, false);
                return;
            }
        }

        // Nếu không tìm được điểm phù hợp, tiếp tục nghỉ ngơi
        EnterIdleState();
    }

    private void UpdateWander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            EnterIdleState();
            return;
        }

        MoveTowards(currentDestination, walkSpeed);

        Vector3 flatDest = currentDestination;
        flatDest.y = transform.position.y;
        if (Vector3.Distance(transform.position, flatDest) < 0.9f)
        {
            EnterIdleState();
        }
    }

    private void EnterFleeState()
    {
        currentState = AnimalState.Flee;
        stateTimer = Random.Range(3.5f, 6.0f);
        SetAnimParameters(true, true);

        // Chạy ra xa khỏi vị trí của người chơi
        Vector3 fleeDir = (transform.position - playerTransform.position).normalized;
        if (fleeDir.sqrMagnitude < 0.01f) fleeDir = transform.forward;

        Vector3 target = transform.position + fleeDir * fleeDistance;
        if (terrain != null)
        {
            float elev = terrain.SampleHeight(target);
            if (elev < MinWaterSafeElevation)
            {
                // Nếu hướng chạy đâm vào sông, rẽ vuông góc
                fleeDir = Quaternion.Euler(0, 90f, 0) * fleeDir;
                target = transform.position + fleeDir * fleeDistance;
            }
        }
        currentDestination = target;
    }

    private void UpdateFlee()
    {
        stateTimer -= Time.deltaTime;
        MoveTowards(currentDestination, runSpeed);

        if (stateTimer <= 0f || (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > fleeDistance + 4f))
        {
            EnterIdleState();
        }
    }

    private void EnterAlertState()
    {
        currentState = AnimalState.Alert;
        stateTimer = Random.Range(2.5f, 5.0f);
        SetAnimParameters(false, false);
    }

    private void UpdateAlert()
    {
        stateTimer -= Time.deltaTime;

        if (playerTransform != null)
        {
            Vector3 lookDir = playerTransform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), rotationSpeed * 1.5f * Time.deltaTime);
            }
        }

        if (stateTimer <= 0f || (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > detectionRadius + 3f))
        {
            EnterIdleState();
        }
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 moveDir = target - transform.position;
        moveDir.y = 0;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    private void StickToTerrain()
    {
        if (terrain == null) terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain == null) return;

        float h = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
        Vector3 p = transform.position;
        p.y = h + yOffset;
        transform.position = p;
    }

    private void SetAnimParameters(bool isMoving, bool isRunning)
    {
        if (animator == null) return;
        if (hasMoveParam) animator.SetBool(moveParamHash, isMoving);
        if (hasRunParam)  animator.SetBool(runParamHash,  isRunning);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = (Application.isPlaying) ? homePosition : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);

        Gizmos.color = isPrey ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

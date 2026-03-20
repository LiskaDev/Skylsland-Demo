using UnityEngine;

public class PaperPlane : MonoBehaviour
{
    [Header("飞行参数")]
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float minSpeed = 3f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 4f;
    [SerializeField] private float turnSpeed = 80f;
    [SerializeField] private float pitchSpeed = 60f;
    [SerializeField] private float bankAngle = 25f;
    [SerializeField] private float boardDistance = 3f;

    [Header("降落检测")]
    [SerializeField] private float landingSpeed = 3f;    // 降落时的减速速度
    [SerializeField] private bool isLanded = false;      // 是否已降落

    [Header("状态")]
    [SerializeField] private bool isFlying = false;
    [SerializeField] private float currentSpeed = 0f;

    [Header("UI")]
    [SerializeField] private GameObject planeUI;

    [Header("起飞保护")]
    [SerializeField] private float takeoffProtectTime = 0.8f; // 上机后这段时间不判定降落
    [SerializeField] private float takeoffAutoLift = 2f;      // 起飞保护期间的自动上抬速度

    private Transform player;
    private PlayerMovement playerMovement;
    private Rigidbody playerRb;
    private Transform seatPoint;
    private float currentBankAngle = 0f;
    private float currentYaw = 0f;

    // 飞机自己的Rigidbody，用于降落物理
    private Rigidbody rb;

    // 新增字段
    private Collider playerCol;
    private Collider planeCol;

    private float takeoffTimer = 0f;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        playerMovement = player.GetComponent<PlayerMovement>();
        playerRb = player.GetComponent<Rigidbody>();
        playerCol = player.GetComponent<Collider>();
        planeCol = GetComponent<Collider>();

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;   // 停在地面时不受物理推动，上机时BoardPlane()会解除
        rb.freezeRotation = true;

        GameObject seat = new GameObject("SeatPoint");
        seat.transform.SetParent(transform);
        seat.transform.localPosition = new Vector3(0, 0.5f, 0);
        seatPoint = seat.transform;

        currentSpeed = minSpeed;
    }

    void Update()
    {
        if (planeUI != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            planeUI.SetActive(!isFlying && dist <= boardDistance);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (!isFlying && dist <= boardDistance)
                BoardPlane();
            else if (isFlying)
                ExitPlane();
        }

        // 起飞保护计时（保护结束后恢复飞机碰撞体）
        if (isFlying && takeoffTimer > 0f)
        {
            takeoffTimer -= Time.deltaTime;
            if (takeoffTimer <= 0f && planeCol != null)
                planeCol.enabled = true;
        }

        if (isFlying && !isLanded)
        {
            HandleFlight();
            player.position = seatPoint.position;
            player.rotation = transform.rotation;
        }

        if (isFlying && isLanded)
        {
            player.position = seatPoint.position;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, landingSpeed * Time.deltaTime);

            if (currentSpeed > 0f)
                transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
        }
    }

    void HandleFlight()
    {
        // 速度控制
        float speedInput = Input.GetAxis("Vertical");
        if (speedInput > 0)
            currentSpeed += acceleration * Time.deltaTime;
        else if (speedInput < 0)
            currentSpeed -= deceleration * Time.deltaTime;
        else
        {
            float midSpeed = (maxSpeed + minSpeed) * 0.5f;
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, midSpeed, deceleration * 0.3f * Time.deltaTime);
        }
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // 转向：累加yaw角度，用自己维护的变量，不从eulerAngles读取
        float turnInput = Input.GetAxis("Horizontal");
        currentYaw += turnInput * turnSpeed * Time.deltaTime;

        // 机身倾斜
        float targetBankAngle = -turnInput * bankAngle;
        currentBankAngle = Mathf.Lerp(
            currentBankAngle, targetBankAngle, 5f * Time.deltaTime);

        // 用四元数组合旋转：先yaw再bank，避免euler角读写导致的漂移
        transform.rotation = Quaternion.Euler(0f, currentYaw, currentBankAngle);

        // 高度控制
        float heightInput = 0f;
        if (Input.GetKey(KeyCode.Q)) heightInput = 1f;
        if (Input.GetKey(KeyCode.Z)) heightInput = -1f;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        heightInput += scroll * 3f;

        Vector3 pos = transform.position;
        pos.y += heightInput * pitchSpeed * Time.deltaTime;

        // 起飞保护期间自动抬升，避免刚上机贴地触发降落
        if (takeoffTimer > 0f)
            pos.y += takeoffAutoLift * Time.deltaTime;

        transform.position = pos;

        // 向前移动
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    // 碰 collision 检测：飞机碰到任何东西
    void OnCollisionEnter(Collision collision)
    {
        // 起飞保护期间不判定降落
        if (!isFlying || isLanded || takeoffTimer > 0f) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                // 只在碰到 Ground 标签时才允许降落
                if (collision.gameObject.CompareTag("Ground"))
                    Land(contact.point);
                break;
            }
        }
    }

    void Land(Vector3 landingPoint)
    {
        isLanded = true;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;                           // 锁死物理
        rb.constraints = RigidbodyConstraints.FreezeAll; // 冻结一切

        currentBankAngle = 0f;
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        Vector3 pos = transform.position;
        pos.y = landingPoint.y + 0.2f;
        transform.position = pos;

        Debug.Log("降落成功！按F下机。");
    }

    void BoardPlane()
    {
        isFlying = true;
        isLanded = false;
        takeoffTimer = takeoffProtectTime;

        if (planeCol != null)
            planeCol.enabled = false;

        // 解除之前下机时的锁定
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        playerRb.velocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.isKinematic = true;
        playerMovement.enabled = false;

        if (playerCol != null && planeCol != null)
            Physics.IgnoreCollision(playerCol, planeCol, true);

        // 起飞瞬间先把飞机抬高一点，脱离地面
        Vector3 pos = transform.position;
        pos.y += 1.5f;
        transform.position = pos;

        // 记录当前飞机的yaw角度，作为飞行中的初始朝向
        currentYaw = transform.eulerAngles.y;
        currentBankAngle = 0f;

        currentSpeed = minSpeed;
        Debug.Log("上机！W加速 S减速 AD转向 Q上升 Z下降 F下机");
    }

    void ExitPlane()
    {
        isFlying = false;
        isLanded = false;

        // 确保碰撞体开启
        if (planeCol != null)
            planeCol.enabled = true;

        // 先把飞机自身旋转清理干净：去掉bank角，只保留yaw
        currentBankAngle = 0f;
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        // 飞机停在原地不动，锁死物理
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 恢复碰撞
        if (playerCol != null && planeCol != null)
            Physics.IgnoreCollision(playerCol, planeCol, false);

        playerRb.isKinematic = false;
        playerMovement.enabled = true;

        // 把玩家放到飞机旁边
        player.position = transform.position + Vector3.up * 2f;

        // 用我们自己维护的yaw角度直接设置玩家朝向，干净可靠
        player.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        playerRb.velocity = Vector3.zero;
        currentSpeed = minSpeed;
        Debug.Log("下机！");
    }
}
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

    [Header("麦克风检测")]
    // 吹气触发的音量阈值，0-1之间，越小越灵敏
    [SerializeField] private float blowThreshold = 0.1f;
    // 吹气持续多少秒才触发（防止误触）
    [SerializeField] private float blowDuration = 0.3f;
    // 是否启用麦克风控制
    [SerializeField] private bool micEnabled = true;
    // 手动指定麦克风设备序号（0=第一个，1=第二个）
    [SerializeField] private int micDeviceIndex = 0;

    // 麦克风相关内部变量
    private AudioClip micClip;
    private string micDevice;
    private float blowTimer = 0f;
    private bool micReady = false;

    [Header("调试")]
    [SerializeField] private TMPro.TextMeshProUGUI micVolumeText;

    [Header("UI")]
    [SerializeField] private GameObject planeUI;

    [Header("起飞保护")]
    [SerializeField] private float takeoffProtectTime = 0.8f; // 上机后这段时间不判定降落
    [SerializeField] private float takeoffAutoLift = 2f;      // 起飞保护期间的自动上抬速度

    // ===== 能量系统 =====
    [Header("能量系统")]
    private float energy = 0f;                                // 当前能量，范围 0~100，初始 0

    [Header("Energy UI")]
    [SerializeField] private UnityEngine.UI.Slider energySlider; // 能量条 Slider

    // 能量条颜色阈值用的图片组件（运行时从 energySlider.fillRect 获取）
    private UnityEngine.UI.Image energyFillImage;
    // ====================

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

        // 初始化麦克风
        InitMicrophone();

        // 缓存能量条的 Fill 图片组件，用于动态改颜色
        if (energySlider != null && energySlider.fillRect != null)
            energyFillImage = energySlider.fillRect.GetComponent<UnityEngine.UI.Image>();
    }

    void InitMicrophone()
    {
        // 检查有没有麦克风
        if (Microphone.devices.Length == 0)
        {
            Debug.Log("没有检测到麦克风！");
            micEnabled = false;
            return;
        }

        // 打印所有麦克风设备，方便调试
        foreach (string device in Microphone.devices)
        {
            Debug.Log("检测到麦克风：" + device);
        }

        // 先给一个默认值
        micDevice = Microphone.devices[0];

        // 优先使用Inspector指定的设备序号
        if (micDeviceIndex >= 0 && micDeviceIndex < Microphone.devices.Length)
        {
            micDevice = Microphone.devices[micDeviceIndex];
        }
        else
        {
            // 序号无效时，自动优先选择外置/无线类设备
            foreach (string device in Microphone.devices)
            {
                string lower = device.ToLower();
                if (lower.Contains("usb") ||
                    lower.Contains("wireless") ||
                    lower.Contains("外置") ||
                    lower.Contains("headset"))
                {
                    micDevice = device;
                    break;
                }
            }
        }

        // 开始录音：设备名，循环录制，1秒缓冲，44100采样率
        micClip = Microphone.Start(micDevice, true, 1, 44100);

        micReady = true;
        Debug.Log("麦克风已就绪：" + micDevice);
    }

    void Update()
    {
        if (planeUI != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            planeUI.SetActive(!isFlying && dist <= boardDistance);
        }

        // 麦克风吹气 → 充能（未上机时才充能）
        if (micEnabled && micReady && !isFlying)
        {
            CheckBlowing();
        }

        // ===== 能量消耗 & 强制下机 =====
        if (isFlying && !isLanded)
        {
            // 飞行中每秒消耗 4 点能量
            energy = Mathf.Max(energy - 4f * Time.deltaTime, 0f);

            // 能量耗尽且速度已降至最低 → 强制下机
            if (energy <= 0f && currentSpeed <= minSpeed)
            {
                Debug.Log("能量耗尽，强制降落！");
                ExitPlane();
            }
        }

        // 每帧更新能量条 UI
        UpdateEnergyUI();
        // ================================

        // 显示当前音量（调试用）
        if (micVolumeText != null)
        {
            if (!micEnabled)
            {
                micVolumeText.text = "Mic: OFF";
            }
            else
            {
                float vol = GetMicVolume();
                micVolumeText.text = "Mic: " + vol.ToString("F3");
            }
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
        if (speedInput > 0 && energy > 0f)  // 能量耗尽时不允许加速
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

        // 能量耗尽时：速度向 minSpeed 滑行（滑翔感），转向和升降仍可用
        if (energy <= 0f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, minSpeed, deceleration * Time.deltaTime);

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
        // ===== 能量检测：不足 20 不允许上机 =====
        if (energy < 20f)
        {
            Debug.Log("Energy low! 请先吹气充能至 20 以上。");
            return;
        }
        // ========================================

        SoundManager.Instance?.PlaySound("Plane");
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

        if (planeCol != null)
            planeCol.enabled = true;

        currentBankAngle = 0f;
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        // 先清除速度（此时还没设isKinematic）
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 再锁死
        rb.isKinematic = true;

        if (playerCol != null && planeCol != null)
            Physics.IgnoreCollision(playerCol, planeCol, false);

        playerRb.isKinematic = false;
        playerMovement.enabled = true;
        player.position = transform.position + Vector3.up * 2f;
        player.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        playerRb.velocity = Vector3.zero;
        currentSpeed = minSpeed;
        Debug.Log("下机！");
    }

    void CheckBlowing()
    {
        float volume = GetMicVolume();
        float dist = Vector3.Distance(transform.position, player.position);

        if (volume > blowThreshold && dist <= boardDistance)
        {
            // 在飞机旁吹气 → 持续充能（上限 100）
            if (energy < 100f)
            {
                energy = Mathf.Min(energy + 30f * Time.deltaTime, 100f);
                Debug.Log("充能中... Energy: " + energy.ToString("F1"));
            }
            // 无论充能与否，重置旧版 blowTimer，保留字段兼容性
            blowTimer = 0f;
        }
        else
        {
            blowTimer = 0f;
        }
    }

    float GetMicVolume()
    {
        if (!micReady || micClip == null) return 0f;

        // 获取当前麦克风录制位置
        int micPosition = Microphone.GetPosition(micDevice);
        if (micPosition <= 0) return 0f;

        // 读取最近256个采样点
        int sampleWindow = 256;
        float[] samples = new float[sampleWindow];

        // 计算读取起始位置，防止越界
        int startPosition = micPosition - sampleWindow;
        if (startPosition < 0) return 0f;

        micClip.GetData(samples, startPosition);

        // 计算RMS音量（均方根，比直接取最大值更准确）
        float sum = 0f;
        foreach (float sample in samples)
        {
            sum += sample * sample;
        }
        return Mathf.Sqrt(sum / sampleWindow);
    }

    // ===== 对外暴露：给 UI 读取能量值 =====
    public float Energy => energy;
    public float MaxEnergy => 100f;
    // ========================================

    // ===== 能量条 UI 更新 =====
    void UpdateEnergyUI()
    {
        if (energySlider == null) return;

        // 同步进度值（0~1）
        energySlider.value = energy / 100f;

        // 根据能量比例改变填充颜色
        if (energyFillImage != null)
        {
            if (energy > 60f)
                energyFillImage.color = Color.green;
            else if (energy > 20f)
                energyFillImage.color = Color.yellow;
            else
                energyFillImage.color = Color.red;
        }
    }
    // ==========================

    void OnDisable()
    {
        if (!string.IsNullOrEmpty(micDevice) && Microphone.IsRecording(micDevice))
        {
            Microphone.End(micDevice);
        }

        micReady = false;
        micClip = null;
        blowTimer = 0f;
    }
}
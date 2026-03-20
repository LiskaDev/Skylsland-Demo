using UnityEngine;
using TMPro;

public class TeleportPoint : MonoBehaviour
{
    [Header("传送设置")]
    // 这个传送点所在的岛编号（1-4）
    [SerializeField] private int islandIndex = 1;
    
    // 四个目标传送位置（每个岛上的落点）
    [SerializeField] private Transform[] teleportDestinations;
    
    // 玩家必须多近才能使用传送点
    [SerializeField] private float interactDistance = 3f;

    [Header("UI")]
    [SerializeField] private GameObject teleportPanel;
    [SerializeField] private GameObject interactHint;

    [Header("动画")]
    // 水晶旋转速度
    [SerializeField] private float rotateSpeed = 90f;
    // 水晶上下浮动速度
    [SerializeField] private float floatSpeed = 1f;
    // 水晶上下浮动幅度
    [SerializeField] private float floatAmount = 0.3f;

    private Transform player;
    private Rigidbody playerRb;
    private bool isPanelOpen = false;
    private Vector3 startPosition;
    private Transform crystal;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        playerRb = player.GetComponent<Rigidbody>();
        
        // 记录初始位置（用于浮动动画）
        startPosition = transform.position;
        
        // 找到子对象Crystal
        crystal = transform.Find("Crystal");
    }

    void Update()
    {
        // 水晶旋转和浮动动画
        AnimateCrystal();

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactDistance)
        {
            // 显示交互提示
            if (interactHint != null)
                interactHint.SetActive(!isPanelOpen);

            // F键打开/关闭传送面板
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (!isPanelOpen)
                    OpenPanel();
                else
                    ClosePanel();
            }
        }
        else
        {
            if (interactHint != null)
                interactHint.SetActive(false);
            if (isPanelOpen)
                ClosePanel();
        }

        // 面板打开时，数字键选择目标岛屿
        if (isPanelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TeleportTo(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TeleportTo(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TeleportTo(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TeleportTo(3);
        }
    }

    void AnimateCrystal()
    {
        if (crystal == null) return;

        // 旋转
        crystal.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);

        // 上下浮动（用Sin函数产生平滑的上下运动）
        // Time.time = 游戏运行的总时间
        // Sin函数输出-1到1之间的值，乘以幅度得到实际偏移
        float newY = startPosition.y + 
            Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.position = new Vector3(
            startPosition.x, newY, startPosition.z);
    }

    void OpenPanel()
    {
        isPanelOpen = true;
        if (teleportPanel != null)
            teleportPanel.SetActive(true);
    }

    void ClosePanel()
    {
        isPanelOpen = false;
        if (teleportPanel != null)
            teleportPanel.SetActive(false);
    }

    void TeleportTo(int index)
    {
        // 检查目标是否存在
        if (teleportDestinations == null || 
            index >= teleportDestinations.Length ||
            teleportDestinations[index] == null)
        {
            Debug.Log("目标传送点未设置！");
            return;
        }

        // 不能传送到自己所在的岛
        if (index == islandIndex - 1)
        {
            Debug.Log("你已经在这个岛上了！");
            return;
        }

        // 传送玩家
        Vector3 destination = teleportDestinations[index].position;
        
        // 先清除速度，防止传送后继续飞
        playerRb.velocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        
        // 移动玩家到目标位置
        player.position = destination;

        ClosePanel();
        Debug.Log("传送到：Island 0" + (index + 1));
    }
}
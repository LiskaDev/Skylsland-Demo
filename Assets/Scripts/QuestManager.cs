using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;        // 对话面板
    public TextMeshProUGUI villagerNameText; // 村民名字
    public TextMeshProUGUI dialogueText;     // 对话内容
    public Button confirmButton;             // 接受任务按钮

    [Header("Quest Settings")]
    public VillagerBuilder villagerBuilder;  // 村民建造脚本
    public float interactRange = 3f;         // 交互距离

    [Header("Reward")]
    public int rewardBrick = 10;             // 完成奖励砖块数量

    // 内部状态
    private Transform player;
    private bool questAccepted = false;      // 任务是否已接取
    private bool questCompleted = false;     // 任务是否已完成
    private bool panelOpen = false;          // 面板是否打开中

    void Start()
    {
        // 找到玩家
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // 给按钮绑定点击事件
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);

        // 确保面板默认关闭
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        // 如果任务已完成，不再处理
        if (questCompleted) return;

        // 计算玩家和村民的距离
        float distance = Vector3.Distance(transform.position, player.position);

        // 玩家在范围内且按下F键
        if (distance <= interactRange && Input.GetKeyDown(KeyCode.F))
        {
            if (!panelOpen)
                OpenDialogue();
            else
                CloseDialogue();
        }

        // 玩家走远了自动关闭面板
        if (distance > interactRange && panelOpen)
            CloseDialogue();
    }

    void OpenDialogue()
    {
        panelOpen = true;
        dialoguePanel.SetActive(true);

        if (!questAccepted)
        {
            // 还没接任务，显示任务对话
            villagerNameText.text = "Villager";
            dialogueText.text = "Help me build a small hut!";
            confirmButton.gameObject.SetActive(true);
        }
        else
        {
            // 已接任务，显示等待对话
            villagerNameText.text = "Villager";
            dialogueText.text = "I'm working on it, please wait...";
            confirmButton.gameObject.SetActive(false);
        }
    }

    void CloseDialogue()
    {
        panelOpen = false;
        dialoguePanel.SetActive(false);
    }

    void OnConfirmButtonClicked()
    {
        // 点击接受任务按钮
        questAccepted = true;
        CloseDialogue();

        // 让村民开始建造（使用现有VillagerBuilder）
        villagerBuilder.StartBuildingFromQuest();
    }

    // 这个方法由VillagerBuilder建造完成后调用
    public void OnQuestComplete()
    {
        questCompleted = true;

        // 给予奖励
        Inventory.Instance.AddItem("Brick", rewardBrick);

        // 打开完成对话
        dialoguePanel.SetActive(true);
        villagerNameText.text = "Villager";
        dialogueText.text = "Thank you! Here are some bricks as reward!";
        confirmButton.gameObject.SetActive(false);

        // 3秒后自动关闭
        Invoke("CloseDialogue", 3f);
    }
}
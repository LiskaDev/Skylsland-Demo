using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;        // 对话面板
    public TextMeshProUGUI interactHintText; // 新增：按F交互的提示文字
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
    private string originalHintString = "";  // 用于保存原本的提示词

    void Start()
    {
        // 找到玩家
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // 给按钮绑定点击事件
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);

        // 确保面板默认关闭
        dialoguePanel.SetActive(false);

        // 如果用户拖入了UI文字，我们在开始时先偷偷记住这上面原本写的（比如 [E] Build...）内容
        if (interactHintText != null)
        {
            originalHintString = interactHintText.text;
        }
    }

    void Update()
    {
        // 先计算玩家和村民的距离
        float distance = Vector3.Distance(transform.position, player.position);

        // ============ 隐藏彩蛋：任务完成后的“感谢对话” ============
        if (questCompleted)
        {
            if (distance <= interactRange)
            {
                // 即使交了差，靠近时依然把 [F] 混剪进原来的 [E] 提示字里提醒玩家
                if (interactHintText != null)
                {
                    interactHintText.text = "[F] Talk   " + originalHintString;
                    interactHintText.gameObject.SetActive(!panelOpen);
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    panelOpen = true; // 加上防连按保护
                    dialoguePanel.SetActive(true);
                    villagerNameText.text = "Villager";
                    dialogueText.text = "Thanks again! You're the best builder!";
                    
                    if (confirmButton != null) 
                        confirmButton.gameObject.SetActive(false);
                        
                    Invoke("CloseDialogue", 2f); // 2秒后自动关闭
                }
            }
            else
            {
                // 走远后，把属于旧面板的提示文字洗干净交还
                if (interactHintText != null && interactHintText.text != originalHintString)
                {
                    interactHintText.text = originalHintString;
                    interactHintText.gameObject.SetActive(false);
                }
            }
            return; // 结束之后的逻辑
        }

        if (distance <= interactRange)
        {
            if (interactHintText != null)
            {
                // 动态把字换成带有 [F] 交互合并版本
                interactHintText.text = "[F] Talk   " + originalHintString;
                interactHintText.gameObject.SetActive(!panelOpen);
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (!panelOpen) OpenDialogue();
                else CloseDialogue();
            }
        }
        else
        {
            // 超出范围，隐藏前顺手把字换回原来的
            if (interactHintText != null)
            {
                interactHintText.text = originalHintString;
                interactHintText.gameObject.SetActive(false);
            }

            if (panelOpen)
                CloseDialogue();
        }
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
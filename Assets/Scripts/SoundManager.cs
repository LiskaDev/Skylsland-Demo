using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // 单例模式，全场景只有一个音效管理器
    public static SoundManager Instance;

    [Header("音效")]
    [SerializeField] private AudioClip buildSound;      // 放置方块音效
    [SerializeField] private AudioClip collectSound;    // 收集物品音效
    [SerializeField] private AudioClip teleportSound;   // 传送音效
    [SerializeField] private AudioClip planeStartSound; // 飞机启动音效

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // 播放音效的通用函数
    public void PlaySound(string soundName)
    {
        AudioClip clip = null;

        switch (soundName)
        {
            case "Build": clip = buildSound; break;
            case "Collect": clip = collectSound; break;
            case "Teleport": clip = teleportSound; break;
            case "Plane": clip = planeStartSound; break;
        }

        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
}
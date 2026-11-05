using UnityEngine;

public enum InteractType { DialogueOnly, ShopNPC, EnemyNPC }

public class WorldInteractable : MonoBehaviour
{
    [Header("Кто это? (Подсвечивается над головой)")]
    public string titleName = "Captain Angmar";
    public InteractType interactType = InteractType.DialogueOnly;
    public float interactDistance = 3.0f;

    [Header("ID для квестов (уникальный! например: marta_weiss)")]
    public string npcId = "npc_001";

    [Header("Кружок подсветки под ногами (необязательно)")]
    public GameObject highlightRing;

    [Header("ДИАЛОГИ — берётся ПЕРВЫЙ вариант, чьё условие выполнено")]
    public DialogueVariant[] dialogues;

    [Header("Маркеры над головой: '!' (есть дело) и '?' (можно сдать дело)")]
    public QuestData[] offersQuests;
    public QuestData[] turnInQuests;
    public float markerDistance = 25f;

    [Header("Запасной простой диалог (если список диалогов выше пуст или ни одно условие не подошло)")]
    [TextArea(2, 4)]
    public string npcGreetingText = "Новичок? Отлично! Хочешь взглянуть на мои товары или проверим твои силы?";
    public string leftButtonText = "Давай! (Действие)";
    public string rightButtonText = "Я пойду...";

    [Header("Если это МАГАЗИН — какие товары продает?")]
    public ItemData[] shopItems;

    [Header("Если это ВРАГ — настройки для 2D боя")]
    public EnemyDifficulty difficulty = EnemyDifficulty.Medium;
    public int enemyHP = 80;
    public int expReward = 50;
    public int goldReward = 25;
    public Sprite enemyBattleSprite;
    public Sprite customBattleBackground;
    [Tooltip("true: после победы NPC исчезает. false: остаётся (тогда используйте флаг defeated_<npcId> в условиях диалога)")]
    public bool destroyOnDefeat = true;
    [Tooltip("Необязательно: реплика сразу после победы (работает, если destroyOnDefeat = false)")]
    public DialogueData afterWinDialogue;

    [HideInInspector] public bool isPlayerNear = false;
    private Transform player;
    private DialogueData legacyDialogue;

    private static GUIStyle nameStyle, markerStyle;

    void Start()
    {
        var p = FindAnyObjectByType<IsoPlayerController>();
        if (p != null) player = p.transform;
        if (highlightRing != null) highlightRing.SetActive(false);
    }

    void Update()
    {
        if (player == null || !player.GetComponent<IsoPlayerController>().enabled)
        {
            isPlayerNear = false;
            if (highlightRing != null) highlightRing.SetActive(false);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        bool nearNow = (dist <= interactDistance);

        if (nearNow != isPlayerNear)
        {
            isPlayerNear = nearNow;
            if (highlightRing != null) highlightRing.SetActive(isPlayerNear);

            if (isPlayerNear)
                RPGGameManager.Instance.SetNearbyInteractable(this);
            else if (RPGGameManager.Instance.currentInteractable == this)
                RPGGameManager.Instance.SetNearbyInteractable(null);
        }
    }

    // ---------------------------------------------------------------- выбор диалога
    public DialogueData PickDialogue()
    {
        if (dialogues != null)
            foreach (var v in dialogues)
                if (v != null && v.dialogue != null && (v.condition == null || v.condition.IsMet()))
                    return v.dialogue;

        if (legacyDialogue == null) legacyDialogue = BuildLegacyDialogue();
        return legacyDialogue;
    }

    // Собираем простой диалог из старых полей (приветствие + две кнопки)
    private DialogueData BuildLegacyDialogue()
    {
        var d = ScriptableObject.CreateInstance<DialogueData>();
        d.startNodeId = "start";

        var action = DialogueAction.None;
        if (interactType == InteractType.ShopNPC) action = DialogueAction.OpenShop;
        if (interactType == InteractType.EnemyNPC) action = DialogueAction.StartBattle;

        d.nodes = new[]
        {
            new DialogueNode
            {
                id = "start",
                text = npcGreetingText,
                choices = new[]
                {
                    new DialogueChoice { text = leftButtonText, action = action },
                    new DialogueChoice { text = rightButtonText }
                }
            }
        };
        return d;
    }

    // ---------------------------------------------------------------- маркер квеста
    private string GetMarker()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return null;

        if (turnInQuests != null)
            foreach (var q in turnInQuests)
                if (q != null && qm.GetState(q) == QuestState.ReadyToTurnIn) return "?";

        if (offersQuests != null)
            foreach (var q in offersQuests)
                if (q != null && qm.GetState(q) == QuestState.NotStarted) return "!";

        return null;
    }

    // ---------------------------------------------------------------- рисование над головой
    void OnGUI()
    {
        if (Camera.main == null) return;
        if (BattleManager.Instance != null && BattleManager.Instance.battleActive) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return;

        if (nameStyle == null)
        {
            nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            markerStyle = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        // Маркер '!' / '?' виден издалека
        string marker = GetMarker();
        if (marker != null && player != null && Vector3.Distance(transform.position, player.position) <= markerDistance)
        {
            Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3.0f);
            if (sp.z > 0)
            {
                float y = Screen.height - sp.y;
                var rect = new Rect(sp.x - 25, y - 25, 50, 50);
                markerStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), marker, markerStyle);
                markerStyle.normal.textColor = marker == "!" ? UITheme.Gold : UITheme.Good;
                GUI.Label(rect, marker, markerStyle);
            }
        }

        // Имя — только когда игрок рядом
        if (!isPlayerNear) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
        if (screenPos.z > 0)
        {
            float guiY = Screen.height - screenPos.y;
            var r = new Rect(screenPos.x - 150, guiY - 30, 300, 35);

            nameStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), titleName, nameStyle);
            nameStyle.normal.textColor = UITheme.Parchment;
            GUI.Label(r, titleName, nameStyle);
        }
    }
}

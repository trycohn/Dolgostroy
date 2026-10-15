using UnityEngine;

namespace Dolgostroy.Quests
{
    /// <summary>
    /// Выдаёт квест, например NPC в конце диалога.
    /// <see cref="Give"/> вызывается из кода или через UnityEvent (кнопка, триггер, диалог).
    /// </summary>
    public class QuestGiver : MonoBehaviour
    {
        [SerializeField] private QuestData quest;

        public QuestData Quest => quest;

        [ContextMenu("Выдать квест")]
        public void Give()
        {
            var manager = QuestManager.Instance;
            if (manager == null || quest == null)
            {
                Debug.LogWarning($"[Квесты] {name}: на сцене нет QuestManager или не назначен квест.", this);
                return;
            }

            if (!manager.StartQuest(quest))
                Debug.Log($"[Квесты] {name}: квест «{quest.Title}» уже был выдан.", this);
        }
    }
}

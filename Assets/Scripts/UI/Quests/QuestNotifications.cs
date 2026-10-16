using UnityEngine;
using UnityEngine.UI;

namespace Dolgostroy.Quests
{
    /// <summary>
    /// Всплывающие уведомления о квестах: новый, обновлён, выполнен, провален.
    /// Каждое сообщение — копия выключенного шаблона, которая исчезает через <see cref="duration"/> секунд.
    /// Шаблон лежит в контейнере с VerticalLayoutGroup, поэтому сообщения встают столбиком.
    /// </summary>
    public class QuestNotifications : MonoBehaviour
    {
        [Tooltip("Пусто — берётся QuestManager.Instance.")]
        [SerializeField] private QuestManager manager;

        [Tooltip("Выключенный текст, который копируется для каждого сообщения.")]
        [SerializeField] private Text template;

        [Min(0.1f)]
        [SerializeField] private float duration = 3f;

        private void OnEnable()
        {
            if (manager == null)
                manager = QuestManager.Instance;

            if (manager == null || template == null)
            {
                Debug.LogWarning("[Квесты] QuestNotifications: нет QuestManager или не назначен шаблон.", this);
                enabled = false;
                return;
            }

            manager.QuestStarted += OnQuestStarted;
            manager.ObjectiveProgressed += OnObjectiveProgressed;
            manager.QuestCompleted += OnQuestCompleted;
            manager.QuestFailed += OnQuestFailed;
        }

        private void OnDisable()
        {
            if (manager == null)
                return;

            manager.QuestStarted -= OnQuestStarted;
            manager.ObjectiveProgressed -= OnObjectiveProgressed;
            manager.QuestCompleted -= OnQuestCompleted;
            manager.QuestFailed -= OnQuestFailed;
        }

        public void Show(string message)
        {
            var text = Instantiate(template, template.transform.parent);
            text.text = message;
            text.gameObject.SetActive(true);
            Destroy(text.gameObject, duration);
        }

        private void OnQuestStarted(Quest quest) =>
            Show($"Новый квест: «{quest.Data.Title}»");

        private void OnObjectiveProgressed(Quest quest, QuestObjectiveProgress objective)
        {
            var progress = objective.Required > 1 ? $"{objective.Current}/{objective.Required}" : "выполнено";
            Show($"«{quest.Data.Title}»: {objective.Data.Description} — {progress}");
        }

        private void OnQuestCompleted(Quest quest) =>
            Show($"Квест выполнен: «{quest.Data.Title}»");

        private void OnQuestFailed(Quest quest) =>
            Show($"Квест провален: «{quest.Data.Title}»");
    }
}

namespace Dolgostroy.Quests
{
    /// <summary>Тип игрового события, которое продвигает цель квеста.</summary>
    /// <remarks>Новые значения добавлять только в конец: ассеты хранят их числом.</remarks>
    public enum QuestObjectiveType
    {
        Talk,    // поговорить с NPC
        Reach,   // дойти до места
        Defeat,  // победить в бою
        Collect, // получить предмет
        Choose,  // выбрать вариант в диалоге
        Custom   // любое другое событие
    }
}

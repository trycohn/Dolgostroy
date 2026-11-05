#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Меню RPG → Sample: создаёт готовую цепочку из 3 дел и диалоги к ней,
/// а потом одним кликом настраивает выбранный в сцене NPC.
/// Положите этот файл в любую папку с именем Editor.
/// </summary>
public static class SampleContentCreator
{
    const string Dir = "Assets/RPGSample";

    // ------------------------------------------------------------------ 1. создать ассеты
    [MenuItem("RPG/Sample/1. Создать пример (квесты + диалоги)")]
    public static void Create()
    {
        if (AssetDatabase.IsValidFolder(Dir))
        {
            Debug.LogWarning("Папка " + Dir + " уже существует — удалите её, если хотите создать пример заново.");
            return;
        }
        AssetDatabase.CreateFolder("Assets", "RPGSample");

        // ---- Цепочка дел (создаём с конца, чтобы ссылаться на nextQuest)
        var q3 = Quest("Q3_Closing", "Закрытие реестра",
            "Реестр почти закрыт. Занеси итоговую бумагу Ивану в архив — это последний штрих.",
            "Иван принял реестр. Агентство «Алая печать» снова в плюсе.",
            100, 100, false, null,
            Obj(ObjectiveType.TalkTo, "clerk_ivan", "Передать итоговую бумагу Ивану"));

        var q2 = Quest("Q2_Viktor", "Должник покрупнее",
            "Виктор Крейн задолжал агентству втрое больше Грегора. Он не откроет дверь по-хорошему.",
            "Крейн расплатился. Пятно на репутации агентства смыто.",
            150, 200, true, q3,
            Obj(ObjectiveType.Defeat, "debtor_viktor", "Взыскать долг с Виктора Крейна"));

        var q1 = Quest("Q1_Gregor", "Первое дело",
            "Грегор Холлоуэй просрочил платёж на полгода. Узнай у Ивана в архиве, где он прячется, и потребуй оплату.",
            "Грегор расплатился. Первое дело в твоём портфолио.",
            100, 150, true, q2,
            Obj(ObjectiveType.TalkTo, "clerk_ivan", "Расспросить Ивана в архиве"),
            Obj(ObjectiveType.Defeat, "debtor_gregor", "Взыскать долг с Грегора"));

        // ---- Диалоги Марты (заказчик)
        D("Marta_Q1_Offer",
            N("start", "{player}, у нас просрочка. Клиент — Грегор Холлоуэй, полгода молчания. Сначала поговори с Иваном в архиве: он знает, где Грегор прячется. Потом — вежливое взыскание. Или не очень вежливое.", "",
                C("Беру это дело.", "ok", DialogueAction.StartQuest, q1),
                C("Мне нужно подготовиться.")),
            N("ok", "Хорошо. И помни: мы не бандиты. Мы — кредиторы с острыми зубами.", "",
                C("Понял.")));

        D("Marta_Q1_Active", N("start", "Ещё не закончил? Иван сидит в архиве, а Грегор должен быть где-то рядом.", "", C("Уже иду.")));

        D("Marta_Q1_Ready",
            N("start", "Слышала, Грегор расплатился. Чисто сработано. Сдавай дело — вот твоя доля.", "",
                C("Сдать дело.", "next", DialogueAction.CompleteQuest, q1)),
            N("next", "Следующее уже ждёт: Виктор Крейн, должник покрупнее. Детали — в твоей папке дел.", "",
                C("Принято.")));

        D("Marta_Q2_Active", N("start", "Виктор Крейн всё ещё на свободе. Не заставляй меня краснеть перед клиентом.", "", C("Разберусь.")));

        D("Marta_Q2_Ready",
            N("start", "Крейн закрыл долг! Отличная работа. Сдавай дело.", "",
                C("Сдать дело.", "next", DialogueAction.CompleteQuest, q2)),
            N("next", "Остался последний штрих: занеси итоговый реестр Ивану. Он давно ждёт.", "",
                C("Сейчас схожу.")));

        D("Marta_Idle", N("start", "Новых дел пока нет. Загляни позже, {player}.", "", C("Хорошо.")));

        // ---- Иван (информатор)
        D("Ivan_Q1", N("start", "Грегор? Он у склада, всегда в одно и то же время. Только не рассчитывай на мирный исход.", "", C("Спасибо.")));
        D("Ivan_Q3", N("start", "О, итоговый реестр! Наконец-то порядок. Держи, это за труды.", "", C("Рад помочь.")));

        // ---- Должники (бой начинается кнопкой в диалоге)
        D("Gregor_Talk",
            N("start", "Опять вы?! Я же сказал: денег нет!", "",
                C("Платить пора. (Бой)", "", DialogueAction.StartBattle),
                C("Зайду позже.")));

        D("Viktor_Talk",
            N("start", "Думаешь, напугаешь меня своей бумажкой?", "",
                C("Бумажка — только начало. (Бой)", "", DialogueAction.StartBattle),
                C("Пожалуй, не сейчас.")));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Пример создан в " + Dir + ". Теперь выделите NPC в сцене и выберите RPG → Sample → 2...");
    }

    // ------------------------------------------------------------------ 2. настроить выбранного NPC
    [MenuItem("RPG/Sample/2a. Настроить выбранного: Марта (заказчик)")]
    static void ApplyMarta()
    {
        var npc = Selected(); if (npc == null) return;
        var q1 = L<QuestData>("Q1_Gregor"); var q2 = L<QuestData>("Q2_Viktor");

        npc.npcId = "marta_weiss"; npc.titleName = "Марта Вайс";
        npc.interactType = InteractType.DialogueOnly;
        npc.offersQuests = new[] { q1 };
        npc.turnInQuests = new[] { q1, q2 };
        npc.dialogues = new[]
        {
            V(q1, QuestState.ReadyToTurnIn, "Marta_Q1_Ready"),
            V(q1, QuestState.Active,        "Marta_Q1_Active"),
            V(q1, QuestState.NotStarted,    "Marta_Q1_Offer"),
            V(q2, QuestState.ReadyToTurnIn, "Marta_Q2_Ready"),
            V(q2, QuestState.Active,        "Marta_Q2_Active"),
            V(null, QuestState.Active,      "Marta_Idle")   // без условия — запасной вариант
        };
        Done(npc);
    }

    [MenuItem("RPG/Sample/2b. Настроить выбранного: Иван (архив)")]
    static void ApplyIvan()
    {
        var npc = Selected(); if (npc == null) return;
        npc.npcId = "clerk_ivan"; npc.titleName = "Иван, архивариус";
        npc.interactType = InteractType.DialogueOnly;
        npc.dialogues = new[]
        {
            V(L<QuestData>("Q3_Closing"), QuestState.Active, "Ivan_Q3"),
            V(L<QuestData>("Q1_Gregor"),  QuestState.Active, "Ivan_Q1")
            // если ни одно условие не подошло — сработает запасной диалог из полей npcGreetingText
        };
        npc.npcGreetingText = "Бумаги, бумаги... Если нужны сведения о должниках — заходи, когда будет дело.";
        npc.leftButtonText = "Понятно.";
        npc.rightButtonText = "Я пойду...";
        Done(npc);
    }

    [MenuItem("RPG/Sample/2c. Настроить выбранного: Грегор (враг)")]
    static void ApplyGregor() { ApplyEnemy("debtor_gregor", "Грегор Холлоуэй", "Gregor_Talk", EnemyDifficulty.Easy, 60); }

    [MenuItem("RPG/Sample/2d. Настроить выбранного: Виктор (враг)")]
    static void ApplyViktor() { ApplyEnemy("debtor_viktor", "Виктор Крейн", "Viktor_Talk", EnemyDifficulty.Medium, 110); }

    static void ApplyEnemy(string id, string title, string dlg, EnemyDifficulty diff, int hp)
    {
        var npc = Selected(); if (npc == null) return;
        npc.npcId = id; npc.titleName = title;
        npc.interactType = InteractType.EnemyNPC;
        npc.difficulty = diff; npc.enemyHP = hp;
        npc.dialogues = new[] { V(null, QuestState.Active, dlg) };
        Done(npc);
    }

    // ------------------------------------------------------------------ помощники
    static WorldInteractable Selected()
    {
        var go = Selection.activeGameObject;
        var npc = go != null ? go.GetComponent<WorldInteractable>() : null;
        if (npc == null) { Debug.LogWarning("Выделите в сцене объект с компонентом WorldInteractable."); return null; }
        if (!AssetDatabase.IsValidFolder(Dir)) { Debug.LogWarning("Сначала выполните RPG → Sample → 1."); return null; }
        Undo.RecordObject(npc, "Apply sample NPC");
        return npc;
    }

    static void Done(WorldInteractable npc) { EditorUtility.SetDirty(npc); Debug.Log("NPC настроен: " + npc.titleName); }

    static T L<T>(string file) where T : Object { return AssetDatabase.LoadAssetAtPath<T>(Dir + "/" + file + ".asset"); }

    static DialogueVariant V(QuestData q, QuestState s, string dialogueFile)
    {
        return new DialogueVariant
        {
            condition = new DialogueCondition { quest = q, state = s },
            dialogue = L<DialogueData>(dialogueFile)
        };
    }

    static QuestObjective Obj(ObjectiveType t, string target, string desc)
    {
        return new QuestObjective { type = t, targetId = target, description = desc, requiredCount = 1 };
    }

    static QuestData Quest(string file, string title, string desc, string done, int xp, int gold, bool turnIn, QuestData next, params QuestObjective[] objs)
    {
        var q = ScriptableObject.CreateInstance<QuestData>();
        q.questId = file; q.title = title; q.description = desc; q.completionText = done;
        q.rewardXP = xp; q.rewardGold = gold; q.needsTurnIn = turnIn;
        q.nextQuest = next; q.autoStartNext = true; q.objectives = objs;
        AssetDatabase.CreateAsset(q, Dir + "/" + file + ".asset");
        return q;
    }

    static DialogueChoice C(string text, string next = "", DialogueAction a = DialogueAction.None, QuestData q = null)
    {
        return new DialogueChoice { text = text, nextNodeId = next, action = a, quest = q };
    }

    static DialogueNode N(string id, string text, string next, params DialogueChoice[] choices)
    {
        return new DialogueNode { id = id, text = text, nextNodeId = next, choices = choices };
    }

    static DialogueData D(string file, params DialogueNode[] nodes)
    {
        var d = ScriptableObject.CreateInstance<DialogueData>();
        d.nodes = nodes; d.startNodeId = nodes[0].id;
        AssetDatabase.CreateAsset(d, Dir + "/" + file + ".asset");
        return d;
    }
}
#endif

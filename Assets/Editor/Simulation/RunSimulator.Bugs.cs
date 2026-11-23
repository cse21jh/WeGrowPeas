using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 시뮬레이터의 벌레. 등장, 움직임, 보상은 게임과 같은 코드이고 충돌과 클릭만 여기서 흉내 낸다.
public sealed partial class RunSimulator : IBugTargets
{
    private const float DaySeconds = 50f;      // BreedingController의 기본 교배 시간
    private const float StepSeconds = 1f / 30f;
    private const float ClickInterval = 0.4f;  // 잇달아 잡을 때의 간격
    private const float HitDelay = 0.1f;       // Bug.HitBug: 맞고 죽기까지

    // ───── 프리팹에서 읽는 값 ─────

    private struct BugInfo
    {
        public BugMove move;
        public float speed, radius;
        public bool revives, stopsToEat;
    }

    private static List<BugInfo> bugInfos;
    private static BugInfo ladybugInfo;
    private static Vector2[] cellPositions;
    private static readonly Dictionary<PlantKind, Rect> plantBoxes = new Dictionary<PlantKind, Rect>();
    private static float pheromoneRadius;

    private static void LoadBugs()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resource/Prefabs/GardenGrid.prefab");
        var grid = new SerializedObject(root.GetComponent<Grid>());

        bugInfos = new List<BugInfo>();
        SerializedProperty list = grid.FindProperty("bugPrefabs");
        for (int i = 0; i < list.arraySize; i++) bugInfos.Add(ReadBug((GameObject)list.GetArrayElementAtIndex(i).objectReferenceValue));
        ladybugInfo = ReadBug((GameObject)grid.FindProperty("ladybugPrefabs").objectReferenceValue);

        cellPositions = new Vector2[8 * PlantBoard.RowCount];
        for (int cell = 0; cell < cellPositions.Length; cell++)
            cellPositions[cell] = root.transform.GetChild(cell / PlantBoard.RowCount).GetChild(cell % PlantBoard.RowCount).position;

        plantBoxes.Clear();
        void AddBox(PlantKind kind, string field)
        {
            var prefab = (GameObject)grid.FindProperty(field).objectReferenceValue;
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            if (box == null) return; // 충돌체가 없으면 벌레가 닿지 않는다
            Vector2 scale = prefab.transform.localScale;
            Vector2 size = Vector2.Scale(box.size, scale);
            plantBoxes[kind] = new Rect(Vector2.Scale(box.center, scale) - size * 0.5f, size);
        }
        AddBox(PlantKind.Pea, "peaPrefab");
        AddBox(PlantKind.Peanut, "peanutPrefab");
        AddBox(PlantKind.Nepenthes, "nepenthesPrefab");
        AddBox(PlantKind.ChiliPepper, "chiliPepperPrefab");
        AddBox(PlantKind.Sprinkler, "sprinklerPrefab");
        AddBox(PlantKind.MoneyTree, "moneyTreePrefab");

        var nepenthes = (GameObject)grid.FindProperty("nepenthesPrefab").objectReferenceValue;
        float size2 = new SerializedObject(nepenthes.GetComponent<Nepenthes>()).FindProperty("pheromoneSize").floatValue;
        pheromoneRadius = nepenthes.GetComponentInChildren<NepenthesPheromone>(true).GetComponent<SphereCollider>().radius * size2 * nepenthes.transform.localScale.x;
    }

    private static BugInfo ReadBug(GameObject prefab)
    {
        Bug bug = prefab.GetComponent<Bug>();
        var data = new SerializedObject(bug);
        return new BugInfo
        {
            move = bug is StraightMovingBug ? BugMove.Straight : bug is RandomMovingBug || bug is Ladybug ? BugMove.Wander : BugMove.Chase,
            speed = data.FindProperty("speed").floatValue,
            radius = prefab.GetComponent<CapsuleCollider>().radius * prefab.transform.localScale.x,
            revives = bug is ReviveBug,
            stopsToEat = !(bug is UnstoppableBug),
        };
    }

    // ───── 하루 ─────

    private sealed class SimBug : IBug
    {
        public RunSimulator sim;
        public BugInfo info;
        public BugMotion motion;
        public float warning = BugRules.WarningSeconds; // 움직이기 시작할 때까지
        public float reaction;                          // 움직이기 시작하고 클릭할 때까지
        public float stun;                              // 맞아서 멈춘 시간
        public float dyingIn = -1f;                     // 0 이상이면 맞아서 곧 죽는다
        public bool armored, dead, isLadybug;
        public readonly HashSet<PlantState> touching = new HashSet<PlantState>();
        public readonly HashSet<PlantState> smelling = new HashSet<PlantState>();

        GridModifiers IBug.Modifiers => sim.field.Modifiers;
        void IBug.AddGold(int amount) => sim.economy.Add(amount);
        void IBug.AddPestResistance(float value) => sim.field.AddPestResistance(value);
        bool IBug.EatPlant(PlantState plant) => sim.EatPlant(this, plant);
        void IBug.StartEating() => motion.Eat();
        void IBug.Die() => sim.KillBug(this);
        void IBug.OnKilled() => sim.events.RaiseBugKilled();

        void IBug.OnPlantEaten()
        {
            sim.result.PlantsEaten++;
            sim.events.RaisePlantDiedByBug();
        }
    }

    private readonly BugSpawnState bugSpawn = new BugSpawnState();
    private readonly List<SimBug> bugs = new List<SimBug>();
    private int bonusDaySeconds;
    private float clickCooldown;

    // 낮 동안 벌레가 나오고, 움직이고, 식물을 먹고, 잡힌다
    private void BugPhase()
    {
        if (!config.bugs) return;

        bugSpawn.BeginDay(stage, mods.GetMul(StatId.BugSpawnIntervalMul, -1, stage));
        float seconds = (DaySeconds + bonusDaySeconds) * mods.GetMul(StatId.BreedingPhaseDurationMul, -1, stage);
        for (float time = 0f; time < seconds && !IsWipedOut; time += StepSeconds)
        {
            for (int count = bugSpawn.Tick(StepSeconds, field.Modifiers, field.Board.States.Count > 0); count > 0; count--) SpawnBug();
            if (bugs.Count > 0) StepBugs();
        }
    }

    private void SpawnBug()
    {
        if (stage < BugSchedule.AppearStage) return;
        int kindIndex = BugSpawnState.RollKind(field.Modifiers, field.Board.LadybugCount, stage, bugInfos.Count);
        if (kindIndex == BugSpawnState.Ladybug)
        {
            var ladybug = new SimBug
            {
                sim = this,
                info = ladybugInfo,
                motion = new BugMotion(ladybugInfo.move, GetBugSpeed(ladybugInfo), true),
                isLadybug = true,
                warning = 0f,
            };
            ladybug.motion.PlaceAtRandomEdge();
            ladybug.motion.Begin();
            bugs.Add(ladybug);
            field.Board.LadybugCount++;
            return;
        }

        BugInfo info = bugInfos[kindIndex];
        var bug = new SimBug
        {
            sim = this,
            info = info,
            motion = new BugMotion(info.move, GetBugSpeed(info), info.stopsToEat),
            armored = info.revives,
            reaction = config.bugReaction * (0.5f + botRng.Range(0, 1001) * 0.001f),
        };
        bug.motion.PlaceAtRandomEdge();
        bugs.Add(bug);
    }

    private float GetBugSpeed(BugInfo info)
        => BugRules.GetSpeed(info.speed, field.Modifiers, mods.GetMul(StatId.BugSpeedMul, -1, stage));

    private void StepBugs()
    {
        clickCooldown -= StepSeconds;
        for (int i = 0; i < bugs.Count; i++)
        {
            SimBug bug = bugs[i];
            if (bug.dead) continue;
            if (bug.isLadybug)
            {
                bug.motion.Step(StepSeconds, true, this);
                Catch(bug);
                continue;
            }
            if (bug.warning > 0f)
            {
                bug.warning -= StepSeconds;
                if (bug.warning <= 0f) bug.motion.Begin();
                continue;
            }

            if (bug.dyingIn >= 0f)
            {
                bug.dyingIn -= StepSeconds;
                if (bug.dyingIn < 0f) KillBug(bug);
            }
            if (bug.stun > 0f) bug.stun -= StepSeconds;
            if (!bug.dead && !bug.motion.Step(StepSeconds, bug.dyingIn < 0f && bug.stun <= 0f, this)) bug.dead = true;
            if (!bug.dead) Touch(bug);
            if (!bug.dead) TryClick(bug);
        }
        bugs.RemoveAll(b => b.dead);
    }

    // Ladybug.OnTriggerEnter: 벌레와 부딪히면 함께 죽는다
    private void Catch(SimBug ladybug)
    {
        foreach (SimBug bug in bugs)
        {
            if (bug.isLadybug || bug.dead) continue;
            float range = ladybug.info.radius + bug.info.radius;
            if ((bug.motion.Position - ladybug.motion.Position).sqrMagnitude > range * range) continue;
            KillBug(bug);
            KillBug(ladybug);
            return;
        }
    }

    // Bug.OnTriggerEnter와 NepenthesPheromone.OnTriggerEnter: 닿기 시작할 때 한 번
    private void Touch(SimBug bug)
    {
        Vector2 p = bug.motion.Position;
        for (int cell = 0; cell < CellCountForBugs; cell++)
        {
            if (!field.Board.States.TryGetValue(cell, out PlantState plant)) continue;
            Vector2 center = cellPositions[cell];

            if (plant.Kind == PlantKind.Nepenthes && field.Modifiers.HasNepenthesPheromone)
            {
                float range = pheromoneRadius * field.Modifiers.GetEffectiveNepenthesPheromoneSizeMultiplier() + bug.info.radius;
                if ((p - center).sqrMagnitude > range * range) bug.smelling.Remove(plant);
                else if (bug.smelling.Add(plant)) bug.motion.LureTo(cell);
            }

            if (!plantBoxes.TryGetValue(plant.Kind, out Rect box)) continue;
            float dx = Mathf.Max(0f, Mathf.Abs(p.x - center.x - box.center.x) - box.width * 0.5f);
            float dy = Mathf.Max(0f, Mathf.Abs(p.y - center.y - box.center.y) - box.height * 0.5f);
            if (dx * dx + dy * dy > bug.info.radius * bug.info.radius) { bug.touching.Remove(plant); continue; }
            if (!bug.touching.Add(plant)) continue;

            BugRules.Touch(bug, plant);
            if (bug.dead) return;
        }
    }

    private bool EatPlant(SimBug bug, PlantState plant)
    {
        DeathOutcome outcome = field.TryKill(plant, DeathCause.Bug, field.Board.LadybugCount != 0);
        if (outcome.Result == DeathResult.BlockedByLadybug)
        {
            // 무당벌레가 곧바로 날아와 벌레와 함께 죽는다
            KillBug(bug);
            KillBug(bugs.Find(b => b.isLadybug && !b.dead));
        }
        economy.Add(outcome.BonusGold);
        return outcome.Died;
    }

    private static int CellCountForBugs => cellPositions.Length;

    private void TryClick(SimBug bug)
    {
        if (bug.dyingIn >= 0f || bug.stun > 0f) return;
        bug.reaction -= StepSeconds;
        if (bug.reaction > 0f || clickCooldown > 0f || LetsBugsEat) return;

        clickCooldown = ClickInterval;
        if (bug.armored)
        {
            bug.armored = false;
            bug.stun = HitDelay + BugRules.ReviveSeconds;
            bug.reaction = config.bugReaction * 0.3f;
        }
        else bug.dyingIn = HitDelay;
    }

    // 벌레에게 먹히는 의뢰가 있고 식물이 넉넉하면 내버려 둔다
    private bool LetsBugsEat
        => IsPlanner && MaxRemaining<FeedBugRequest>() > 0 && field.Board.CountLivingPlants() > Bot.feedBugPlants;

    private void KillBug(SimBug bug)
    {
        if (bug.dead) return;
        bug.dead = true;
        if (bug.isLadybug) field.Board.LadybugCount--;
        result.BugsKilled++;
        BugRules.Reward(bug, BugRules.BaseGold);
    }

    // ───── IBugTargets ─────

    int IBugTargets.CellCount => field.Board.CellCount;

    bool IBugTargets.TryGetPlant(int cell, out Vector2 position, out bool isNepenthes)
    {
        bool found = field.Board.States.TryGetValue(cell, out PlantState plant);
        position = found ? cellPositions[cell] : Vector2.zero;
        isNepenthes = found && plant.Kind == PlantKind.Nepenthes;
        return found;
    }
}

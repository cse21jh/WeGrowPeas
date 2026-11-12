using System.Collections.Generic;
using UnityEngine;

namespace WeGrowPeas.PeaMerge
{
    public sealed class PeaMergeGame : MonoBehaviour
    {
        public enum GameState { Playing, Paused, GameOver }

        [SerializeField] private PeaMergeSettings settings;
        [SerializeField] private PeaMergeUI ui;
        [SerializeField] private Transform piecesRoot;
        [SerializeField] private LineRenderer aimLine;
        private readonly List<PeaMergePiece> pieces = new();
        private readonly Queue<(PeaMergePiece a, PeaMergePiece b)> merges = new();
        private PeaMergePiece preview;
        private float nextDropAt;
        private float aimX;
        private int nextTier;
        private int bestScore;
        private float noticeUntil;

        public PeaMergeSettings Settings => settings;
        public GameState State { get; private set; }
        public int Score { get; private set; }
        public int BestScore => bestScore;
        public int MergeCount { get; private set; }
        public float GameClock { get; private set; }
        public IReadOnlyList<PeaMergePiece> Pieces => pieces;
        public PeaMergePiece Preview => preview;
        public bool CanDrop => State == GameState.Playing && preview != null && GameClock >= nextDropAt;

        public void Configure(PeaMergeSettings config, PeaMergeUI view, Transform root, LineRenderer guide)
        {
            settings = config;
            ui = view;
            piecesRoot = root;
            aimLine = guide;
        }

        private void Start()
        {
            if (settings == null || settings.peaPrefab == null || settings.tiers == null || settings.tiers.Length == 0)
            {
                Debug.LogError("Pea Merge needs a prefab and at least one tier.", this);
                enabled = false;
                return;
            }
            bestScore = PlayerPrefs.GetInt(settings.bestScoreKey, 0);
            ui.Bind(this);
            Restart();
        }

        public void Restart()
        {
            SaveBest();
            merges.Clear();
            foreach (var piece in pieces) if (piece != null) piece.Retire();
            pieces.Clear();
            if (preview != null) preview.Retire();
            preview = null;
            Score = 0;
            MergeCount = 0;
            GameClock = 0f;
            nextDropAt = 0f;
            noticeUntil = 0f;
            aimX = 0f;
            State = GameState.Playing;
            nextTier = settings.RandomTier();
            SpawnPreview();
            ui.Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (State != GameState.Playing) return;
            GameClock += Time.deltaTime;
            if (preview == null && GameClock >= nextDropAt) SpawnPreview();
            CheckOverflow(Time.deltaTime);
            UpdateAimLine();
            if (noticeUntil > 0f && GameClock > noticeUntil)
            {
                noticeUntil = 0f;
                ui.SetHint("좌우로 움직인 뒤 손을 떼면 떨어집니다");
            }
        }

        private void FixedUpdate()
        {
            if (State != GameState.Playing) return;
            // Reserve immediately in callbacks, then mutate bodies before the next physics step.
            while (merges.Count > 0)
            {
                var pair = merges.Dequeue();
                if (pair.a == null || pair.b == null) continue;
                int tier = pair.a.TierIndex;
                Vector2 position = (pair.a.Body.position + pair.b.Body.position) * 0.5f;
                Vector2 velocity = (pair.a.Body.linearVelocity + pair.b.Body.linearVelocity) * 0.5f;
                pieces.Remove(pair.a);
                pieces.Remove(pair.b);
                pair.a.Retire();
                pair.b.Retire();
                if (tier + 1 < settings.tiers.Length)
                {
                    float radius = settings.tiers[tier + 1].radius;
                    position.x = Mathf.Clamp(position.x, -settings.halfWidth + radius, settings.halfWidth - radius);
                    position.y = Mathf.Max(settings.floorY + radius, position.y);
                    var merged = SpawnPiece(tier + 1, position);
                    merged.Drop(true);
                    merged.Body.linearVelocity = Vector2.ClampMagnitude(velocity, 3f);
                    pieces.Add(merged);
                    if (tier + 2 == settings.tiers.Length) ShowNotice("최종 완두콩 완성! 두 개를 합치면 보너스 점수!");
                }
                else ShowNotice("최종 완두콩 합체! 보너스 점수 획득!");
                Score += settings.tiers[tier].mergeScore;
                MergeCount++;
                bestScore = Mathf.Max(bestScore, Score);
                ui.RefreshScore();
            }
        }

        public bool TryMerge(PeaMergePiece a, PeaMergePiece b)
        {
            if (State != GameState.Playing || a == null || b == null || a == b ||
                a.Owner != this || b.Owner != this || !a.IsDropped || !b.IsDropped ||
                a.IsReserved || b.IsReserved || a.TierIndex != b.TierIndex ||
                GameClock - a.BornAt < 0.12f || GameClock - b.BornAt < 0.12f) return false;
            a.Reserve();
            b.Reserve();
            merges.Enqueue((a, b));
            return true;
        }

        private PeaMergePiece SpawnPiece(int tier, Vector2 position)
        {
            var instance = Instantiate(settings.peaPrefab, position, Quaternion.identity, piecesRoot);
            var piece = instance.AddComponent<PeaMergePiece>();
            piece.Initialize(this, tier);
            return piece;
        }

        private void SpawnPreview()
        {
            preview = SpawnPiece(nextTier, new Vector2(aimX, settings.dropY));
            nextTier = settings.RandomTier();
            Aim(aimX);
            ui.SetNext(settings.tiers[nextTier], nextTier);
        }

        public void Aim(float worldX)
        {
            if (State != GameState.Playing) return;
            float radius = preview != null ? preview.Radius : settings.tiers[0].radius;
            aimX = Mathf.Clamp(worldX, -settings.halfWidth + radius + 0.04f, settings.halfWidth - radius - 0.04f);
            if (preview != null) preview.transform.position = new Vector3(aimX, settings.dropY, 0f);
            UpdateAimLine();
        }

        public bool Drop()
        {
            if (!CanDrop) return false;
            preview.Drop();
            pieces.Add(preview);
            preview = null;
            nextDropAt = GameClock + settings.dropCooldown;
            UpdateAimLine();
            return true;
        }

        private void UpdateAimLine()
        {
            if (aimLine == null) return;
            aimLine.enabled = CanDrop;
            if (!aimLine.enabled) return;
            aimLine.SetPosition(0, new Vector3(aimX, settings.dropY - preview.Radius, 0f));
            aimLine.SetPosition(1, new Vector3(aimX, settings.floorY, 0f));
        }

        private void CheckOverflow(float delta)
        {
            float worst = 0f;
            foreach (var piece in pieces)
            {
                if (piece == null || piece.IsReserved) continue;
                bool over = piece.HasContact && GameClock - piece.BornAt >= settings.spawnGraceSeconds && piece.Top > settings.dangerY;
                piece.OverflowTime = over ? piece.OverflowTime + delta : 0f;
                worst = Mathf.Max(worst, piece.OverflowTime);
                if (piece.Body.position.y < settings.floorY - 3f) worst = settings.overflowSeconds;
            }
            ui.SetDanger(worst / settings.overflowSeconds);
            if (worst < settings.overflowSeconds) return;
            State = GameState.GameOver;
            FreezePieces(true);
            if (preview != null) preview.gameObject.SetActive(false);
            UpdateAimLine();
            SaveBest();
            ui.Refresh();
        }

        public void TogglePause()
        {
            if (State == GameState.GameOver) return;
            State = State == GameState.Paused ? GameState.Playing : GameState.Paused;
            FreezePieces(State == GameState.Paused);
            UpdateAimLine();
            ui.Refresh();
        }

        private void FreezePieces(bool freeze)
        {
            foreach (var piece in pieces) if (piece != null) piece.Freeze(freeze);
        }

        private void ShowNotice(string message)
        {
            noticeUntil = GameClock + 3f;
            ui.SetHint(message);
        }

        private void SaveBest()
        {
            if (settings == null) return;
            bestScore = Mathf.Max(bestScore, Score);
            // Do not touch the main game's profiles or save files.
            if (bestScore <= PlayerPrefs.GetInt(settings.bestScoreKey, 0)) return;
            PlayerPrefs.SetInt(settings.bestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            SaveBest();
            if (State == GameState.Playing) TogglePause();
        }

        private void OnDisable() => SaveBest();
    }
}

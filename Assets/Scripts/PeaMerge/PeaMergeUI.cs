using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeGrowPeas.PeaMerge
{
    public sealed class PeaMergeUI : MonoBehaviour
    {
        public TMP_Text scoreText;
        public TMP_Text bestText;
        public TMP_Text nextText;
        public TMP_Text hintText;
        public TMP_Text dangerText;
        public TMP_Text modalTitle;
        public TMP_Text modalScore;
        public GameObject modal;
        public Button pauseButton;
        public Button resumeButton;
        public Button restartButton;
        public Image nextBody;
        public Image nextAccessory;
        public Image dangerLine;
        private PeaMergeGame game;

        public void Bind(PeaMergeGame owner)
        {
            game = owner;
            pauseButton.onClick.AddListener(game.TogglePause);
            resumeButton.onClick.AddListener(game.TogglePause);
            restartButton.onClick.AddListener(game.Restart);
        }

        public void Refresh()
        {
            RefreshScore();
            bool paused = game.State == PeaMergeGame.GameState.Paused;
            bool ended = game.State == PeaMergeGame.GameState.GameOver;
            modal.SetActive(paused || ended);
            modalTitle.text = ended ? "게임 종료" : "일시정지";
            modalScore.text = $"점수 {game.Score:N0}   /   최고 {game.BestScore:N0}";
            resumeButton.gameObject.SetActive(paused);
            pauseButton.interactable = !ended;
            if (!paused && !ended)
            {
                SetHint("좌우로 움직인 뒤 손을 떼면 떨어집니다");
                SetDanger(0f);
            }
        }

        public void RefreshScore()
        {
            scoreText.text = game.Score.ToString("N0");
            bestText.text = game.BestScore.ToString("N0");
        }

        public void SetNext(PeaMergeSettings.Tier tier, int index)
        {
            nextText.text = $"다음 · {index + 1}단계";
            nextBody.sprite = tier.bodyIcon;
            nextAccessory.sprite = tier.accessoryIcon;
            nextAccessory.color = tier.accessoryTint;
            nextAccessory.enabled = tier.accessoryIcon != null;
        }

        public void SetHint(string message) => hintText.text = message;

        public void SetDanger(float amount)
        {
            bool danger = amount > 0f;
            dangerLine.color = danger ? new Color(0.9f, 0.24f, 0.2f) : new Color(0.66f, 0.53f, 0.48f, 0.65f);
            dangerText.text = danger ? $"넘치기 전에 합쳐주세요! {Mathf.Max(0f, game.Settings.overflowSeconds * (1f - amount)):0.0}" : "이 선을 넘겨 쌓이면 게임 종료";
            dangerText.color = danger ? new Color(0.8f, 0.18f, 0.16f) : new Color(0.48f, 0.49f, 0.46f);
        }

        private void OnDestroy()
        {
            if (game == null) return;
            pauseButton.onClick.RemoveListener(game.TogglePause);
            resumeButton.onClick.RemoveListener(game.TogglePause);
            restartButton.onClick.RemoveListener(game.Restart);
        }
    }
}

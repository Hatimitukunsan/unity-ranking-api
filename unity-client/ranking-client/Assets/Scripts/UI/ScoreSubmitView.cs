using System;
using Hakusa.RankingClient.Api;
using UnityEngine;
using UnityEngine.UI;

namespace Hakusa.RankingClient.UI
{
    // スコア送信エリアの表示と入力取得を担当するView
    // API通信は行わず、Controllerに「送信ボタンが押された」ことだけを知らせる
    public sealed class ScoreSubmitView : MonoBehaviour
    {
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField scoreInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private Text statusText;

        public event Action SubmitRequested;

        private void OnEnable()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(NotifySubmitRequested);
            }
        }

        private void OnDisable()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(NotifySubmitRequested);
            }
        }

        public bool TryCreateRequest(out ScoreCreateRequest requestBody)
        {
            requestBody = null;

            // ユーザー名は空文字を許可しない
            string username = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                SetMessage("ユーザー名を入力してください");
                return false;
            }

            // スコアは整数だけを受け付ける
            // 詳細な範囲チェックはAPI側でも行う想定
            string scoreText = scoreInput != null ? scoreInput.text.Trim() : string.Empty;
            if (!int.TryParse(scoreText, out int score))
            {
                SetMessage("スコアには整数を入力してください");
                return false;
            }

            requestBody = new ScoreCreateRequest(username, score);
            return true;
        }

        public void ShowReady()
        {
            // 初期表示や通信完了後の待機状態
            SetMessage("送信できます");
        }

        public void ShowSubmitting()
        {
            // POST /scores を待っている状態
            SetMessage("スコア送信中...");
        }

        public void ShowRankLoading()
        {
            // POST成功後に、自分の順位を取得している状態
            SetMessage("順位を取得中...");
        }

        public void ShowSubmittedScore(ScoreResponse postedScore, ScoreRankResponse rank)
        {
            SetMessage($"送信成功: {postedScore.username} / {postedScore.score}点\nあなたの順位: {rank.rank}位");
        }

        public void ShowError(string message)
        {
            SetMessage(message);
        }

        public void SetInteractable(bool interactable)
        {
            // 通信中に連打されるとリクエストが重なるため、一時的に操作できないようにする
            if (submitButton != null)
            {
                submitButton.interactable = interactable;
            }

            if (usernameInput != null)
            {
                usernameInput.interactable = interactable;
            }

            if (scoreInput != null)
            {
                scoreInput.interactable = interactable;
            }
        }

        private void NotifySubmitRequested()
        {
            SubmitRequested?.Invoke();
        }

        private void SetMessage(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}

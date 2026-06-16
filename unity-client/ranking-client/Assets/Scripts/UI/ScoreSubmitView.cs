using System;
using Hakusa.RankingClient.Api;
using UnityEngine;
using UnityEngine.UI;

namespace Hakusa.RankingClient.UI
{
    /// <summary>
    /// スコア送信エリアの表示と入力取得を担当するView
    /// API通信は行わず、Controllerに「送信ボタンが押された」ことだけを知らせる
    /// </summary>
    public sealed class ScoreSubmitView : MonoBehaviour
    {
        /// <summary>
        /// ユーザー名を入力するInputField
        /// </summary>
        [SerializeField] private InputField usernameInput;

        /// <summary>
        /// スコアを入力するInputField
        /// </summary>
        [SerializeField] private InputField scoreInput;

        /// <summary>
        /// スコア送信を開始するButton
        /// </summary>
        [SerializeField] private Button submitButton;

        /// <summary>
        /// 送信状態やエラーを表示するText
        /// </summary>
        [SerializeField] private Text statusText;

        /// <summary>
        /// Submitボタンが押されたことをControllerへ通知するイベント
        /// </summary>
        public event Action SubmitRequested;

        /// <summary>
        /// GameObjectが有効になったときにSubmitボタンのイベントを購読する
        /// </summary>
        private void OnEnable()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(NotifySubmitRequested);
            }
        }

        /// <summary>
        /// GameObjectが無効になったときにSubmitボタンのイベント購読を解除する
        /// </summary>
        private void OnDisable()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(NotifySubmitRequested);
            }
        }

        /// <summary>
        /// 入力欄の値を読み取り、APIへ送るリクエストモデルを作成する
        /// 入力が不正な場合は画面にメッセージを表示してfalseを返す
        /// </summary>
        /// <param name="requestBody">作成したスコア登録リクエスト</param>
        /// <returns>入力値が有効ならtrue</returns>
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

        /// <summary>
        /// 送信できる待機状態を表示する
        /// </summary>
        public void ShowReady()
        {
            // 初期表示や通信完了後の待機状態
            SetMessage("送信できます");
        }

        /// <summary>
        /// スコア送信中の状態を表示する
        /// </summary>
        public void ShowSubmitting()
        {
            // POST /scores を待っている状態
            SetMessage("スコア送信中...");
        }

        /// <summary>
        /// スコア登録後に順位を取得している状態を表示する
        /// </summary>
        public void ShowRankLoading()
        {
            // POST成功後に、自分の順位を取得している状態
            SetMessage("順位を取得中...");
        }

        /// <summary>
        /// スコア送信と順位取得が成功した結果を表示する
        /// </summary>
        /// <param name="postedScore">登録されたスコア情報</param>
        /// <param name="rank">順位付きのスコア情報</param>
        public void ShowSubmittedScore(ScoreResponse postedScore, ScoreRankResponse rank)
        {
            SetMessage($"送信成功: {postedScore.username} / {postedScore.score}点\nあなたの順位: {rank.rank}位");
        }

        /// <summary>
        /// エラーメッセージを表示する
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        public void ShowError(string message)
        {
            SetMessage(message);
        }

        /// <summary>
        /// 入力欄とSubmitボタンの操作可否を切り替える
        /// 通信中の多重送信を防ぐために使う
        /// </summary>
        /// <param name="interactable">操作可能にするならtrue</param>
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

        /// <summary>
        /// SubmitボタンのOnClickから呼ばれ、SubmitRequestedイベントを発火する
        /// </summary>
        private void NotifySubmitRequested()
        {
            SubmitRequested?.Invoke();
        }

        /// <summary>
        /// 状態表示用Textにメッセージを反映する
        /// Text参照が未設定でも落ちないようにnullチェックする
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        private void SetMessage(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}

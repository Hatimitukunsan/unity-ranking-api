using System;
using System.Collections.Generic;
using Hakusa.RankingClient.Api;
using UnityEngine;
using UnityEngine.UI;

namespace Hakusa.RankingClient.UI
{
    /// <summary>
    /// ランキング一覧エリアの表示を担当するView
    /// 順位、ユーザー名、スコアを別々のText列に分けて表示する
    /// </summary>
    public sealed class RankingListView : MonoBehaviour
    {
        /// <summary>
        /// ランキングを再取得するButton
        /// </summary>
        [SerializeField] private Button refreshButton;

        /// <summary>
        /// 「上位n件を表示中」を表示するText
        /// </summary>
        [SerializeField] private Text limitInfoText;

        /// <summary>
        /// Text表示用フィールド
        /// 主にエラー表示などのフォールバックとして使う
        /// </summary>
        [SerializeField] private Text rankingText;

        /// <summary>
        /// 順位列を表示するText
        /// </summary>
        [SerializeField] private Text rankColumnText;

        /// <summary>
        /// ユーザー名列を表示するText
        /// </summary>
        [SerializeField] private Text usernameColumnText;

        /// <summary>
        /// スコア列を表示するText
        /// </summary>
        [SerializeField] private Text scoreColumnText;

        /// <summary>
        /// Refreshボタンが押されたことをControllerへ通知するイベント
        /// </summary>
        public event Action RefreshRequested;

        /// <summary>
        /// GameObjectが有効になったときにRefreshボタンのイベントを購読する
        /// </summary>
        private void OnEnable()
        {
            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(NotifyRefreshRequested);
            }
        }

        /// <summary>
        /// GameObjectが無効になったときにRefreshボタンのイベント購読を解除する
        /// </summary>
        private void OnDisable()
        {
            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveListener(NotifyRefreshRequested);
            }
        }

        /// <summary>
        /// ランキング未取得の待機状態を表示する
        /// </summary>
        /// <param name="limit">表示予定の上限件数</param>
        public void ShowWaiting(int limit)
        {
            // まだGET /ranking を呼んでいない状態
            SetLimitInfo(limit);
            SetText("ランキング未取得");
        }

        /// <summary>
        /// ランキング取得中の状態を表示する
        /// </summary>
        /// <param name="limit">取得中の上限件数</param>
        public void ShowLoading(int limit)
        {
            // GET /ranking のレスポンス待ち
            SetLimitInfo(limit);
            SetText("ランキング取得中...");
        }

        /// <summary>
        /// APIから取得したランキングを画面へ表示する
        /// 直近で投稿したスコアがある場合は、その行を強調表示する
        /// </summary>
        /// <param name="ranking">APIから取得したランキング配列</param>
        /// <param name="limit">表示上限件数</param>
        /// <param name="highlightedScore">強調したい自分のスコア。未投稿ならnull</param>
        public void ShowRanking(ScoreResponse[] ranking, int limit, ScoreRankResponse highlightedScore)
        {
            SetLimitInfo(limit);

            // APIから空配列が返ってきた場合も、画面上で分かるようにする
            if (ranking.Length == 0)
            {
                SetRankingColumns(
                    new[] { string.Empty },
                    new[] { "ランキングデータがありません" },
                    new[] { string.Empty });
                return;
            }

            // 順位・名前・スコアを別々のTextに流し込む
            // 単一Textでスペースやタブを使うより、列の縦位置が揃いやすい
            // 自分が直近で投稿したスコアが含まれる場合は、その行だけ強調する
            bool containsHighlightedScore = false;
            List<string> rankLines = new List<string>();
            List<string> usernameLines = new List<string>();
            List<string> scoreLines = new List<string>();
            for (int i = 0; i < ranking.Length; i++)
            {
                ScoreResponse score = ranking[i];
                string rankText = $"{i + 1}.";
                string usernameText = score.username;
                string scoreText = score.score.ToString();

                if (highlightedScore != null && score.id == highlightedScore.id)
                {
                    containsHighlightedScore = true;
                    rankText = HighlightLine(rankText);
                    usernameText = HighlightLine(usernameText);
                    scoreText = HighlightLine(scoreText);
                }

                rankLines.Add(rankText);
                usernameLines.Add(usernameText);
                scoreLines.Add(scoreText);
            }

            // 自分のスコアが上位n件に入っていない場合でも、現在順位が分かるように末尾へ追加する
            if (highlightedScore != null && !containsHighlightedScore)
            {
                rankLines.Add("...");
                usernameLines.Add("...");
                scoreLines.Add("...");

                rankLines.Add(HighlightLine($"{highlightedScore.rank}."));
                usernameLines.Add(HighlightLine(highlightedScore.username));
                scoreLines.Add(HighlightLine(highlightedScore.score.ToString()));
            }

            SetRankingColumns(rankLines, usernameLines, scoreLines);
        }

        /// <summary>
        /// ランキング欄にエラーを表示する
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        public void ShowError(string message)
        {
            SetText(message);
        }

        /// <summary>
        /// Refreshボタンの操作可否を切り替える
        /// 通信中の多重リクエストを防ぐために使う
        /// </summary>
        /// <param name="interactable">操作可能にするならtrue</param>
        public void SetInteractable(bool interactable)
        {
            // 通信中にRefreshが連打されないようにする
            if (refreshButton != null)
            {
                refreshButton.interactable = interactable;
            }
        }

        /// <summary>
        /// RefreshボタンのOnClickから呼ばれ、RefreshRequestedイベントを発火する
        /// </summary>
        private void NotifyRefreshRequested()
        {
            RefreshRequested?.Invoke();
        }

        /// <summary>
        /// 単一メッセージをランキング欄に表示する
        /// 通常のランキング表示では列表示を使う
        /// </summary>
        /// <param name="text">表示する文字列</param>
        private void SetText(string text)
        {
            if (rankingText != null)
            {
                // <b>と<color>を使って自分の行だけ強調する
                rankingText.supportRichText = true;
                rankingText.text = text;
            }

            SetRankingColumns(
                new[] { string.Empty },
                new[] { text },
                new[] { string.Empty });
        }

        /// <summary>
        /// 順位、ユーザー名、スコアをそれぞれのText列へ反映する
        /// </summary>
        /// <param name="ranks">順位列</param>
        /// <param name="usernames">ユーザー名列</param>
        /// <param name="scores">スコア列</param>
        private void SetRankingColumns(
            IReadOnlyList<string> ranks,
            IReadOnlyList<string> usernames,
            IReadOnlyList<string> scores)
        {
            SetColumnText(rankColumnText, ranks);
            SetColumnText(usernameColumnText, usernames);
            SetColumnText(scoreColumnText, scores);
        }

        /// <summary>
        /// 指定されたTextに複数行の文字列を設定する
        /// 自分の行の強調表示にrich textを使うためsupportRichTextを有効にする
        /// </summary>
        /// <param name="targetText">反映先のText</param>
        /// <param name="lines">表示する行一覧</param>
        private static void SetColumnText(Text targetText, IReadOnlyList<string> lines)
        {
            if (targetText == null)
            {
                return;
            }

            targetText.supportRichText = true;
            targetText.text = string.Join("\n", lines);
        }

        /// <summary>
        /// 表示上限件数の説明テキストを更新する
        /// </summary>
        /// <param name="limit">表示上限件数</param>
        private void SetLimitInfo(int limit)
        {
            if (limitInfoText != null)
            {
                limitInfoText.text = $"上位{limit}件を表示中";
            }
        }

        /// <summary>
        /// 自分のスコア行を青色かつ太字にするrich textを付ける
        /// </summary>
        /// <param name="line">強調したい文字列</param>
        /// <returns>rich textタグ付きの文字列</returns>
        private static string HighlightLine(string line)
        {
            return $"<b><color=#0B47B7>{line}</color></b>";
        }
    }
}

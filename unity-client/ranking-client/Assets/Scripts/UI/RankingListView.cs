using System;
using System.Collections.Generic;
using Hakusa.RankingClient.Api;
using UnityEngine;
using UnityEngine.UI;

namespace Hakusa.RankingClient.UI
{
    // ランキング一覧エリアの表示を担当するView
    // 最初はText 1つに複数行で表示し、Prefab化やScrollView化は後回しにする
    public sealed class RankingListView : MonoBehaviour
    {
        [SerializeField] private Button refreshButton;
        [SerializeField] private Text limitInfoText;
        [SerializeField] private Text rankingText;
        [SerializeField] private Text rankColumnText;
        [SerializeField] private Text usernameColumnText;
        [SerializeField] private Text scoreColumnText;

        public event Action RefreshRequested;

        private void OnEnable()
        {
            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(NotifyRefreshRequested);
            }
        }

        private void OnDisable()
        {
            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveListener(NotifyRefreshRequested);
            }
        }

        public void Configure(Button refreshButton, Text limitInfoText, Text rankingText)
        {
            this.refreshButton = refreshButton;
            this.limitInfoText = limitInfoText;
            this.rankingText = rankingText;
        }

        public void Configure(
            Button refreshButton,
            Text limitInfoText,
            Text rankColumnText,
            Text usernameColumnText,
            Text scoreColumnText)
        {
            this.refreshButton = refreshButton;
            this.limitInfoText = limitInfoText;
            this.rankColumnText = rankColumnText;
            this.usernameColumnText = usernameColumnText;
            this.scoreColumnText = scoreColumnText;
        }

        public void ShowWaiting(int limit)
        {
            // まだGET /ranking を呼んでいない状態
            SetLimitInfo(limit);
            SetText("ランキング未取得");
        }

        public void ShowLoading(int limit)
        {
            // GET /ranking のレスポンス待ち
            SetLimitInfo(limit);
            SetText("ランキング取得中...");
        }

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
                usernameLines.Add(string.Empty);
                scoreLines.Add(string.Empty);

                rankLines.Add(HighlightLine($"{highlightedScore.rank}."));
                usernameLines.Add(HighlightLine(highlightedScore.username));
                scoreLines.Add(HighlightLine(highlightedScore.score.ToString()));
            }

            SetRankingColumns(rankLines, usernameLines, scoreLines);
        }

        public void ShowError(string message)
        {
            SetText(message);
        }

        public void SetInteractable(bool interactable)
        {
            // 通信中にRefreshが連打されないようにする
            if (refreshButton != null)
            {
                refreshButton.interactable = interactable;
            }
        }

        private void NotifyRefreshRequested()
        {
            RefreshRequested?.Invoke();
        }

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

        private void SetRankingColumns(
            IReadOnlyList<string> ranks,
            IReadOnlyList<string> usernames,
            IReadOnlyList<string> scores)
        {
            SetColumnText(rankColumnText, ranks);
            SetColumnText(usernameColumnText, usernames);
            SetColumnText(scoreColumnText, scores);
        }

        private static void SetColumnText(Text targetText, IReadOnlyList<string> lines)
        {
            if (targetText == null)
            {
                return;
            }

            targetText.supportRichText = true;
            targetText.text = string.Join("\n", lines);
        }

        private void SetLimitInfo(int limit)
        {
            if (limitInfoText != null)
            {
                limitInfoText.text = $"上位{limit}件を表示中";
            }
        }

        private static string HighlightLine(string line)
        {
            return $"<b><color=#0B47B7>{line}</color></b>";
        }

    }
}

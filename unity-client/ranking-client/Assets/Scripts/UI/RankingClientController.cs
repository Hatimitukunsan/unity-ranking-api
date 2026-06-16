using System;
using Cysharp.Threading.Tasks;
using Hakusa.RankingClient.Api;
using UnityEngine;

namespace Hakusa.RankingClient.UI
{
    // 画面全体の流れを制御するController
    // Viewから入力やボタンイベントを受け取り、ApiClientを呼び出して結果をViewへ反映する
    public sealed class RankingClientController : MonoBehaviour
    {
        [SerializeField] private string apiBaseUrl = "http://localhost:8080";
        [SerializeField] private int rankingLimit = 10;
        [SerializeField] private ScoreSubmitView scoreSubmitView;
        [SerializeField] private RankingListView rankingListView;
        [SerializeField] private ApiStatusView apiStatusView;

        private RankingApiClient apiClient;
        private ScoreRankResponse lastSubmittedScore;

        private void Awake()
        {
            // API通信クラスはUIを知らない純粋な通信担当として作る
            apiClient = new RankingApiClient(apiBaseUrl);

            // 起動直後の表示を各Viewに任せる
            scoreSubmitView?.ShowReady();
            rankingListView?.ShowWaiting(rankingLimit);
            apiStatusView?.SetApiBaseUrl(apiBaseUrl);
            apiStatusView?.ShowReady();
        }

        private void OnEnable()
        {
            // ButtonのOnClickをControllerが直接持たず、Viewのイベントを購読する
            if (scoreSubmitView != null)
            {
                scoreSubmitView.SubmitRequested += OnSubmitRequested;
            }

            if (rankingListView != null)
            {
                rankingListView.RefreshRequested += OnRefreshRequested;
            }
        }

        private void OnDisable()
        {
            if (scoreSubmitView != null)
            {
                scoreSubmitView.SubmitRequested -= OnSubmitRequested;
            }

            if (rankingListView != null)
            {
                rankingListView.RefreshRequested -= OnRefreshRequested;
            }
        }

        public void Configure(
            ScoreSubmitView scoreSubmitView,
            RankingListView rankingListView,
            ApiStatusView apiStatusView)
        {
            // Editor自動生成スクリプトから、生成したViewをまとめて渡す
            this.scoreSubmitView = scoreSubmitView;
            this.rankingListView = rankingListView;
            this.apiStatusView = apiStatusView;
        }

        private void OnSubmitRequested()
        {
            SubmitScoreAsync().Forget();
        }

        private void OnRefreshRequested()
        {
            RefreshRankingAsync().Forget();
        }

        private async UniTaskVoid SubmitScoreAsync()
        {
            // 入力値の読み取りと簡単なチェックはSubmitViewに任せる
            if (scoreSubmitView == null || !scoreSubmitView.TryCreateRequest(out ScoreCreateRequest requestBody))
            {
                return;
            }

            SetBusy(true);
            scoreSubmitView.ShowSubmitting();
            apiStatusView?.ShowLoading("スコア送信中");

            try
            {
                // まずスコアを登録し、レスポンスで登録済みidを受け取る
                ScoreResponse postedScore = await apiClient.PostScoreAsync(
                    requestBody,
                    this.GetCancellationTokenOnDestroy());

                scoreSubmitView.ShowRankLoading();
                apiStatusView?.ShowLoading("順位取得中");

                // 登録されたidを使って、自分の順位を取得する
                ScoreRankResponse rank = await apiClient.GetRankAsync(
                    postedScore.id,
                    this.GetCancellationTokenOnDestroy());

                // ランキング表示で自分のスコアを強調するため、最後に投稿したスコアを保持する
                lastSubmittedScore = rank;
                scoreSubmitView.ShowSubmittedScore(postedScore, rank);
                apiStatusView?.ShowConnected();
                Debug.Log($"Score posted. id={postedScore.id}, username={postedScore.username}, score={postedScore.score}, rank={rank.rank}");

                // 登録後にランキングも更新して、画面全体の状態を最新にする
                await RefreshRankingAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Submit score request was canceled");
            }
            catch (Exception exception)
            {
                HandleException("スコア送信に失敗しました", exception);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async UniTask RefreshRankingAsync()
        {
            SetBusy(true);
            rankingListView?.ShowLoading(rankingLimit);
            apiStatusView?.ShowLoading("ランキング取得中");

            try
            {
                // ランキングはAPI側でスコア降順に並べて返す
                ScoreResponse[] ranking = await apiClient.GetRankingAsync(
                    rankingLimit,
                    this.GetCancellationTokenOnDestroy());

                rankingListView?.ShowRanking(ranking, rankingLimit, lastSubmittedScore);
                apiStatusView?.ShowConnected();
                Debug.Log($"Ranking loaded. count={ranking.Length}");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Refresh ranking request was canceled");
            }
            catch (Exception exception)
            {
                HandleException("ランキング取得に失敗しました", exception);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void HandleException(string userMessage, Exception exception)
        {
            // API独自例外なら、HTTPステータスやレスポンス本文もログに出す
            if (exception is RankingApiException apiException)
            {
                scoreSubmitView?.ShowError($"{userMessage}\nHTTP {apiException.StatusCode}: {apiException.Message}");
                rankingListView?.ShowError(userMessage);
                apiStatusView?.ShowError();
                Debug.LogError($"{userMessage}: status={apiException.StatusCode}, body={apiException.ResponseBody}");
                return;
            }

            // 想定外の例外も画面には短く表示し、詳細はConsoleへ出す
            scoreSubmitView?.ShowError($"{userMessage}\n{exception.Message}");
            rankingListView?.ShowError(userMessage);
            apiStatusView?.ShowError();
            Debug.LogError(exception);
        }

        private void SetBusy(bool isBusy)
        {
            // 通信中は入力やボタンを止め、ユーザーに処理中であることを伝える
            scoreSubmitView?.SetInteractable(!isBusy);
            rankingListView?.SetInteractable(!isBusy);
        }
    }
}

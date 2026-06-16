using System;
using Cysharp.Threading.Tasks;
using Hakusa.RankingClient.Api;
using UnityEngine;

namespace Hakusa.RankingClient.UI
{
    /// <summary>
    /// ランキング画面全体の流れを制御するControllerです。
    /// Viewから入力やボタンイベントを受け取り、ApiClientを呼び出して結果をViewへ反映します。
    /// </summary>
    public sealed class RankingClientController : MonoBehaviour
    {
        /// <summary>
        /// 接続先APIのベースURLです。
        /// Unity Editor上でローカルのFastAPIへ接続するため、初期値は http://localhost:8080 にしています。
        /// </summary>
        [SerializeField] private string apiBaseUrl = "http://localhost:8080";

        /// <summary>
        /// ランキング表示の上限件数です。
        /// この値を GET /ranking の limit クエリパラメータとしてAPIへ渡します。
        /// </summary>
        [SerializeField] private int rankingLimit = 10;

        /// <summary>
        /// 左側のスコア送信エリアを担当するViewです。
        /// ユーザー名とスコアの入力取得、送信状態の表示を行います。
        /// </summary>
        [SerializeField] private ScoreSubmitView scoreSubmitView;

        /// <summary>
        /// 右側のランキング表示エリアを担当するViewです。
        /// ランキング一覧、上位件数、自分のスコア行の強調表示を行います。
        /// </summary>
        [SerializeField] private RankingListView rankingListView;

        /// <summary>
        /// 画面下部のAPI接続状態を担当するViewです。
        /// 通信中、接続OK、エラーなどの状態を表示します。
        /// </summary>
        [SerializeField] private ApiStatusView apiStatusView;

        /// <summary>
        /// FastAPIサーバーと通信するためのAPIクライアントです。
        /// UIを直接触らず、HTTP通信だけを担当します。
        /// </summary>
        private RankingApiClient apiClient;

        /// <summary>
        /// 最後に投稿したスコアと順位です。
        /// ランキング表示で自分の行を強調するために保持します。
        /// </summary>
        private ScoreRankResponse lastSubmittedScore;

        /// <summary>
        /// Unityがオブジェクトを初期化するときに呼び出します。
        /// APIクライアントを作成し、各Viewを初期表示にします。
        /// </summary>
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

        /// <summary>
        /// GameObjectが有効になったときに呼び出されます。
        /// Viewから通知されるボタンイベントを購読します。
        /// </summary>
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

        /// <summary>
        /// GameObjectが無効になったときに呼び出されます。
        /// イベント購読を解除し、同じイベントが重複して呼ばれないようにします。
        /// </summary>
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

        /// <summary>
        /// Submitボタンが押されたときに呼び出されます。
        /// async処理を開始しますが、UnityのButtonイベントから呼べるように戻り値はvoidにしています。
        /// </summary>
        private void OnSubmitRequested()
        {
            SubmitScoreAsync().Forget();
        }

        /// <summary>
        /// Refreshボタンが押されたときに呼び出されます。
        /// ランキング再取得のasync処理を開始します。
        /// </summary>
        private void OnRefreshRequested()
        {
            RefreshRankingAsync().Forget();
        }

        /// <summary>
        /// 入力されたユーザー名とスコアをAPIへ送信し、登録後に自分の順位を取得します。
        /// 最後にランキングも再取得して、画面全体を最新状態にします。
        /// </summary>
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

        /// <summary>
        /// ランキングをAPIから取得し、ランキングViewへ反映します。
        /// 最後に投稿したスコアがある場合は、その行を強調表示する情報も渡します。
        /// </summary>
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

        /// <summary>
        /// API通信や予期しない例外を画面表示とConsoleログに変換します。
        /// ユーザーには短いメッセージを表示し、開発者向けの詳細はConsoleに出します。
        /// </summary>
        /// <param name="userMessage">画面に表示する短いエラーメッセージ</param>
        /// <param name="exception">発生した例外</param>
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

        /// <summary>
        /// 通信中かどうかに応じて、入力欄やボタンの操作可否を切り替えます。
        /// 連打による多重リクエストを防ぐために使います。
        /// </summary>
        /// <param name="isBusy">通信中ならtrue</param>
        private void SetBusy(bool isBusy)
        {
            // 通信中は入力やボタンを止め、ユーザーに処理中であることを伝える
            scoreSubmitView?.SetInteractable(!isBusy);
            rankingListView?.SetInteractable(!isBusy);
        }
    }
}

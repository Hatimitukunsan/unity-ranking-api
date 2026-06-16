using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Hakusa.RankingClient.Api
{
    /// <summary>
    /// FastAPIのランキングAPIと通信するためのクラス
    /// UIを直接触らず、HTTP通信とJSON変換だけを担当する
    /// </summary>
    public class RankingApiClient
    {
        /// <summary>
        /// APIサーバーのベースURL
        /// 例: http://localhost:8080
        /// </summary>
        private readonly string baseUrl;

        /// <summary>
        /// APIクライアントを作成する
        /// 末尾のスラッシュは内部で取り除く
        /// </summary>
        /// <param name="baseUrl">APIサーバーのベースURL</param>
        public RankingApiClient(string baseUrl)
        {
            // Inspectorで末尾スラッシュ付きURLを入れても動くように整える
            this.baseUrl = NormalizeBaseUrl(baseUrl);
        }

        /// <summary>
        /// スコア登録APIを呼び出す
        /// POST /scores にユーザー名とスコアを送信する
        /// </summary>
        /// <param name="requestBody">送信するユーザー名とスコア</param>
        /// <param name="cancellationToken">GameObject破棄時などに通信をキャンセルするためのToken</param>
        /// <returns>登録されたスコア情報</returns>
        public async UniTask<ScoreResponse> PostScoreAsync(
            ScoreCreateRequest requestBody,
            CancellationToken cancellationToken)
        {
            // Unity側の入力モデルをJSON文字列に変換してPOSTする
            string json = JsonUtility.ToJson(requestBody);
            string response = await SendJsonAsync(
                UnityWebRequest.kHttpVerbPOST,
                "/scores",
                json,
                cancellationToken);

            return JsonUtility.FromJson<ScoreResponse>(response);
        }

        /// <summary>
        /// ランキング取得APIを呼び出す
        /// GET /ranking?limit=n からスコア順の一覧を取得する
        /// </summary>
        /// <param name="limit">取得する最大件数</param>
        /// <param name="cancellationToken">GameObject破棄時などに通信をキャンセルするためのToken</param>
        /// <returns>ランキングのスコア配列</returns>
        public async UniTask<ScoreResponse[]> GetRankingAsync(
            int limit,
            CancellationToken cancellationToken)
        {
            // FastAPIはランキングをJSON配列として返す
            // JsonUtilityは配列を直接扱いにくいため、後でラップして変換する
            string response = await SendJsonAsync(
                UnityWebRequest.kHttpVerbGET,
                $"/ranking?limit={limit}",
                null,
                cancellationToken);

            return FromJsonArray<ScoreResponse>(response);
        }

        /// <summary>
        /// 順位取得APIを呼び出す
        /// GET /scores/{scoreId}/rank から指定スコアの順位を取得する
        /// </summary>
        /// <param name="scoreId">順位を調べたいスコアのID</param>
        /// <param name="cancellationToken">GameObject破棄時などに通信をキャンセルするためのToken</param>
        /// <returns>順位付きのスコア情報</returns>
        public async UniTask<ScoreRankResponse> GetRankAsync(
            int scoreId,
            CancellationToken cancellationToken)
        {
            // POST /scores のレスポンスで得たidを使って、自分の順位を取得する
            string response = await SendJsonAsync(
                UnityWebRequest.kHttpVerbGET,
                $"/scores/{scoreId}/rank",
                null,
                cancellationToken);

            return JsonUtility.FromJson<ScoreRankResponse>(response);
        }

        /// <summary>
        /// UnityWebRequestを使ってJSON APIへ通信する共通処理
        /// 成功時はレスポンス本文を返し、失敗時はRankingApiExceptionを投げる
        /// </summary>
        /// <param name="method">HTTPメソッド</param>
        /// <param name="path">ベースURL以降のパス</param>
        /// <param name="requestJson">送信するJSON文字列。GETの場合はnull</param>
        /// <param name="cancellationToken">GameObject破棄時などに通信をキャンセルするためのToken</param>
        /// <returns>レスポンス本文</returns>
        private async UniTask<string> SendJsonAsync(
            string method,
            string path,
            string requestJson,
            CancellationToken cancellationToken)
        {
            // UnityWebRequestはusingで囲み、通信終了後に確実に破棄する
            using UnityWebRequest request = new UnityWebRequest($"{baseUrl}{path}", method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(requestJson))
            {
                // JSONをリクエストBodyとして送るため、UTF-8のbyte配列に変換する
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.SetRequestHeader("Accept", "application/json");

            // UniTaskを使って、UnityWebRequestの完了をasync/awaitで待つ
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            string responseBody = request.downloadHandler.text;
            if (request.result != UnityWebRequest.Result.Success)
            {
                // UI側で扱いやすいように、HTTPステータスや本文を含む独自例外に変換する
                throw new RankingApiException(
                    request.error,
                    request.responseCode,
                    responseBody);
            }

            return responseBody;
        }

        /// <summary>
        /// JSON配列をUnityのJsonUtilityで扱えるようにラップして変換する
        ///
        /// 入力例:
        /// [{"id":1,"username":"ore","score":12345}]
        ///
        /// 変換後のJSON例:
        /// {"items":[{"id":1,"username":"ore","score":12345}]}
        ///
        /// 戻り値の例:
        /// ScoreResponse[] の 0 番目に id=1, username="ore", score=12345 が入る
        /// </summary>
        /// <typeparam name="T">配列要素の型</typeparam>
        /// <param name="json">APIから返ってきたJSON配列</param>
        /// <returns>変換後の配列</returns>
        private static T[] FromJsonArray<T>(string json)
        {
            // JsonUtilityは [{"id":1}] のようなトップレベル配列を直接扱いにくい
            // そのため {"items":[{"id":1}]} のように、itemsフィールドを持つJSONオブジェクトへ包む
            string wrappedJson = "{\"items\":" + json + "}";
            JsonArrayWrapper<T> wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrappedJson);
            return wrapper.items ?? Array.Empty<T>();
        }

        /// <summary>
        /// APIのベースURLを扱いやすい形に整える
        /// 空の場合はローカル開発用URLを返す
        ///
        /// 入出力例:
        /// " http://localhost:8080/ " -> "http://localhost:8080"
        /// "http://localhost:8080///" -> "http://localhost:8080"
        /// "" -> "http://localhost:8080"
        /// </summary>
        /// <param name="url">入力されたURL</param>
        /// <returns>正規化したURL</returns>
        private static string NormalizeBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                // 未設定でもローカル開発環境へ接続できるようにする
                return "http://localhost:8080";
            }

            return url.Trim().TrimEnd('/');
        }
    }
}

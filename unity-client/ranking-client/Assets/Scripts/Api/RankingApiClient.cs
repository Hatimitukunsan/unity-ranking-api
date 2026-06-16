using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Hakusa.RankingClient.Api
{
    // FastAPIのランキングAPIと通信するためのクラス
    // UIを直接触らず、HTTP通信とJSON変換だけを担当する
    public sealed class RankingApiClient
    {
        private readonly string baseUrl;

        public RankingApiClient(string baseUrl)
        {
            // Inspectorで末尾スラッシュ付きURLを入れても動くように整える
            this.baseUrl = NormalizeBaseUrl(baseUrl);
        }

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

        private static T[] FromJsonArray<T>(string json)
        {
            // 例: [{"id":1}] を {"items":[{"id":1}]} にしてJsonUtilityで読める形にする
            string wrappedJson = "{\"items\":" + json + "}";
            JsonArrayWrapper<T> wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrappedJson);
            return wrapper.items ?? Array.Empty<T>();
        }

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

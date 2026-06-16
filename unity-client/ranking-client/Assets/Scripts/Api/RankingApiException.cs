using System;

namespace Hakusa.RankingClient.Api
{
    /// <summary>
    /// API通信でHTTPエラーやネットワークエラーが起きたときに投げる例外
    /// UI側でステータスコードやレスポンス本文を表示しやすくするために用意している
    /// </summary>
    public class RankingApiException : Exception
    {
        /// <summary>
        /// HTTPステータスコード
        /// ネットワークエラーなどで取得できない場合はUnityWebRequestの値に従う
        /// </summary>
        public long StatusCode { get; }

        /// <summary>
        /// APIから返ってきたレスポンス本文
        /// エラー原因をConsoleで確認するために保持する
        /// </summary>
        public string ResponseBody { get; }

        /// <summary>
        /// API通信エラー用の例外を作成する
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="statusCode">HTTPステータスコード</param>
        /// <param name="responseBody">レスポンス本文</param>
        public RankingApiException(string message, long statusCode, string responseBody)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}

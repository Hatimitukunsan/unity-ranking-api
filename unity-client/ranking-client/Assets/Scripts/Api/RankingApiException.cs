using System;

namespace Hakusa.RankingClient.Api
{
    // API通信でHTTPエラーやネットワークエラーが起きたときに投げる例外
    // UI側でステータスコードやレスポンス本文を表示しやすくするために用意している
    public sealed class RankingApiException : Exception
    {
        public long StatusCode { get; }
        public string ResponseBody { get; }

        public RankingApiException(string message, long statusCode, string responseBody)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}

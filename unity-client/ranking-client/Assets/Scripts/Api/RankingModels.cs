using System;

namespace Hakusa.RankingClient.Api
{
    /// <summary>
    /// POST /scores に送るリクエスト用モデル
    /// FastAPI側の ScoreCreate と同じ形にしておく
    /// </summary>
    [Serializable]
    public sealed class ScoreCreateRequest
    {
        /// <summary>
        /// 登録するユーザー名
        /// JSONのキー名に合わせるため小文字フィールドにしている
        /// </summary>
        public string username;

        /// <summary>
        /// 登録するスコア
        /// JSONのキー名に合わせるため小文字フィールドにしている
        /// </summary>
        public int score;

        /// <summary>
        /// スコア登録リクエストを作成する
        /// </summary>
        /// <param name="username">ユーザー名</param>
        /// <param name="score">スコア</param>
        public ScoreCreateRequest(string username, int score)
        {
            this.username = username;
            this.score = score;
        }
    }

    /// <summary>
    /// POST /scores や GET /ranking から返ってくるスコア情報
    /// JsonUtilityで変換するため、プロパティではなくpublicフィールドにしている
    /// </summary>
    [Serializable]
    public sealed class ScoreResponse
    {
        /// <summary>
        /// ユーザー名
        /// </summary>
        public string username;

        /// <summary>
        /// スコア
        /// </summary>
        public int score;

        /// <summary>
        /// DBに保存されたスコアID
        /// </summary>
        public int id;
    }

    /// <summary>
    /// GET /scores/{score_id}/rank から返ってくる順位付きのスコア情報
    /// </summary>
    [Serializable]
    public sealed class ScoreRankResponse
    {
        /// <summary>
        /// ユーザー名
        /// </summary>
        public string username;

        /// <summary>
        /// スコア
        /// </summary>
        public int score;

        /// <summary>
        /// DBに保存されたスコアID
        /// </summary>
        public int id;

        /// <summary>
        /// スコアの順位
        /// 同点は同順位としてAPI側で計算される
        /// </summary>
        public int rank;
    }

    /// <summary>
    /// UnityのJsonUtilityでJSON配列を扱うためのラッパー
    /// {"items":[...]} の形に包んでから変換する
    /// </summary>
    /// <typeparam name="T">配列要素の型</typeparam>
    [Serializable]
    internal sealed class JsonArrayWrapper<T>
    {
        /// <summary>
        /// ラップされた配列
        /// </summary>
        public T[] items;
    }
}

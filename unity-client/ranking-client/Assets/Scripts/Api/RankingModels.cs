using System;

namespace Hakusa.RankingClient.Api
{
    // POST /scores に送るリクエスト用モデル
    // FastAPI側の ScoreCreate と同じ形にしておく
    [Serializable]
    public sealed class ScoreCreateRequest
    {
        public string username;
        public int score;

        public ScoreCreateRequest(string username, int score)
        {
            this.username = username;
            this.score = score;
        }
    }

    // POST /scores や GET /ranking から返ってくるスコア情報
    // JsonUtilityで変換するため、プロパティではなくpublicフィールドにしている
    [Serializable]
    public sealed class ScoreResponse
    {
        public string username;
        public int score;
        public int id;
    }

    // GET /scores/{score_id}/rank から返ってくる順位付きのスコア情報
    [Serializable]
    public sealed class ScoreRankResponse
    {
        public string username;
        public int score;
        public int id;
        public int rank;
    }

    // UnityのJsonUtilityはJSON配列を直接FromJsonできない
    // そのため {"items":[...]} の形に包んでから変換する
    [Serializable]
    internal sealed class JsonArrayWrapper<T>
    {
        public T[] items;
    }
}

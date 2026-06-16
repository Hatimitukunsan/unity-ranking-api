using UnityEngine;
using UnityEngine.UI;

namespace Hakusa.RankingClient.UI
{
    /// <summary>
    /// API接続状態を画面下部に表示するView
    /// 通信中か、成功したか、エラーになったかをユーザーに伝える
    /// </summary>
    public class ApiStatusView : MonoBehaviour
    {
        /// <summary>
        /// API状態を表示するText
        /// </summary>
        [SerializeField] private Text statusText;

        /// <summary>
        /// 表示するAPIのベースURL
        /// </summary>
        [SerializeField] private string apiBaseUrl = "http://localhost:8080";

        /// <summary>
        /// Controller側の設定値をAPI状態表示に反映する
        /// </summary>
        /// <param name="apiBaseUrl">APIのベースURL</param>
        public void SetApiBaseUrl(string apiBaseUrl)
        {
            // Controller側の設定値を実行時にも反映する
            this.apiBaseUrl = apiBaseUrl;
        }

        /// <summary>
        /// まだ通信していない待機状態を表示する
        /// </summary>
        public void ShowReady()
        {
            // まだ通信していない状態
            SetStatus("待機中");
        }

        /// <summary>
        /// 通信中の状態を表示する
        /// どの処理を待っているか分かるように処理名を受け取る
        /// </summary>
        /// <param name="action">実行中の処理名</param>
        public void ShowLoading(string action)
        {
            // どの通信を待っているか分かるように、処理名も表示する
            SetStatus($"{action}...");
        }

        /// <summary>
        /// API通信が成功した状態を表示する
        /// </summary>
        public void ShowConnected()
        {
            SetStatus("接続OK");
        }

        /// <summary>
        /// API通信に失敗した状態を表示する
        /// </summary>
        public void ShowError()
        {
            SetStatus("エラー");
        }

        /// <summary>
        /// API状態と接続先URLをTextへ反映する
        /// </summary>
        /// <param name="status">表示する状態</param>
        private void SetStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = $"API Status: {status} | API Base URL: {apiBaseUrl}";
            }
        }
    }
}

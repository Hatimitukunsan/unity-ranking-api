using UnityEngine;
using UnityEngine.UI;

namespace Hakusa.RankingClient.UI
{
    // API接続状態を画面下部に表示するView
    // 通信中か、成功したか、エラーになったかをユーザーに伝える
    public sealed class ApiStatusView : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private string apiBaseUrl = "http://localhost:8080";

        public void Configure(Text statusText, string apiBaseUrl)
        {
            // Editorスクリプトから自動生成したTextとAPI URLを渡す
            this.statusText = statusText;
            this.apiBaseUrl = apiBaseUrl;
        }

        public void SetApiBaseUrl(string apiBaseUrl)
        {
            // Controller側の設定値を実行時にも反映する
            this.apiBaseUrl = apiBaseUrl;
        }

        public void ShowReady()
        {
            // まだ通信していない状態
            SetStatus("待機中");
        }

        public void ShowLoading(string action)
        {
            // どの通信を待っているか分かるように、処理名も表示する
            SetStatus($"{action}...");
        }

        public void ShowConnected()
        {
            SetStatus("接続OK");
        }

        public void ShowError()
        {
            SetStatus("エラー");
        }

        private void SetStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = $"API Status: {status} | API Base URL: {apiBaseUrl}";
            }
        }
    }
}

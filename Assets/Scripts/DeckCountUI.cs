// DeckCountUI.cs

using UnityEngine;
using TMPro;
using System.Collections; // Coroutineのために必要

public class DeckCountUI : MonoBehaviour
{
    public TextMeshProUGUI countText;

    void Start()
    {
        // CoroutineでUpdateDeckCountを呼び出し、初期化の遅延に対応
        StartCoroutine(WaitForCardManager());
    }

    IEnumerator WaitForCardManager()
    {
        // CardManagerが初期化されるまで待機（最大1フレーム）
        yield return null; 
        
        // 待機後、更新処理を実行
        UpdateDeckCount();
    }

    public void UpdateDeckCount()
    {
        if (countText == null)
        {
            Debug.LogError("CountText is not assigned to DeckCountUI.");
            return;
        }

        if (CardManager.Instance == null)
        {
            // ★リトライ後も失敗した場合、エラーメッセージを変更★
            countText.text = "N/A";
            Debug.LogError("FATAL: CardManager Instance is not available, even after waiting one frame. Check Scene setup.");
            return;
        }
        
        int currentCount = CardManager.Instance.GetMainDeckCount();
        int maxLimit = CardManager.Instance.deckSizeLimit;
        
        countText.text = $"{currentCount} / {maxLimit}";
    }
}
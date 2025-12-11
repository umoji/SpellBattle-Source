using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUIController : MonoBehaviour
{
    [Header("UI パネルとコンテンツ")]
    public RectTransform cardListContent;    // コレクションの親 (CardListPanel)
    public RectTransform deckListContent;    // デッキリストの親 (DeckListPanel) 
    public GameObject cardUIPrefab;          // カードUIプレハブ
    
    [Header("デッキ情報表示")]
    public TextMeshProUGUI deckCountText;
    public Button toBattleButton;

    void Start()
    {
        if (CardManager.Instance == null)
        {
            Debug.LogError("CardManager Instance is missing. Cannot initialize Inventory UI.");
            return;
        }

        // 1. コレクションのカードを表示
        DisplayAllCards();

        // 2. デッキのカードを表示 ★DeckListPanelの表示を担当★
        DisplayDeckCards(); 
        
        // 3. デッキカウントUIを更新
        UpdateDeckCountUI(); 
    }

    // -------------------------------------------------------------------
    // カードリスト (コレクション) の表示
    // -------------------------------------------------------------------

    public void DisplayAllCards()
    {
        // 既存のカードUIをクリア
        foreach (Transform child in cardListContent)
        {
            Destroy(child.gameObject);
        }

        List<int> ownedCardIDs = new List<int>();
        
        // 所持しているすべてのカードIDを取得 (ID 1〜49を所持と仮定)
        for (int i = 1; i <= 49; i++) 
        {
            if (CardManager.Instance.GetCardCount(i) > 0)
            {
                ownedCardIDs.Add(i);
            }
        }

        foreach (int cardID in ownedCardIDs)
        {
            CardData cardData = CardManager.Instance.GetCardDataByID(cardID);

            if (cardData != null)
            {
                GameObject cardObject = Instantiate(cardUIPrefab, cardListContent);
                CardUI cardUI = cardObject.GetComponent<CardUI>();

                if (cardUI != null)
                {
                    // Inventory表示のためBattleManagerはnull
                    cardUI.SetupCard(cardData, null); 
                    // 所持枚数表示の更新ロジック
                }
            }
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardListContent);
    }
    
    // -------------------------------------------------------------------
    // デッキリスト (DeckListPanel) の表示
    // -------------------------------------------------------------------

    /// <summary>
    /// 現在デッキに入っているカードを DeckListPanel に表示する
    /// </summary>
    public void DisplayDeckCards()
    {
        // 既存のカードUIをクリア (重複を防ぐため)
        if (deckListContent == null)
        {
            Debug.LogError("DeckListContent (DeckListPanel parent) is not assigned in the Inspector!");
            return;
        }
        
        foreach (Transform child in deckListContent)
        {
            Destroy(child.gameObject);
        }

        // CardManagerからカードIDリストを取得 (ID 1-20が強制設定済み)
        List<int> deckIDs = CardManager.Instance.mainDeckCardIDs;

        if (deckIDs.Count == 0)
        {
            Debug.LogWarning("Deck list is empty. No cards to display in DeckListPanel.");
            return;
        }

        foreach (int cardID in deckIDs)
        {
            CardData cardData = CardManager.Instance.GetCardDataByID(cardID);

            if (cardData != null)
            {
                // UIを生成し、DeckListContentの子要素にする
                GameObject cardObject = Instantiate(cardUIPrefab, deckListContent);
                CardUI cardUI = cardObject.GetComponent<CardUI>();

                if (cardUI != null)
                {
                    // デッキ内表示のためBattleManagerはnull
                    cardUI.SetupCard(cardData, null); 
                }
            }
        }
        
        // レイアウトグループの強制更新
        LayoutRebuilder.ForceRebuildLayoutImmediate(deckListContent);
    }
    
    // -------------------------------------------------------------------
    // デッキカウントUIの更新
    // -------------------------------------------------------------------

    public void UpdateDeckCountUI()
    {
        if (deckCountText != null && CardManager.Instance != null)
        {
            int currentCount = CardManager.Instance.GetMainDeckCount();
            deckCountText.text = $"{currentCount} / {CardManager.Instance.deckSizeLimit}";
            
            deckCountText.color = (currentCount == CardManager.Instance.deckSizeLimit) ? Color.white : Color.red;
        }
    }

    // -------------------------------------------------------------------
    // ボタンのイベントハンドラ (省略)
    // -------------------------------------------------------------------
    
    public void OnCardAddedToDeck(int cardID)
    {
        // ... (デッキに追加するロジック)
        UpdateDeckCountUI();
        DisplayDeckCards(); 
    }
    
    public void OnCardRemovedFromDeck(int cardID)
    {
        // ... (デッキから削除するロジック)
        UpdateDeckCountUI();
        DisplayDeckCards(); 
    }
    
    public void OnToBattleButtonClicked()
    {
        // 【TODO】次のシーンへの遷移ロジック
    }
}
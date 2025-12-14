using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq; // Enumerable.Count() を使うために必要

public class InventoryUIController : MonoBehaviour
{
    [Header("UI パネルとコンテンツ")]
    public RectTransform cardListContent;    // コレクションの親 (CardListPanel)
    public RectTransform deckListContent;    // デッキリストの親 (DeckListPanel) 
    public GameObject cardUIPrefab;          // カードUIプレハブ
    
    [Header("デッキ情報表示")]
    public TextMeshProUGUI deckCountText;
    public Button toBattleButton;

    // ★修正点★: Gacha References は HomeUIController に移動したため削除
    
    void Start()
    {
        if (CardManager.Instance == null)
        {
            Debug.LogError("CardManager Instance is missing. Cannot initialize Inventory UI.");
            return;
        }

        // 1. コレクションのカードを表示 (HomeSceneでガチャを引いた結果がCardManagerに反映済み)
        DisplayAllCards();

        // 2. デッキのカードを表示
        DisplayDeckCards(); 
        
        // 3. デッキカウントUIを更新
        UpdateDeckCountUI(); 
        
        // ★修正点★: ガチャ関連の初期設定や結果表示ロジックは全て削除
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

        // 所持しているすべてのカードIDと枚数を取得
        Dictionary<int, int> ownedCardCounts = CardManager.Instance.GetAllOwnedCardCounts();

        foreach (var kvp in ownedCardCounts)
        {
            // 所持枚数が0より大きいカードのみ表示
            if (kvp.Value > 0)
            {
                int cardID = kvp.Key;
                // int count = kvp.Value; // 所持枚数表示用

                CardData cardData = CardManager.Instance.GetCardDataByID(cardID);

                if (cardData != null)
                {
                    GameObject cardObject = Instantiate(cardUIPrefab, cardListContent);
                    CardUI cardUI = cardObject.GetComponent<CardUI>();

                    if (cardUI != null)
                    {
                        // Inventory表示のためBattleManagerはnull
                        cardUI.SetupCard(cardData, null); 
                        
                        // 【TODO】コレクション内のカードUIに所持枚数を表示するロジックを追加
                    }
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
        if (deckListContent == null)
        {
            Debug.LogError("DeckListContent (DeckListPanel parent) is not assigned!");
            return;
        }
        
        // 既存のカードUIをクリア (重複を防ぐため)
        foreach (Transform child in deckListContent)
        {
            Destroy(child.gameObject);
        }

        // CardManagerからカードIDリストを取得
        List<int> deckIDs = CardManager.Instance.mainDeckCardIDs;

        foreach (int cardID in deckIDs)
        {
            CardData cardData = CardManager.Instance.GetCardDataByID(cardID);

            if (cardData != null)
            {
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
            
            deckCountText.color = (currentCount == CardManager.Instance.deckSizeLimit) ? Color.white : Color.yellow;
        }
        
        // DeckCountUI.csのインスタンスも更新
        DeckCountUI existingDeckCountUI = FindObjectOfType<DeckCountUI>();
        if (existingDeckCountUI != null)
        {
            existingDeckCountUI.UpdateDeckCount();
        }
    }

    // -------------------------------------------------------------------
    // その他イベントハンドラ
    // -------------------------------------------------------------------
    
    public void OnCardAddedToDeck(int cardID)
    {
        UpdateDeckCountUI();
        DisplayDeckCards(); 
    }
    
    public void OnCardRemovedFromDeck(int cardID)
    {
        UpdateDeckCountUI();
        DisplayDeckCards(); 
    }
    
    public void OnToBattleButtonClicked()
    {
        // デッキが規定サイズ（deckSizeLimit）に達していないとバトル開始できないようにするガード
        if (CardManager.Instance.GetMainDeckCount() != CardManager.Instance.deckSizeLimit)
        {
            Debug.LogWarning($"Cannot start battle. Deck size is {CardManager.Instance.GetMainDeckCount()} / {CardManager.Instance.deckSizeLimit}.");
            return;
        }
        
        // 【TODO】次のシーンへの遷移ロジック
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadBattleScene();
        }
    }
}
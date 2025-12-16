using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DropZoneType {
    Deck,
    Collection,
    None
}

public class DropZone : MonoBehaviour, IDropHandler
{
    public DropZoneType zoneType = DropZoneType.Deck; 

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;

        if (droppedObject != null)
        {
            CardUI cardUI = droppedObject.GetComponent<CardUI>();

            if (cardUI != null)
            {
                CardData droppedCardData = cardUI.GetCardData(); 
                RectTransform cardRect = droppedObject.GetComponent<RectTransform>();

                if (droppedCardData != null && CardManager.Instance != null)
                {
                    bool success = false;
                    
                    if (zoneType == DropZoneType.Deck)
                    {
                        // --- Deck DropZone (カードを追加) ---
                        
                        // 1. 同一リスト内ドロップのチェック: 同じリスト内なら並べ替えとみなし、データ操作をスキップ
                        // ★修正ポイント: GetOriginalParent() を使用
                        if (cardUI.GetOriginalParent() == this.transform)
                        {
                            cardUI.transform.SetParent(this.transform, false);
                            Debug.Log("Card dropped within the same DeckZone. Skipping data add.");
                            success = true; 
                        }
                        // 外部リストからのドロップの場合のみ、追加処理を行う
                        else if (CardManager.Instance.mainDeckCardIDs.Count < CardManager.Instance.deckSizeLimit)
                        {
                            // 所持数が1以上あるか確認
                            if (CardManager.Instance.GetCardCount(droppedCardData.CardID) > 0)
                            {
                                // データの追加
                                CardManager.Instance.mainDeckCardIDs.Add(droppedCardData.CardID);
                                
                                // 在庫消費 (-1)
                                CardManager.Instance.ChangeCardCount(droppedCardData.CardID, -1);
                                
                                // 見た目の移動
                                cardUI.transform.SetParent(this.transform, false);
                                
                                // ★重要: 新しい親を記憶させる
                                cardUI.SetOriginalParent(this.transform);
                                
                                Debug.Log($"Card ID {droppedCardData.CardID} added to deck. Owned: {CardManager.Instance.GetCardCount(droppedCardData.CardID)}");
                                success = true;
                            }
                            else
                            {
                                Debug.LogWarning("在庫がないため追加できません。");
                                ReturnToOriginalPosition(cardUI, cardRect);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("デッキ上限超過です。");
                            ReturnToOriginalPosition(cardUI, cardRect);
                        }
                    }
                    else if (zoneType == DropZoneType.Collection)
                    {
                        // --- Collection DropZone (カードを削除) ---
                        if (CardManager.Instance.mainDeckCardIDs.Remove(droppedCardData.CardID))
                        {
                            // 在庫の返却 (+1)
                            CardManager.Instance.ChangeCardCount(droppedCardData.CardID, 1);
                            
                            // デッキから外す場合はオブジェクトを破却（コレクション側は自動更新されるため）
                            Destroy(droppedObject); 
                            
                            Debug.Log($"Card ID {droppedCardData.CardID} removed from deck.");
                            success = true;
                        }
                        else
                        {
                            ReturnToOriginalPosition(cardUI, cardRect);
                        }
                    }
                    
                    if (success)
                    {
                        if(cardRect != null)
                        {
                            cardRect.anchoredPosition = Vector2.zero; 
                        }

                        // UIの再描画
                        InventoryUIController inventoryController = FindObjectOfType<InventoryUIController>();
                        if (inventoryController != null)
                        {
                            inventoryController.DisplayDeckCards(); 
                            inventoryController.DisplayAllCards(); 
                            inventoryController.UpdateDeckCountUI();
                        }
                    }
                }
                
                // 親要素のレイアウトを強制的に再計算
                RectTransform parentRect = GetComponent<RectTransform>();
                if (parentRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                }
            }
        }
    }

    /// <summary>
    /// ドロップ失敗時にカードを元の親と位置に戻す
    /// </summary>
    private void ReturnToOriginalPosition(CardUI cardUI, RectTransform cardRect)
    {
        Transform originalParent = cardUI.GetOriginalParent();
        int originalIndex = cardUI.GetOriginalIndex();
        
        if (cardRect != null && originalParent != null)
        {
            cardRect.SetParent(originalParent); 
            cardRect.SetSiblingIndex(originalIndex); 
            LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent.GetComponent<RectTransform>());
        }
    }
}
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
                        if (cardUI.GetOriginalParent() == this.transform)
                        {
                            cardUI.transform.SetParent(this.transform, false);
                            Debug.Log("Card dropped within the same DeckZone. Skipping data add.");
                            success = true; // 見た目の移動は成功
                        }
                        // 外部リストからのドロップの場合のみ、追加処理を行う
                        else if (CardManager.Instance.mainDeckCardIDs.Count < CardManager.Instance.deckSizeLimit)
                        {
                            // 【★修正ポイント 1: 在庫チェック★】
                            // 所持数が1以上あるか確認
                            if (CardManager.Instance.GetCardCount(droppedCardData.CardID) > 0)
                            {
                                // データの追加
                                CardManager.Instance.mainDeckCardIDs.Add(droppedCardData.CardID);
                                
                                // 【★修正ポイント 2: 在庫消費 (-1)★】
                                CardManager.Instance.ChangeCardCount(droppedCardData.CardID, -1);
                                
                                // 見た目の移動 (成功した場合のみ)
                                cardUI.transform.SetParent(this.transform, false);
                                
                                Debug.Log($"Card ID {droppedCardData.CardID} added to mainDeckCardIDs. New owned count: {CardManager.Instance.GetCardCount(droppedCardData.CardID)}");
                                success = true;
                            }
                            else
                            {
                                // 在庫がない場合は拒否
                                Debug.LogWarning($"Card ID {droppedCardData.CardID} has 0 owned count. Cannot add to deck. Returning card to original position.");
                                
                                // 元の位置に戻す処理
                                Transform originalParent = cardUI.GetOriginalParent();
                                int originalIndex = cardUI.GetOriginalIndex();
                                
                                if (cardRect != null && originalParent != null)
                                {
                                    cardRect.SetParent(originalParent); 
                                    cardRect.SetSiblingIndex(originalIndex); 
                                    LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent.GetComponent<RectTransform>());
                                }
                                // success は false のまま
                            }
                        }
                        else
                        {
                            // デッキ上限超過時、元の位置に戻す
                            Debug.LogWarning($"Deck size limit reached ({CardManager.Instance.deckSizeLimit}). Card returned to its original position.");
                            
                            Transform originalParent = cardUI.GetOriginalParent();
                            int originalIndex = cardUI.GetOriginalIndex();
                            
                            if (cardRect != null && originalParent != null)
                            {
                                cardRect.SetParent(originalParent); // 元の親に戻す
                                cardRect.SetSiblingIndex(originalIndex); // 元のインデックスに戻す
                                
                                // 元の親のレイアウトを更新
                                LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent.GetComponent<RectTransform>());
                            }
                            // success は false のまま
                        }
                    }
                    else if (zoneType == DropZoneType.Collection)
                    {
                        // --- Collection DropZone (カードを削除) ---
                        if (CardManager.Instance.mainDeckCardIDs.Remove(droppedCardData.CardID))
                        {
                            // 所持カード数を +1 する (在庫の返却)
                            CardManager.Instance.ChangeCardCount(droppedCardData.CardID, 1);
                            
                            // データ削除成功時のゲームオブジェクト破棄
                            Destroy(droppedObject); 
                            
                            Debug.Log($"Card ID {droppedCardData.CardID} removed from mainDeckCardIDs. New owned count: {CardManager.Instance.GetCardCount(droppedCardData.CardID)}");
                            success = true;
                        }
                        else
                        {
                            Debug.LogWarning($"Card ID {droppedCardData.CardID} not found in deck to remove.");
                            
                            // 削除失敗時（2回目のドラッグなど）は、元の位置に戻す
                            Transform originalParent = cardUI.GetOriginalParent();
                            int originalIndex = cardUI.GetOriginalIndex();
                            
                            if (cardRect != null && originalParent != null)
                            {
                                cardRect.SetParent(originalParent); // 元の親に戻す
                                cardRect.SetSiblingIndex(originalIndex); // 元のインデックスに戻す
                                
                                // 元の親のレイアウトを更新
                                LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent.GetComponent<RectTransform>());
                            }
                        }
                    }
                    
                    if (success)
                    {
                        // データ操作に成功した場合のみ位置をリセット
                        if(cardRect != null)
                        {
                            cardRect.anchoredPosition = Vector2.zero; 
                        }

                        // 両リストの再描画
                        InventoryUIController inventoryController = FindObjectOfType<InventoryUIController>();
                        
                        if (inventoryController != null)
                        {
                            inventoryController.DisplayDeckCards(); 
                            inventoryController.DisplayAllCards(); // コレクションも更新
                            inventoryController.UpdateDeckCountUI();
                        }
                        else 
                        {
                            DeckCountUI deckCountUI = FindObjectOfType<DeckCountUI>();
                            if (deckCountUI != null)
                            {
                                deckCountUI.UpdateDeckCount();
                            }
                        }
                    }
                }
                
                // 5. 親要素のレイアウトを強制的に再計算させる
                RectTransform parentRect = GetComponent<RectTransform>();
                if (parentRect != null && (GetComponent<LayoutGroup>() != null || GetComponentInParent<LayoutGroup>() != null))
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                }
                
                Debug.Log($"Card dropped onto DropZone: {gameObject.name}");
            }
        }
    }
}
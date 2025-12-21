using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryCardUI : CardUIBase, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform originalParent;
    private int originalIndex;

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ワールド座標でマウス位置に直接合わせる（Canvas設定に左右されない）
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        // DropZoneに受け取られなかった場合は元の位置へ戻る
        if (transform.parent == originalParent || transform.parent.root == transform.parent) {
            transform.SetSiblingIndex(originalIndex);
        }
    }
}
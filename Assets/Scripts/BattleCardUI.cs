using UnityEngine;
using UnityEngine.EventSystems;

public class BattleCardUI : CardUIBase, IBeginDragHandler, IDragHandler, IPointerClickHandler
{
    private BattleManager battleManager;
    private bool isSelected = false;
    private Vector3 originalLocalPos;
    private float slideTimer = 0;
    private bool isInitialized = false;

    public void SetupBattleCard(CardData data, BattleManager manager)
    {
        base.SetupCard(data);
        battleManager = manager;
        transform.localPosition = new Vector3(3000, 0, 0); // 初期位置を右端へ
    }

    void Update()
    {
        if (!isInitialized) {
            if (transform.localPosition.x < 2500) {
                originalLocalPos = transform.localPosition;
                isInitialized = true;
            }
            return;
        }

        if (slideTimer < 0.5f) {
            slideTimer += Time.deltaTime;
            // 3000から本来の位置へスライド
            transform.localPosition = Vector3.Lerp(new Vector3(originalLocalPos.x + 2000, originalLocalPos.y, 0), originalLocalPos, slideTimer / 0.5f);
        }

        // 選択時の浮き上がり
        Vector3 target = isSelected ? originalLocalPos + new Vector3(0, 40, 0) : originalLocalPos;
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * 15f);
    }

    public void OnDrag(PointerEventData eventData) {
        // 上下スワイプ判定のみ（座標は動かさない）
        float deltaY = eventData.position.y - eventData.pressPosition.y;
        if (Mathf.Abs(deltaY) > 50) {
            if (deltaY > 0 && !isSelected) ToggleSelect();
            else if (deltaY < 0 && isSelected) ToggleSelect();
            eventData.pointerDrag = null; // ドラッグ継続を解除
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {}
    public void OnPointerClick(PointerEventData eventData) => ToggleSelect();

    private void ToggleSelect() {
        if (isSelected) { battleManager.OnCardDeselected(this); isSelected = false; }
        else { if (battleManager.OnCardSelected(this)) isSelected = true; }
    }
}
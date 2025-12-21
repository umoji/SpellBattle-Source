using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image visualImage;     
    public Image frameImage;      
    public TextMeshProUGUI numberText; 
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI effectText;

    [Header("Detail Settings")]
    public GameObject cardDetailPrefab; 
    private GameObject currentDetailObj;
    private bool isPressing = false;
    private float pressTime = 0f;
    private const float LONG_PRESS_THRESHOLD = 0.5f; 
    private bool detailShownThisPress = false;

    [Header("Animation Settings")]
    public float moveSpeed = 15f; 
    public float selectOffset = 40f; 
    private Vector3 targetLocalPosition;
    private Vector3 originalLocalPosition; 
    private bool isPositionInitialized = false;
    
    private CardData assignedCardData; 
    private BattleManager battleManager; 
    private bool isSelected = false;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private Vector2 dragStartPosition;
    private bool isSwipeProcessed = false;
    private bool isDragging = false; 

    private float slideDuration = 0.5f; 
    private float slideTimer = 0f;
    private float defaultGroundY; 
    private bool isBattleScene = false;

    // --- 外部（DropZone等）から参照される変数 ---
    private Transform originalParent; 
    private int originalIndex;

    public void SetupCard(CardData data, BattleManager manager)
    {
        assignedCardData = data;
        battleManager = manager; 
        rectTransform = GetComponent<RectTransform>(); 
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        if (numberText != null) numberText.text = data.Number.ToString();
        if (nameText != null) nameText.text = data.CardName;
        if (effectText != null) effectText.text = data.EffectText;
        
        SetupTextAutoSizing(); 
        LoadVisualImage(data.visualAssetPath); 
        LoadFrameImage(data.Attribute); 
        SetupFrameAspects(); 

        isBattleScene = SceneManager.GetActiveScene().name == "BattleScene";
        isPositionInitialized = false; 

        if (isBattleScene)
        {
            slideTimer = 0f;
            transform.localPosition = new Vector3(3000f, 0, 0);
        }
        else
        {
            slideTimer = slideDuration; 
        }
    }

    void Update()
    {
        if (this == null || gameObject == null) return;
        
        // 1. 初期化待ち
        if (!isPositionInitialized)
        {
            HandleInitialPosition();
            return;
        }

        // 2. ドラッグ中は座標更新を停止（物理移動を優先）
        if (isDragging) return;

        // 3. 詳細表示（長押し）のカウント
        if (isPressing && currentDetailObj == null)
        {
            pressTime += Time.deltaTime;
            if (pressTime >= LONG_PRESS_THRESHOLD) ShowDetail();
        }
        if (currentDetailObj != null) UpdateDetailPosition();

        // 4. シーン別の目的地計算
        if (isBattleScene)
        {
            UpdateBattlePosition();
        }
        else
        {
            UpdateInventoryPosition();
        }

        // 5. 滑らかな移動の適用
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, Time.deltaTime * moveSpeed);
    }

    private void HandleInitialPosition()
    {
        if (Mathf.Abs(transform.localPosition.x) < 2500f || !isBattleScene)
        {
            originalLocalPosition = transform.localPosition;
            defaultGroundY = originalLocalPosition.y;
            targetLocalPosition = originalLocalPosition;

            if (isBattleScene)
            {
                transform.localPosition = new Vector3(originalLocalPosition.x + 2000f, originalLocalPosition.y, 0);
            }
            isPositionInitialized = true;
        }
    }

    private void UpdateBattlePosition()
    {
        if (slideTimer < slideDuration)
        {
            slideTimer += Time.deltaTime;
        }
        else
        {
            originalLocalPosition = new Vector3(transform.localPosition.x, defaultGroundY, 0);
        }
        targetLocalPosition = isSelected ? originalLocalPosition + new Vector3(0, selectOffset, 0) : originalLocalPosition;
    }

    private void UpdateInventoryPosition()
    {
        // インベントリではLayoutGroupに従う。選択中ならオフセット
        originalLocalPosition = transform.localPosition;
        targetLocalPosition = isSelected ? originalLocalPosition + new Vector3(0, selectOffset, 0) : originalLocalPosition;
    }

    // --- イベントハンドラ（ドラッグ & ドロップ） ---

    public void OnBeginDrag(PointerEventData eventData) 
    { 
        HideDetail(); 
        dragStartPosition = eventData.position; 
        isSwipeProcessed = false;
        isDragging = true; 
        
        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();
        
        if (!isBattleScene)
        {
            // インベントリ：描画順を最前面へ
            transform.SetAsLastSibling();
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }
    }

	public void OnDrag(PointerEventData eventData)
    {
        // 1. インベントリ・ガチャ画面：カードをマウスに追従させる
        if (!isBattleScene)
        {
            // 親（Canvas等）の RectTransform を取得
            RectTransform parentRT = transform.parent as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();

            if (parentRT != null && canvas != null)
            {
                // スクリーン座標を親のローカル座標に変換
                // CameraモードでもOverlayモードでも正確に追従させるための標準的な手法です
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRT, 
                    eventData.position, 
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, 
                    out Vector2 localPos))
                {
                    // 消失を防ぐため、anchoredPosition を使用してUI平面上に固定する
                    rectTransform.anchoredPosition = localPos;
                }
            }
        }
        // 2. バトル画面：スワイプによるカード選択
        else if (!isSwipeProcessed && isPositionInitialized)
        {
            float dragDistanceY = eventData.position.y - dragStartPosition.y;
            float dragDistanceX = Mathf.Abs(eventData.position.x - dragStartPosition.x);

            // 横ブレ耐性（40f未満）
            if (dragDistanceX < 40f) 
            {
                if (!isSelected && dragDistanceY > 60f) { ToggleSelect(); isSwipeProcessed = true; }
                else if (isSelected && dragDistanceY < -60f) { ToggleSelect(); isSwipeProcessed = true; }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData) 
    { 
        isDragging = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        if (!isBattleScene && transform.parent == transform.root)
        {
            transform.SetSiblingIndex(originalIndex);
        }
    }

    // --- 外部参照用メソッド（DropZoneエラー解消） ---
    public CardData GetCardData() => assignedCardData;
    public Transform GetOriginalParent() => originalParent;
    public int GetOriginalIndex() => originalIndex;
    public void SetOriginalParent(Transform parent) => originalParent = parent;

    // --- その他共通メソッド ---
    public void SetAvailableState(bool isAvailable) 
    { 
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>(); 
        canvasGroup.alpha = isAvailable ? 1.0f : 0.4f; 
        canvasGroup.blocksRaycasts = isAvailable; 
    }

    public void ResetPosition() { isSelected = false; }

    private void ShowDetail()
    {
        if (cardDetailPrefab == null || assignedCardData == null) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        currentDetailObj = Instantiate(cardDetailPrefab, canvas.transform);
        if (currentDetailObj != null)
        {
            detailShownThisPress = true;
            RectTransform rt = currentDetailObj.GetComponent<RectTransform>();
            rt.localScale = new Vector3(2.5f, 2.5f, 1f);
            UpdateDetailPosition();

            CardDetailController controller = currentDetailObj.GetComponent<CardDetailController>();
            if (controller != null) controller.SetDetail(assignedCardData);
            currentDetailObj.transform.SetAsLastSibling();
        }
    }

    private void UpdateDetailPosition()
    {
        if (currentDetailObj == null) return;
        RectTransform detailRT = currentDetailObj.GetComponent<RectTransform>();
        RectTransform canvasRT = detailRT.parent as RectTransform;
        Vector3 cardWorldPos = transform.position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, cardWorldPos, null, out Vector2 cardLocalPos);

        float detailWidth = detailRT.rect.width * 2.5f;
        float detailHeight = detailRT.rect.height * 2.5f;
        float offsetWidth = 350f; 
        Vector2 targetPos = cardLocalPos + new Vector2(-offsetWidth, 0);

        float canvasLeftEdge = -canvasRT.rect.width / 2f;
        if (targetPos.x - (detailWidth / 2f) < canvasLeftEdge) targetPos.x = cardLocalPos.x + offsetWidth;
        
        float canvasRightEdge = canvasRT.rect.width / 2f;
        targetPos.x = Mathf.Clamp(targetPos.x, canvasLeftEdge + (detailWidth / 2f), canvasRightEdge - (detailWidth / 2f));

        float canvasTopEdge = canvasRT.rect.height / 2f;
        float canvasBottomEdge = -canvasRT.rect.height / 2f;
        targetPos.y = Mathf.Clamp(targetPos.y, canvasBottomEdge + (detailHeight / 2f), canvasTopEdge - (detailHeight / 2f));

        detailRT.anchoredPosition = targetPos;
        detailRT.localPosition = new Vector3(detailRT.localPosition.x, detailRT.localPosition.y, 0f);
    }

    private void HideDetail()
    {
        isPressing = false;
        pressTime = 0f;
        if (currentDetailObj != null) { Destroy(currentDetailObj); currentDetailObj = null; }
    }

    public void OnPointerDown(PointerEventData eventData) { isPressing = true; pressTime = 0f; detailShownThisPress = false; }
    public void OnPointerUp(PointerEventData eventData) { HideDetail(); }
    public void OnPointerExit(PointerEventData eventData) { HideDetail(); }
    public void OnPointerClick(PointerEventData eventData) { if (eventData.dragging || detailShownThisPress || pressTime >= LONG_PRESS_THRESHOLD) return; ToggleSelect(); }

    private void ToggleSelect() 
    { 
        if (!isPositionInitialized) return; 
        if (isBattleScene) 
        { 
            if (battleManager == null) return;
            if (!isSelected) { if (battleManager.OnCardSelected(this)) isSelected = true; } 
            else { battleManager.OnCardDeselected(this); isSelected = false; } 
        } 
        else { isSelected = !isSelected; } 
    }

    private void SetupFrameAspects() { AdjustAspect(visualImage); }
    private void AdjustAspect(Image targetImage) { if (targetImage == null || targetImage.sprite == null) return; AspectRatioFitter fitter = targetImage.GetComponent<AspectRatioFitter>() ?? targetImage.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectRatio = targetImage.sprite.rect.width / targetImage.sprite.rect.height; fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; }
    private void LoadVisualImage(string assetPath) { if (visualImage == null) return; string path = "CardVisuals/" + (string.IsNullOrEmpty(assetPath) ? "default_visual" : assetPath); Sprite s = Resources.Load<Sprite>(path); if (s != null) { visualImage.sprite = s; visualImage.color = Color.white; } }
    private void LoadFrameImage(ElementType attribute) { if (frameImage == null) return; string path = "CardFrames/CardFrame" + attribute.ToString(); Sprite s = Resources.Load<Sprite>(path); if (s != null) { frameImage.sprite = s; frameImage.color = Color.white; } }
    private void SetupTextAutoSizing() { if (nameText != null) nameText.enableAutoSizing = true; if (numberText != null) numberText.enableAutoSizing = true; if (effectText != null) effectText.enableAutoSizing = true; }
}
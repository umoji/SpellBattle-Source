using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public Image visualImage;     
    public Image frameImage;      
    // ★修正ポイント：変数名を numberText に変更
    public TextMeshProUGUI numberText; 
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI effectText;

    private CardData assignedCardData; 
    private BattleManager battleManager; 

    private bool isSelected = false;
    private Vector3 originalLocalPosition;
    private bool isPositionInitialized = false; 
    private float selectOffset = 40f; 

    private Transform originalParent; 
    private int originalIndex;        
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    public void SetupCard(CardData data, BattleManager manager)
    {
        assignedCardData = data;
        battleManager = manager; 
        
        rectTransform = GetComponent<RectTransform>(); 
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // ★修正ポイント：numberText を使用
        if (numberText != null)
        {
            numberText.text = data.Number.ToString();
        }
        
        nameText.text = data.CardName;
        effectText.text = data.EffectText;
        
        SetupTextAutoSizing(); 
        LoadVisualImage(data.visualAssetPath); 
        LoadFrameImage(data.Attribute); 
        SetupFrameAspects(); 

        isSelected = false;
        isPositionInitialized = false;
        
        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();

        Debug.Log($"DEBUG: CardUI Setup complete for {data.CardName} (Number: {data.Number}).");
    }

    public CardData GetCardData() { return assignedCardData; }
    public Transform GetOriginalParent() { return originalParent; }
    public int GetOriginalIndex() { return originalIndex; }
    public void SetOriginalParent(Transform parent) { originalParent = parent; }

    // --- バトル用クリック処理 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;
        ToggleSelect();
    }

    private void ToggleSelect()
    {
        if (battleManager == null) return;

        if (!isPositionInitialized)
        {
            originalLocalPosition = transform.localPosition;
            isPositionInitialized = true;
        }

        if (!isSelected)
        {
            if (battleManager.OnCardSelected(this))
            {
                isSelected = true;
                transform.localPosition = originalLocalPosition + new Vector3(0, selectOffset, 0);
            }
        }
        else
        {
            battleManager.OnCardDeselected(this);
            ResetPosition();
        }
    }

    public void ResetPosition()
    {
        isSelected = false;
        if (isPositionInitialized)
        {
            transform.localPosition = originalLocalPosition;
        }
    }

    // --- 編成画面用ドラッグ処理 ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();
        
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.6f;
        }
        transform.SetParent(transform.root); 
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1.0f;
        }

        if (transform.parent == transform.root)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalIndex);
        }
    }

    // --- 共通処理 ---
    private void SetupFrameAspects() { AdjustAspect(visualImage); }

    private void AdjustAspect(Image targetImage)
    {
        if (targetImage == null || targetImage.sprite == null) return;
        AspectRatioFitter fitter = targetImage.GetComponent<AspectRatioFitter>() ?? targetImage.gameObject.AddComponent<AspectRatioFitter>();
        float aspectRatio = targetImage.sprite.rect.width / targetImage.sprite.rect.height;
        fitter.aspectRatio = aspectRatio;
        fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; 
    }

    private void LoadVisualImage(string assetPath)
    {
        if (visualImage == null) return;
        string path = "CardVisuals/" + (string.IsNullOrEmpty(assetPath) ? "default_visual" : assetPath);
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) { visualImage.sprite = s; visualImage.color = Color.white; }
    }

    private void LoadFrameImage(ElementType attribute) 
    {
        if (frameImage == null) return;
        string path = "CardFrames/CardFrame" + attribute.ToString();
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) { frameImage.sprite = s; frameImage.color = Color.white; }
    }

    private void SetupTextAutoSizing()
    {
        if (nameText != null) nameText.enableAutoSizing = true;
        // ★修正ポイント：numberText を使用
        if (numberText != null) numberText.enableAutoSizing = true;
        if (effectText != null) effectText.enableAutoSizing = true;
    }
}
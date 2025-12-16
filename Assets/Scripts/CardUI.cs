using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

// IBeginDragHandler, IDragHandler, IEndDragHandlerを実装
public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // --- UIコンポーネントへの参照 ---
    public Image visualImage;     
    public Image frameImage;      
    public TextMeshProUGUI numberText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI effectText;

    // --- ロジックに必要なデータ ---
    private CardData assignedCardData; 
    private BattleManager battleManager; 

    // --- ドラッグ機能に必要な変数 ---
    private Transform originalParent; 
    private RectTransform rectTransform; 
    private CanvasGroup canvasGroup; 
    private Button buttonComponent; 
    private int originalIndex;

    // 初期化関数
    public void SetupCard(CardData data, BattleManager manager)
    {
        assignedCardData = data;
        battleManager = manager; 
        
        rectTransform = GetComponent<RectTransform>(); 
        canvasGroup = GetComponent<CanvasGroup>();
        buttonComponent = GetComponent<Button>(); 
        
        numberText.text = data.Number.ToString();
        nameText.text = data.CardName;
        effectText.text = data.EffectText;
        
        // ユーティリティメソッドの呼び出し
        SetupTextAutoSizing(); 
        LoadVisualImage(data.visualAssetPath); 
        
        // ★修正点★: 属性を渡してフレーム画像をロードする
        LoadFrameImage(data.Attribute); 
        
        SetupFrameAspects(); 

        // Buttonのリスナーはドラッグ使用に切り替えたため削除
        if (buttonComponent != null)
        {
            buttonComponent.onClick.RemoveAllListeners();
        }
        
        Debug.Log($"DEBUG: CardUI Setup complete for {data.CardName}.");
    }

    // --- DropZone.cs が必要とするゲッター ---
    public CardData GetCardData() { return assignedCardData; }
    public Transform GetOriginalParent() { return originalParent; }
    public int GetOriginalIndex() { return originalIndex; }
    
    // =================================================================
    // ドラッグ機能の実装
    // =================================================================
    
    public void OnBeginDrag(PointerEventData eventData) 
    {
        if (rectTransform == null) { rectTransform = GetComponent<RectTransform>(); if (rectTransform == null) return; }
        
        // 元の親とインデックスを保持
        originalIndex = rectTransform.GetSiblingIndex(); 
        originalParent = rectTransform.parent;
        
        // ドラッグ中はCanvasのルートに移動 (手札レイアウトから一時的に離脱)
        rectTransform.SetParent(transform.root); 
        
        if (canvasGroup != null)
        {
            // ★重要★: ドラッグ中はレイキャストを無効にし、ドロップ先オブジェクトを透過させる
            canvasGroup.blocksRaycasts = false; 
        }
        
        // ボタンを無効化 (競合防止)
        if (buttonComponent != null)
        {
            buttonComponent.interactable = false;
        }
        Debug.Log($"DEBUG: Dragging started for {assignedCardData.CardName}.");
    }

    public void OnDrag(PointerEventData eventData) 
    {
        if (rectTransform == null) { rectTransform = GetComponent<RectTransform>(); return; }
        
        // マウスの動きに合わせてカードを移動
        rectTransform.anchoredPosition += eventData.delta / transform.lossyScale.x; 
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            // ★重要★: ドラッグ終了後にレイキャストを有効に戻す
            canvasGroup.blocksRaycasts = true; 
        }
        
        if (buttonComponent != null)
        {
            buttonComponent.interactable = true;
        }
        
        bool droppedOnUseZone = false;
        
        // ポインターの下にあるオブジェクトをチェックし、UseZoneを探す
        foreach (GameObject droppedObject in eventData.hovered)
        {
            if (droppedObject.GetComponent<UseZone>() != null || droppedObject.GetComponentInParent<UseZone>() != null)
            {
                droppedOnUseZone = true;
                break;
            }
        }
        
        if (droppedOnUseZone && assignedCardData != null && battleManager != null)
        {
            Debug.Log($"DEBUG: Card dropped on UseZone. Attempting to use {assignedCardData.CardName}.");
            
            // BattleManagerにカード使用を試み、成功/失敗の結果を受け取る
            bool success = battleManager.UseCard(this.gameObject, assignedCardData);
            
            if (!success)
            {
                // コスト不足などで失敗した場合、手札に戻す
                MoveCardBackToHand();
            }
        }
        else
        {
            // UseZoneにドロップされなかった場合、元の場所に戻す
            MoveCardBackToHand();
        }
    }
    
    // カードを手札の元の位置に戻す
    private void MoveCardBackToHand()
    {
        if (originalParent != null && rectTransform != null)
        {
            rectTransform.SetParent(originalParent);
            rectTransform.SetSiblingIndex(originalIndex); 
            // レイアウト再計算
            LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent.GetComponent<RectTransform>());
        }
    }

    // =================================================================
    // ユーティリティメソッドの実装
    // =================================================================

    private void SetupFrameAspects()
    {
        AdjustAspect(visualImage);
    }

    private void AdjustAspect(Image targetImage)
    {
        if (targetImage == null || targetImage.sprite == null) return;

        AspectRatioFitter fitter = targetImage.GetComponent<AspectRatioFitter>();
        if (fitter == null)
        {
            fitter = targetImage.gameObject.AddComponent<AspectRatioFitter>();
        }

        float aspectRatio = targetImage.sprite.rect.width / targetImage.sprite.rect.height;
        fitter.aspectRatio = aspectRatio;
        fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; 
    }

    private void LoadVisualImage(string assetPath)
    {
        if (visualImage == null) return;
        
        // アセット名が 'default_visual' のままなので、必要に応じて 'ETNR_001' などに変更
        string finalAssetPath = string.IsNullOrEmpty(assetPath) ? "default_visual" : assetPath; 
        string path = "CardVisuals/" + finalAssetPath;
        
        Sprite cardSprite = Resources.Load<Sprite>(path);
        
        if (cardSprite != null)
        {
            visualImage.sprite = cardSprite;
            visualImage.color = Color.white;
            Debug.Log($"Load Visual Success: {path}");
        }
        else
        {
            Debug.LogError($"CRITICAL: Sprite NOT found at Resources path: {path}. Check asset path and file name.");
            visualImage.color = Color.red; 
        }
    }

    // ★修正箇所★: 属性名に基づいてフレーム画像をロードするように修正
    private void LoadFrameImage(ElementType attribute) 
    {
        if (frameImage == null)
        {
            Debug.LogError("FrameImageがCardUIコンポーネントに割り当てられていません！");
            return;
        }

        // フレーム画像の命名規則: "CardFrame" + 属性名 (例: "CardFrameFire")
        string assetName = "CardFrame" + attribute.ToString(); 
        // フォルダパスと連結
        string path = "CardFrames/" + assetName;
        
        Sprite frameSprite = Resources.Load<Sprite>(path);

        if (frameSprite != null)
        {
            frameImage.sprite = frameSprite;
            frameImage.color = Color.white;
            Debug.Log($"Load Frame Success: {path}");
        }
        else
        {
            Debug.LogError($"CRITICAL: Attribute Frame Sprite NOT found at Resources path: {path}. Check asset path and file name.");
            frameImage.sprite = null;
        }
    }

    private void SetupTextAutoSizing()
    {
        if (nameText != null) {
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 8; 
            nameText.fontSizeMax = 32; 
        }
        if (numberText != null) {
            numberText.enableAutoSizing = true;
            numberText.fontSizeMin = 6; 
            numberText.fontSizeMax = 24; 
        }
        if (effectText != null) {
            effectText.enableAutoSizing = true;
            effectText.fontSizeMin = 4; 
            effectText.fontSizeMax = 18;
            effectText.enableWordWrapping = true;
        }
    }
}
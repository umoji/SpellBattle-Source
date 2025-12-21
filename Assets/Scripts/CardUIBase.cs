using UnityEngine;
using TMPro;
using UnityEngine.UI;

// abstractにすることで、このスクリプト単体では動かないようにします
public abstract class CardUIBase : MonoBehaviour 
{
    [Header("UI References")]
    public Image visualImage;     
    public Image frameImage;      
    public TextMeshProUGUI numberText; 
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI effectText;

    protected CardData assignedCardData;
    protected RectTransform rectTransform;
    protected CanvasGroup canvasGroup;

    public virtual void SetupCard(CardData data)
    {
        assignedCardData = data;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (numberText != null) numberText.text = data.Number.ToString();
        if (nameText != null) nameText.text = data.CardName;
        if (effectText != null) effectText.text = data.EffectText;

        LoadVisualImage(data.visualAssetPath);
        LoadFrameImage(data.Attribute);
    }

    public CardData GetCardData() => assignedCardData;

    // --- 共通の画像読み込みロジック ---
    protected void LoadVisualImage(string assetPath) {
        string path = "CardVisuals/" + (string.IsNullOrEmpty(assetPath) ? "default_visual" : assetPath);
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) visualImage.sprite = s;
    }

    protected void LoadFrameImage(ElementType attribute) {
        string path = "CardFrames/CardFrame" + attribute.ToString();
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) frameImage.sprite = s;
    }
}
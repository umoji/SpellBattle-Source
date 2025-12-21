using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardDetailController : MonoBehaviour
{
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI numberText; // Inspectorで数字用のテキストをアサイン
    public Image cardImage;
    public Image frameImage; 

    public void SetDetail(CardData data)
    {
        if (data == null) return;

        if (cardNameText != null) cardNameText.text = data.CardName;
        if (descriptionText != null) descriptionText.text = data.EffectText;
        if (numberText != null) numberText.text = data.Number.ToString();

        if (cardImage != null)
        {
            string path = "CardVisuals/" + (string.IsNullOrEmpty(data.visualAssetPath) ? "default_visual" : data.visualAssetPath);
            cardImage.sprite = Resources.Load<Sprite>(path);
        }

        if (frameImage != null)
        {
            string framePath = "CardFrames/CardFrame" + data.Attribute.ToString();
            frameImage.sprite = Resources.Load<Sprite>(framePath);
        }
    }

    // CardUI側で座標管理するため空にしておく
    public void UpdatePosition(Vector2 screenPosition) { }
}
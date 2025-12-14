using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// UI Imageコンポーネントを点滅させるエフェクトを制御します。
/// </summary>
public class ButtonFlasher : MonoBehaviour
{
    private Image buttonImage;
    private bool isFlashing = false;
    private Coroutine flashCoroutine;

    [Header("Flash Settings")]
    [Tooltip("点滅の最高輝度色")]
    // ★修正点★: 最高輝度を白 (White) に設定
    public Color startColor = Color.white; 
    
    [Tooltip("点滅の最低輝度色 (ベースカラーと透明度を調整)")]
    // ★修正点★: 最低輝度を半透明な白に設定 (白い光彩のような効果)
    public Color endColor = new Color(1f, 1f, 1f, 0.5f); 
    
    [Tooltip("点滅スピード (値が大きいほど速い)")]
    public float flashSpeed = 1.5f; 

    void Awake()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            Debug.LogError("ButtonFlasher requires an Image component on the same GameObject.");
            enabled = false;
        }
    }

    /// <summary>
    /// 点滅効果を開始します。
    /// </summary>
    public void StartFlashing()
    {
        if (isFlashing) return;

        isFlashing = true;
        flashCoroutine = StartCoroutine(FlashingEffect());
    }

    /// <summary>
    /// 点滅効果を停止し、ボタンの色をリセットします。
    /// </summary>
    public void StopFlashing()
    {
        if (!isFlashing) return;

        isFlashing = false;
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        
        // ボタンの色を通常の色（White）に戻す
        if (buttonImage != null)
        {
            buttonImage.color = Color.white; 
        }
    }

    private IEnumerator FlashingEffect()
    {
        while (isFlashing)
        {
            // Mathf.Sin() を使用して、0から1の間を滑らかに繰り返す値 (t) を生成します。
            float t = (Mathf.Sin(Time.time * flashSpeed * 2 * Mathf.PI) + 1f) / 2f;
            
            // endColor (ベース) と startColor (明るい白) の間で Lerp
            buttonImage.color = Color.Lerp(endColor, startColor, t);
            
            yield return null;
        }
    }
}
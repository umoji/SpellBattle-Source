using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Imageコンポーネントを使うために必要

public class EnemyAnimator : MonoBehaviour
{
    // 敵のImageコンポーネント (Inspectorで設定)
    [SerializeField] private Image enemyImage;

    // --- 色変化の設定 ---
    [Header("Hit Color Settings")]
    public Color hitColor = Color.red;      // 被弾時の色 (赤)
    public float hitDuration = 0.1f;        // 色を維持する時間
    private Color originalColor;

    // --- 揺れ（ノックバック）の設定 ---
    [Header("Shake Settings")]
    public float shakeDistance = 10f;       // 揺れる最大距離 (ピクセル単位)
    public float shakeDuration = 0.2f;      // 揺れにかける時間
    private Vector3 originalPosition;

    void Awake()
    {
        if (enemyImage == null)
        {
            enemyImage = GetComponent<Image>();
        }
        if (enemyImage != null)
        {
            originalColor = enemyImage.color;
            // RectTransformであれば、localPositionを取得
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                originalPosition = rect.localPosition;
            }
            else
            {
                originalPosition = transform.localPosition;
            }
        }
    }

    /// <summary>
    /// ダメージを受けた際のアニメーションを起動する
    /// </summary>
    public void PlayHitAnimation()
    {
        // 既にアニメーションが実行中の場合は停止して再開
        StopAllCoroutines(); 
        StartCoroutine(HitFlashCoroutine());
        StartCoroutine(ShakeCoroutine());
    }

    // --- コルーチン 1: 点滅アニメーション ---
    private IEnumerator HitFlashCoroutine()
    {
        if (enemyImage != null)
        {
            // 1. 赤く点滅
            enemyImage.color = hitColor;
            yield return new WaitForSeconds(hitDuration);

            // 2. 元の色に戻る
            enemyImage.color = originalColor;
        }
    }

    // --- コルーチン 2: 揺れアニメーション ---
    private IEnumerator ShakeCoroutine()
    {
        float timer = 0f;
        
        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;
            
            // 進行度 (0.0 -> 1.0)
            float progress = timer / shakeDuration;

            // 揺れのランダムオフセットを生成 (ノックバックなので、一方向に勢いづけるのも効果的)
            // この例では、単純な左右の振動
            float xOffset = Mathf.Sin(progress * Mathf.PI * 4) * shakeDistance * (1f - progress);
            
            // 最終位置を更新 (元の位置 + オフセット)
            transform.localPosition = originalPosition + new Vector3(xOffset, 0, 0);

            yield return null;
        }

        // アニメーション終了後、元の位置に戻す
        transform.localPosition = originalPosition;
    }
}
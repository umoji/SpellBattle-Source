using UnityEngine;
using TMPro;
using System.Collections; // コルーチンを使うために必要

public class DamageTextController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textComponent;

    // --- 動作設定 ---
    [Header("Movement & Duration")]
    public float moveSpeed = 3.0f;          // 最初の勢い（速度）
    public float disappearTime = 0.8f;      // フェードアウトが始まるまでの時間
    public float animationDuration = 1.2f;  // 全体の移動にかける時間（disappearTimeより長く）
    public float fadeSpeed = 3.0f;          // フェードアウトの速さ

    // --- ランダム方向 ---
    [Header("Random Direction")]
    public float maxRandomX = 0.5f;         // 横方向の最大ブレ幅 (アニメーション用)
    private Vector3 randomDirection;         // ランダムな移動ベクトル

    // --- ランダム位置調整 ---
    [Header("Random Position Offset")]
    public float maxPositionXOffset = 50f; // X軸方向の最大ブレ幅 (出現位置のオフセット)
    public float maxPositionYOffset = 25f; // Y軸方向の最大ブレ幅 (出現位置のオフセット)

    // --- スケーリング (サイズ変化) ---
    [Header("Scaling")]
    public float scaleUpDuration = 0.1f;    // 拡大にかける時間
    public float maxScale = 1.2f;           // 最大拡大率 (1.2 = 120%)

    private float timer;
    private Color startColor;

    void Awake()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }

        if (textComponent != null)
        {
            startColor = textComponent.color;
        }
        
        timer = 0f;

        // 1. --- ランダムな位置オフセットを適用 ---
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float offsetX = Random.Range(-maxPositionXOffset, maxPositionXOffset);
            float offsetY = Random.Range(-maxPositionYOffset, maxPositionYOffset);

            // 現在のlocalPosition (Prefabで設定された画面固定位置) に対してオフセットを加える
            rectTransform.localPosition += new Vector3(offsetX, offsetY, 0f);
        }

        // 2. --- ランダム方向の初期設定 (アニメーションの方向用) ---
        float randomX = Random.Range(-maxRandomX, maxRandomX);
        randomDirection = (Vector3.up * 1f + new Vector3(randomX, 0f, 0f)).normalized;
    }

    void Start()
    {
        // 出現時のサイズ拡大アニメーションを開始
        StartCoroutine(ScaleAnimation());
    }

    /// <summary>
    /// 表示するダメージ値を設定する
    /// </summary>
    public void SetDamageValue(int damage)
    {
        if (textComponent != null)
        {
            textComponent.text = damage.ToString();
        }
    }

void Update()
    {
        timer += Time.deltaTime;
        
        // 1. 移動の緩急計算 (Ease Out)
        float progress = Mathf.Min(timer / animationDuration, 1f); 
        float currentSpeedFactor = 1f - progress; // シンプルな線形減衰

        // 2. 移動処理
        transform.position += randomDirection * moveSpeed * currentSpeedFactor * Time.deltaTime;

        // 3. フェードアウト処理
        if (timer >= disappearTime)
        {
            // Time.deltaTimeを使用して、フレームレートに依存しないスムーズな減衰を実現
            float alphaDelta = fadeSpeed * Time.deltaTime; 
            
            Color currentColor = textComponent.color;
            currentColor.a -= alphaDelta; // アルファ値を減らす
            textComponent.color = currentColor;
            
            if (currentColor.a <= 0f)
            {
                // ★修正箇所★: クローンされた Canvas ルート（PlayerDamageTextCanvas(Clone)）を破棄
                // スクリプトがアタッチされているオブジェクトの親を破棄することで、メインの Canvas は維持される。
                if (transform.parent != null)
                {
                    Destroy(transform.parent.gameObject);
                }
                else
                {
                    // 万が一親がない場合（通常は発生しない）、自分自身を破棄
                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// テキストのサイズを拡大・縮小させるコルーチン
    /// </summary>
    private IEnumerator ScaleAnimation()
    {
        float t = 0;
        Vector3 startScale = Vector3.one;
        Vector3 peakScale = Vector3.one * maxScale;

        // 拡大フェーズ
        while (t < scaleUpDuration)
        {
            t += Time.deltaTime;
            float progress = t / scaleUpDuration;
            transform.localScale = Vector3.Lerp(startScale, peakScale, progress);
            yield return null;
        }

        // 頂点に達したら、元のサイズに戻る（縮小フェーズ）
        t = 0;
        while (t < scaleUpDuration)
        {
            t += Time.deltaTime;
            float progress = t / scaleUpDuration;
            transform.localScale = Vector3.Lerp(peakScale, startScale, progress);
            yield return null;
        }

        transform.localScale = startScale; // 最終的に元のサイズに確定
    }
}
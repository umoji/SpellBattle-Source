using UnityEngine;
using TMPro;
using System.Collections;

public class DamageTextController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textComponent;

    [Header("Movement & Duration")]
    public float moveSpeed = 100.0f;        
    public float disappearTime = 0.8f;      
    public float animationDuration = 1.2f;  
    public float fadeSpeed = 3.0f;          

    [Header("Random Direction")]
    public float maxRandomX = 0.5f;         
    private Vector3 randomDirection;         

    [Header("Random Position Offset")]
    public float maxPositionXOffset = 0f; 
    public float maxPositionYOffset = 0f; 

    [Header("Scaling")]
    public float scaleUpDuration = 0.1f;    
    public float maxScale = 1.2f;

    [Header("Behavior Settings")]
    public bool autoDestroy = true; 

    private float timer;
    private Color startColor;

    // ★追加：BattleManagerから呼び出されるダメージ設定用関数
    public void SetDamageValue(int damage)
    {
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
        
        if (textComponent != null)
        {
            textComponent.text = damage.ToString();
        }
    }

    void Awake()
    {
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent != null) startColor = textComponent.color;
        timer = 0f;

        float randomX = Random.Range(-maxRandomX, maxRandomX);
        randomDirection = (Vector3.up * 1f + new Vector3(randomX, 0f, 0f)).normalized;
    }

    void Start()
    {
        StartCoroutine(ScaleAnimation());
        
        if (autoDestroy)
        {
            StartCoroutine(DelayedDestroy());
        }
    }

    // スケールアニメーションのコルーチン（元のコードに必要と思われるため追加）
    private IEnumerator ScaleAnimation()
    {
        Vector3 originalScale = transform.localScale;
        float t = 0;
        while (t < scaleUpDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * maxScale, t / scaleUpDuration);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(disappearTime);
        yield return FadeAndDestroyRoutine();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Min(timer / animationDuration, 1f); 
        float currentSpeedFactor = 1f - progress; 
        transform.localPosition += randomDirection * moveSpeed * currentSpeedFactor * Time.deltaTime;
    }

    public void DestroyWithFade()
    {
        StopAllCoroutines(); 
        StartCoroutine(FadeAndDestroyRoutine());
    }

    private IEnumerator FadeAndDestroyRoutine()
    {
        float t = 0;
        if (textComponent == null) yield break;
        
        Color c = textComponent.color;
        while (t < 0.3f) 
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1, 0, t / 0.3f);
            textComponent.color = c;
            yield return null;
        }
        
        if (transform.parent != null) Destroy(transform.parent.gameObject);
        else Destroy(gameObject);
    }
}
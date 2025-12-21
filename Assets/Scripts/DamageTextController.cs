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
    
    [Header("Scaling")]
    public float scaleUpDuration = 0.1f;    
    public float maxScale = 1.2f;

    [Header("Behavior Settings")]
    public bool autoDestroy = true; 

    private float timer;

    public void SetDamageValue(int damage)
    {
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent != null) textComponent.text = damage.ToString();
    }

    void Awake()
    {
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
        timer = 0f;
    }

	void Start()
	{
		StartCoroutine(ScaleAnimation());
		
		// ★修正：フラグをチェックして、外部管理(false)の時は勝手に消えないようにする
		if (autoDestroy) 
		{
			StartCoroutine(DelayedDestroy());
		}
	}

    private IEnumerator ScaleAnimation()
    {
        Vector3 originalScale = Vector3.one;
        transform.localScale = Vector3.zero; 
        float t = 0;
        while (t < scaleUpDuration)
        {
            t += Time.deltaTime;
            float progress = t / scaleUpDuration;
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale * maxScale, progress);
            yield return null;
        }
        transform.localScale = originalScale;
    }

	private IEnumerator DelayedDestroy()
	{
		// autoDestroy が true の時だけ呼ばれるタイマー
		yield return new WaitForSeconds(disappearTime);
		yield return FadeAndDestroyRoutine();
	}

	void Update()
	{
		// 移動だけを管理。ここには Destroy や Fade の処理は含めない
		timer += Time.deltaTime;
		float progress = Mathf.Min(timer / animationDuration, 1f);
		float currentSpeedFactor = 1f - progress;

		transform.localPosition += Vector3.up * moveSpeed * currentSpeedFactor * Time.deltaTime;
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
        // 親（Canvas付きPrefabの場合）ごと消す
        if (transform.parent != null) Destroy(transform.parent.gameObject);
        else Destroy(gameObject);
    }
}
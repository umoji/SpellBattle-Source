using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using System.Linq; 
using System; 

public class BattleManager : MonoBehaviour
{
    // --- 既存の変数 ---
    [Header("UI References")]
    public TextMeshProUGUI numberText; 
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText; 
    public Image playerHPFillImage; 
    public Image enemyHPFillImage; 
    public TextMeshProUGUI enemyNameText;
    public Button actionButton; 
    public RectTransform handArea; 
    public Transform damageTextParent; 
    public RectTransform mainCanvasRect; // ★追加：CanvasのRectTransformをアサイン

    [Header("Combo Display UI (Left Side)")]
    public RectTransform baseDamagePanel; 
    public RectTransform multiplierPanel;
    public TextMeshProUGUI totalBaseDamageText;    
    public TextMeshProUGUI totalMultiplierText;    

    [Header("Animation References")]
    public GameObject enemyDamagePrefab;   
    public GameObject playerDamagePrefab;  
    public GameObject multiplierTextPrefab;
    public EnemyAnimator enemyAnimator;
    public Transform enemyDamageAnchor; 
    public Transform playerDamageAnchor; 

    [Header("Card UI References")]
    public GameObject cardUIPrefab; 

    [Header("Game End UI")]
    public GameObject gameEndPanel;          
    public TextMeshProUGUI resultText;      
    public Button returnToHomeButton;        

    [Header("Damage Flash & Shake Settings")]
    public Image damageFlashImage; 
    public float flashDuration = 0.2f; 
    public Color flashColor = new Color(1f, 0f, 0f, 0.4f);
    public float screenShakeDuration = 0.3f; // ★シェイクの時間
    public float screenShakeMagnitude = 15f; // ★シェイクの強さ

    private List<CardUI> selectedCards = new List<CardUI>(); 
    private int currentTurnCount = 0;
    private bool isPlayerTurn = false;

    [Header("Data & Manager References")]
    public BattleDataContainer dataContainer; 
    private SceneLoader sceneLoader; 
    private const int MAX_HAND_SIZE = 10; 

    [Header("Combo Settings")]
    public float bonusPerComboCard = 0.5f; 
    public Color normalColor = Color.white;         
    public Color midComboColor = new Color(1f, 0.6f, 0f); 
    public Color maxComboColor = new Color(1f, 0.2f, 0f); 

    [Header("Shake Settings")]
    public float shakeDuration = 0.15f; 
    public float baseShakeMagnitude = 5.0f; 
    public float maxShakeMagnitude = 30.0f; 

    [Header("Text Size Settings")]
    public float minFontSize = 80f;   
    public float maxFontSize = 150f;
	
	[Header("Shake Parent")]
	// 今までの mainCanvasRect の代わりにこれを使います
	public RectTransform shakeParentRect;

    private Vector3 basePanelOriginPos;
    private Vector3 multiPanelOriginPos;
    private Vector3 canvasOriginPos; // ★Canvasの初期位置

    private Coroutine playerBarCoroutine;
    private Coroutine enemyBarCoroutine;

    void Start()
    {
        if (dataContainer == null) return;
        sceneLoader = SceneLoader.Instance; 
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (returnToHomeButton != null) returnToHomeButton.onClick.AddListener(OnEndScreenClicked);
        
        if (baseDamagePanel != null) basePanelOriginPos = baseDamagePanel.localPosition;
        if (multiplierPanel != null) multiPanelOriginPos = multiplierPanel.localPosition;
        
        // ★Canvasの初期位置を記録
        if (mainCanvasRect != null) canvasOriginPos = mainCanvasRect.localPosition;

        UpdateComboUI(0, 1.0f);

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(ExecutePlayerAttack);
            var buttonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Attack!";
            actionButton.interactable = false;
        }
        StartBattle();
    }

	private IEnumerator ScreenShake()
	{
		// shakeParentRect がアサインされていない場合は何もしない
		if (shakeParentRect == null) yield break;

		float elapsed = 0f;
		// 親オブジェクトの初期位置を保存
		Vector2 originalPos = shakeParentRect.anchoredPosition;

		while (elapsed < screenShakeDuration)
		{
			elapsed += Time.deltaTime;

			// 揺れの強さを時間の経過とともに弱くしていく（減衰）
			float strength = screenShakeMagnitude * (1f - (elapsed / screenShakeDuration));

			// ランダムな方向にずらす
			float x = UnityEngine.Random.Range(-1f, 1f) * strength;
			float y = UnityEngine.Random.Range(-1f, 1f) * strength;

			// アンカーポジションを直接操作して揺らす
			shakeParentRect.anchoredPosition = originalPos + new Vector2(x, y);

			yield return null;
		}

		// 最後にピタッと元の位置（0,0）に戻す
		shakeParentRect.anchoredPosition = originalPos;
	}

    private void ApplyDamageToPlayer(int d) 
    { 
        if (dataContainer.playerData != null) 
        { 
            dataContainer.playerData.currentHP -= d; 
            if (dataContainer.playerData.currentHP < 0) dataContainer.playerData.currentHP = 0; 
            
            StartCoroutine(FlashDamageEffect());
            StartCoroutine(ScreenShake()); // ★追加：ダメージ時に画面を揺らす

            if (playerDamagePrefab != null && playerDamageAnchor != null) 
                ShowTextAtTargetPerfectly(playerDamagePrefab, playerDamageAnchor, d.ToString(), Color.white, true); 
            
            UpdateUI();
        } 
    }

    private void ApplyDamageToEnemy(int damage, Color comboColor) 
    { 
        if (dataContainer.enemyData != null) 
        { 
            dataContainer.enemyData.currentHP -= damage; 
            if (dataContainer.enemyData.currentHP < 0) dataContainer.enemyData.currentHP = 0; 
            
            if (enemyAnimator != null) enemyAnimator.PlayHitAnimation(); 
            StartCoroutine(ScreenShake()); // ★追加：敵を殴った時も少し揺らすと気持ちいい

            if (enemyDamageAnchor != null)
            {
                GameObject dtObj = ShowTextAtTargetPerfectly(enemyDamagePrefab, enemyDamageAnchor, damage.ToString(), comboColor, true);
                // (以下、テキスト演出コード...省略なしで適用してください)
                if (dtObj != null)
                {
                    TextMeshProUGUI textComp = dtObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComp != null)
                    {
                        textComp.enableWordWrapping = false;
                        textComp.enableAutoSizing = true;
                        textComp.fontSizeMin = 40; 
                        textComp.fontSizeMax = maxFontSize * 1.5f;
                        textComp.alignment = TextAlignmentOptions.Center;
                        textComp.fontStyle = FontStyles.Bold;
                    }
                    dtObj.transform.localPosition += new Vector3(0, 100f, 0);
                }
            }
        } 
    }

    // (以下、SmoothUpdateHPBarなどの既存メソッド... 以前のものをそのまま維持)
    private IEnumerator SmoothUpdateHPBar(Image fillImage, TextMeshProUGUI hpText, int currentHP, int maxHP)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        float startFill = fillImage.fillAmount;
        float targetFill = (float)currentHP / maxHP;
        int startHPValue;
        if (!int.TryParse(hpText.text.Split('/')[0].Trim(), out startHPValue)) startHPValue = currentHP;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f); 
            fillImage.fillAmount = Mathf.Lerp(startFill, targetFill, easedT);
            int displayHP = Mathf.RoundToInt(Mathf.Lerp(startHPValue, currentHP, easedT));
            hpText.text = $"{displayHP} / {maxHP}";
            yield return null;
        }
        fillImage.fillAmount = targetFill;
        hpText.text = $"{currentHP} / {maxHP}";
    }

    private IEnumerator FlashDamageEffect()
    {
        if (damageFlashImage == null) yield break;
        damageFlashImage.color = flashColor;
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(flashColor.a, 0f, elapsed / flashDuration);
            damageFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        damageFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }

    // --- その他、以前からある全メソッドをそのまま含めてください ---
    public void ExecutePlayerAttack() { if (selectedCards.Count == 0 || !isPlayerTurn) return; StartCoroutine(ComboAttackRoutine()); }
    private IEnumerator ComboAttackRoutine() { /* 以前のロジックそのまま */ 
        isPlayerTurn = false; actionButton.interactable = false;
        int accumulatedBaseDamage = 0; float accumulatedMultiplier = 1.0f;
        List<GameObject> cardsToRemove = new List<GameObject>();
        List<DamageTextController> activeDamageTexts = new List<DamageTextController>();
        int consecutiveNumbers = 1; int consecutiveAttributes = 1; CardData lastCard = null;

        for (int i = 0; i < selectedCards.Count; i++) {
            CardUI cardUI = selectedCards[i]; CardData currentCard = cardUI.GetCardData();
            int cardBase = currentCard.Number * 10; accumulatedBaseDamage += cardBase;
            bool isCombo = false;
            if (lastCard != null) {
                if (currentCard.Number == lastCard.Number) consecutiveNumbers++; else consecutiveNumbers = 1;
                if (currentCard.Attribute == lastCard.Attribute) consecutiveAttributes++; else consecutiveAttributes = 1;
                if (consecutiveNumbers >= 3 || consecutiveAttributes >= 3) { accumulatedMultiplier += bonusPerComboCard; isCombo = true; }
            }
            UpdateComboUI(accumulatedBaseDamage, accumulatedMultiplier);
            RectTransform cardRect = cardUI.GetComponent<RectTransform>();
            if (cardRect != null) StartCoroutine(ShakeCardUI(cardRect, baseShakeMagnitude * accumulatedMultiplier));
            Color currentColor = GetComboColor(accumulatedMultiplier);
            GameObject dtObj = ShowTextAtTargetPerfectly(enemyDamagePrefab, cardUI.transform, cardBase.ToString(), currentColor, false);
            if (dtObj != null) { dtObj.transform.localPosition += new Vector3(0, 120f, 0); DamageTextController ctrl = dtObj.GetComponentInChildren<DamageTextController>(); if (ctrl != null) activeDamageTexts.Add(ctrl); }
            if (isCombo && multiplierTextPrefab != null) {
                GameObject multiObj = ShowTextAtTargetPerfectly(multiplierTextPrefab, cardUI.transform, $"+{bonusPerComboCard}", currentColor, true);
                if (multiObj != null) { multiObj.transform.localPosition += new Vector3(0, 200f, 0); multiObj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f); }
            }
            cardsToRemove.Add(cardUI.gameObject);
            if (dataContainer.playerData.hand.Remove(currentCard)) dataContainer.playerData.discardPile.Add(currentCard.CardID);
            lastCard = currentCard; yield return new WaitForSeconds(0.9f);
        }
        yield return new WaitForSeconds(0.8f);
        foreach (var dt in activeDamageTexts) if (dt != null) dt.DestroyWithFade();
        yield return new WaitForSeconds(0.2f);
        foreach (var cardObj in cardsToRemove) Destroy(cardObj);
        int finalDamage = Mathf.RoundToInt(accumulatedBaseDamage * accumulatedMultiplier);
        ApplyDamageToEnemy(finalDamage, GetComboColor(accumulatedMultiplier));
        UpdateComboUI(0, 1.0f); selectedCards.Clear(); UpdateUI();
        if (dataContainer.enemyData.currentHP > 0) EndTurn(); else { yield return new WaitForSeconds(1.0f); CheckGameEnd(); }
    }

    private IEnumerator ShakeCardUI(RectTransform target, float magnitude) { if (target == null) yield break; Vector3 originalPos = target.localPosition; float elapsed = 0f; Vector3 punchScale = new Vector3(1.1f, 1.1f, 1.1f); target.localScale = punchScale; while (elapsed < shakeDuration) { elapsed += Time.deltaTime; float percent = elapsed / shakeDuration; float x = UnityEngine.Random.Range(-1f, 1f) * magnitude; float y = UnityEngine.Random.Range(-1f, 1f) * magnitude; target.localPosition = originalPos + new Vector3(x, y, 0); target.localScale = Vector3.Lerp(punchScale, Vector3.one, percent); yield return null; } target.localPosition = originalPos; target.localScale = Vector3.one; }
    private Color GetComboColor(float multiplier) { if (multiplier <= 1.0f) return normalColor; float t = Mathf.InverseLerp(1.0f, 2.0f, multiplier); if (t < 0.5f) return Color.Lerp(normalColor, midComboColor, t * 2f); else return Color.Lerp(midComboColor, maxComboColor, (t - 0.5f) * 2f); }
    private void UpdateComboUI(int baseDmg, float multi) { Color currentColor = GetComboColor(multi); float t = Mathf.InverseLerp(1.0f, 2.0f, multi); float currentFontSize = Mathf.Lerp(minFontSize, maxFontSize, t); float currentShakeMag = Mathf.Lerp(baseShakeMagnitude, maxShakeMagnitude, t); if (totalBaseDamageText != null) { totalBaseDamageText.text = baseDmg.ToString(); totalBaseDamageText.color = currentColor; totalBaseDamageText.fontSize = currentFontSize; if(baseDmg > 0 && baseDamagePanel != null) StartCoroutine(ShakeUI(baseDamagePanel, basePanelOriginPos, currentShakeMag)); } if (totalMultiplierText != null) { totalMultiplierText.text = $"x{multi:F1}"; totalMultiplierText.color = currentColor; totalMultiplierText.fontSize = currentFontSize; if(multi > 1.0f && multiplierPanel != null) StartCoroutine(ShakeUI(multiplierPanel, multiPanelOriginPos, currentShakeMag)); } }
    private IEnumerator ShakeUI(RectTransform target, Vector3 originPos, float magnitude) { float elapsed = 0f; Vector3 punchScale = new Vector3(1.15f, 1.15f, 1.15f); target.localScale = punchScale; while (elapsed < shakeDuration) { elapsed += Time.deltaTime; float percent = elapsed / shakeDuration; float x = UnityEngine.Random.Range(-1f, 1f) * magnitude; float y = UnityEngine.Random.Range(-1f, 1f) * magnitude; target.localPosition = originPos + new Vector3(x, y, 0); target.localScale = Vector3.Lerp(punchScale, Vector3.one, percent); yield return null; } target.localPosition = originPos; target.localScale = Vector3.one; }
    private GameObject ShowTextAtTargetPerfectly(GameObject prefab, Transform targetTransform, string textValue, Color textColor, bool autoDestroy) { if (prefab == null || targetTransform == null) return null; GameObject inst = Instantiate(prefab); inst.transform.position = targetTransform.position; inst.transform.SetParent(damageTextParent, true); inst.transform.localScale = Vector3.one; Vector3 lp = inst.transform.localPosition; lp.z = 0; inst.transform.localPosition = lp; TextMeshProUGUI textComp = inst.GetComponentInChildren<TextMeshProUGUI>(); if (textComp != null) { textComp.text = textValue; textComp.color = textColor; } DamageTextController controller = inst.GetComponentInChildren<DamageTextController>(); if (controller != null) controller.autoDestroy = autoDestroy; return inst; }
    public void StartBattle() { EnemyData s = EnemyData.GetFixedDataByID(1); if (s != null) dataContainer.enemyData = new EnemyBattleData(s); if (CardManager.Instance != null) { dataContainer.playerData.deck.Clear(); dataContainer.playerData.hand.Clear(); dataContainer.playerData.discardPile.Clear(); dataContainer.playerData.deck.AddRange(CardManager.Instance.mainDeckCardIDs); ShuffleDeckIDs(); } DrawCards(5); StartPlayerTurn(); }
    private void StartPlayerTurn() { isPlayerTurn = true; currentTurnCount++; if (currentTurnCount > 1) { int r = 5 - dataContainer.playerData.hand.Count; if (r > 0) DrawCards(r); if (dataContainer.playerData.hand.Count < MAX_HAND_SIZE) DrawCards(1); } UpdateUI(); }
    private void EndTurn() { isPlayerTurn = false; selectedCards.Clear(); UpdateUI(); StartCoroutine(EnemyTurnCoroutine()); }
    private IEnumerator EnemyTurnCoroutine() { yield return new WaitForSeconds(1.5f); ApplyDamageToPlayer(30); UpdateUI(); CheckGameEnd(); if (dataContainer.playerData.currentHP > 0 && dataContainer.enemyData.currentHP > 0) StartPlayerTurn(); }
    private void UpdateUI() { if (dataContainer == null) return; if (numberText != null) numberText.text = $"Turn: {currentTurnCount}"; if (playerHPFillImage != null && playerHPText != null) { if (playerBarCoroutine != null) StopCoroutine(playerBarCoroutine); playerBarCoroutine = StartCoroutine(SmoothUpdateHPBar(playerHPFillImage, playerHPText, dataContainer.playerData.currentHP, dataContainer.playerData.maxHP)); } if (dataContainer.enemyData != null && dataContainer.enemyData.enemyData != null) { if (enemyHPFillImage != null && enemyHPText != null) { if (enemyBarCoroutine != null) StopCoroutine(enemyBarCoroutine); enemyBarCoroutine = StartCoroutine(SmoothUpdateHPBar(enemyHPFillImage, enemyHPText, dataContainer.enemyData.currentHP, dataContainer.enemyData.enemyData.MaxHP)); } enemyNameText.text = dataContainer.enemyData.enemyData.EnemyName; } RefreshHandVisuals(); if (actionButton != null) actionButton.interactable = (isPlayerTurn && selectedCards.Count > 0); }
    private void RefreshHandVisuals() { CardUI[] cards = handArea.GetComponentsInChildren<CardUI>(); foreach (CardUI c in cards) { if (selectedCards.Contains(c)) { c.SetAvailableState(true); continue; } if (selectedCards.Count == 0) c.SetAvailableState(true); else { CardData last = selectedCards[selectedCards.Count - 1].GetCardData(); CardData curr = c.GetCardData(); bool connect = (curr.Number == last.Number || curr.Attribute == last.Attribute); c.SetAvailableState(connect); } } }
    private void DrawCards(int c) { for (int i = 0; i < c; i++) { if (dataContainer.playerData.hand.Count >= MAX_HAND_SIZE) break; if (dataContainer.playerData.deck.Count == 0) { if (dataContainer.playerData.discardPile.Count > 0) { dataContainer.playerData.deck.AddRange(dataContainer.playerData.discardPile); dataContainer.playerData.discardPile.Clear(); ShuffleDeckIDs(); } else break; } int id = dataContainer.playerData.deck[0]; dataContainer.playerData.deck.RemoveAt(0); CardData d = CardManager.Instance.GetCardDataByID(id); dataContainer.playerData.hand.Add(d); if (cardUIPrefab != null && handArea != null) { GameObject obj = Instantiate(cardUIPrefab, handArea); obj.GetComponent<CardUI>().SetupCard(d, this); } } }
    private void ShuffleDeckIDs() { List<int> d = dataContainer.playerData.deck; System.Random r = new System.Random(); int n = d.Count; while (n > 1) { n--; int k = r.Next(n + 1); int v = d[k]; d[k] = d[n]; d[n] = v; } }
    private void CheckGameEnd() { if (dataContainer.playerData.currentHP <= 0) EndGame("Lose"); else if (dataContainer.enemyData.currentHP <= 0) EndGame("Win"); }
    private void EndGame(string r) { isPlayerTurn = false; if (gameEndPanel != null) { gameEndPanel.SetActive(true); resultText.text = r; } }
    private void OnEndScreenClicked() { if (sceneLoader != null) sceneLoader.LoadHomeScene(); }
    public bool OnCardSelected(CardUI c) { if (!isPlayerTurn) return false; CardData n = c.GetCardData(); if (selectedCards.Count > 0) { CardData l = selectedCards[selectedCards.Count - 1].GetCardData(); if (n.Number != l.Number && n.Attribute != l.Attribute) return false; } selectedCards.Add(c); UpdateUI(); return true; }
    public void OnCardDeselected(CardUI c) { int i = selectedCards.IndexOf(c); if (i != -1) { for (int j = selectedCards.Count - 1; j >= i; j--) { selectedCards[j].ResetPosition(); selectedCards.RemoveAt(j); } } UpdateUI(); }
}
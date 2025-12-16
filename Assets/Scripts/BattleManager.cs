using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using System.Linq; 
using System; 

public class BattleManager : MonoBehaviour
{
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
    
    private List<CardUI> selectedCards = new List<CardUI>(); 
    private int currentTurnCount = 0;
    private bool isPlayerTurn = false;

    [Header("Data & Manager References")]
    public BattleDataContainer dataContainer; 
    private SceneLoader sceneLoader; 
    private const int MAX_HAND_SIZE = 10; 

    [Header("Combo Settings")]
    public float comboMultiplier = 1.5f; 
    // オレンジ色の定義（R=1, G=0.5, B=0）
    private Color comboColor = new Color(1.0f, 0.5f, 0.0f);

    void Start()
    {
        if (dataContainer == null) return;
        sceneLoader = SceneLoader.Instance; 
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (returnToHomeButton != null) returnToHomeButton.onClick.AddListener(OnEndScreenClicked);
        
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(ExecutePlayerAttack);
            var buttonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Attack!";
            actionButton.interactable = false;
        }
        StartBattle();
    }

    public void ExecutePlayerAttack()
    {
        if (selectedCards.Count == 0 || !isPlayerTurn) return;
        StartCoroutine(ComboAttackRoutine());
    }

    private IEnumerator ComboAttackRoutine()
    {
        isPlayerTurn = false; 
        actionButton.interactable = false;

        float totalDamageFloat = 0;
        List<GameObject> cardsToRemove = new List<GameObject>();
        List<DamageTextController> activeDamageTexts = new List<DamageTextController>();

        int consecutiveNumbers = 1;
        int consecutiveAttributes = 1;
        CardData lastCard = null;

        for (int i = 0; i < selectedCards.Count; i++)
        {
            CardUI cardUI = selectedCards[i];
            CardData currentCard = cardUI.GetCardData();
            int baseCardDamage = currentCard.Number * 10;
            float currentMultiplier = 1.0f;
            bool isCombo = false;

            if (lastCard != null)
            {
                if (currentCard.Number == lastCard.Number) consecutiveNumbers++;
                else consecutiveNumbers = 1;

                if (currentCard.Attribute == lastCard.Attribute) consecutiveAttributes++;
                else consecutiveAttributes = 1;

                if (consecutiveNumbers >= 3 || consecutiveAttributes >= 3)
                {
                    currentMultiplier = comboMultiplier;
                    isCombo = true;
                }
            }

            int finalCardDamage = Mathf.RoundToInt(baseCardDamage * currentMultiplier);
            totalDamageFloat += finalCardDamage;

            // ★修正：コンボ中ならオレンジ、通常なら白を指定
            Color dColor = isCombo ? comboColor : Color.white;

            // 1. ダメージテキストを表示（色を指定）
            GameObject dtObj = ShowTextAtTargetPerfectly(enemyDamagePrefab, cardUI.transform, finalCardDamage.ToString(), dColor, false); 
            if (dtObj != null)
            {
                DamageTextController ctrl = dtObj.GetComponentInChildren<DamageTextController>();
                if (ctrl != null) activeDamageTexts.Add(ctrl);
            }

            // 2. コンボ中なら倍率テキストを表示
			if (isCombo && multiplierTextPrefab != null)
			{
				// 倍率テキストを表示
				GameObject multiObj = ShowTextAtTargetPerfectly(multiplierTextPrefab, cardUI.transform, $"x{currentMultiplier}!!", comboColor, true);
				if (multiObj != null)
				{
					// ① 位置をさらに上に（50f を 80f などに増やす）
					multiObj.transform.localPosition += new Vector3(0, 80f, 0);

					// ② 軽く斜めにする（Z軸を 15度 ほど傾ける）
					multiObj.transform.localRotation = Quaternion.Euler(0, 0, 15f);
				}
			}

            CanvasGroup cg = cardUI.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0; 
            cardsToRemove.Add(cardUI.gameObject);

            if (dataContainer.playerData.hand.Remove(currentCard))
                dataContainer.playerData.discardPile.Add(currentCard.CardID);

            lastCard = currentCard;
            yield return new WaitForSeconds(0.25f);
        }

        yield return new WaitForSeconds(0.5f);

        foreach (var dt in activeDamageTexts)
        {
            if (dt != null) dt.DestroyWithFade();
        }
        yield return new WaitForSeconds(0.2f);

        foreach (var cardObj in cardsToRemove) Destroy(cardObj);

        ApplyDamageToEnemy(Mathf.RoundToInt(totalDamageFloat));

        selectedCards.Clear();
        UpdateUI();
        EndTurn();
    }

    // ★色(textColor)を受け取れるように拡張
    private GameObject ShowTextAtTargetPerfectly(GameObject prefab, Transform targetTransform, string textValue, Color textColor, bool autoDestroy)
    {
        if (prefab == null || targetTransform == null) return null;
        
        GameObject inst = Instantiate(prefab);
        inst.transform.position = targetTransform.position;
        inst.transform.SetParent(damageTextParent, true);
        inst.transform.localScale = Vector3.one;
        Vector3 lp = inst.transform.localPosition;
        lp.z = 0;
        inst.transform.localPosition = lp;

        // テキストコンポーネントを取得して内容と色をセット
        TextMeshProUGUI textComp = inst.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            textComp.text = textValue;
            textComp.color = textColor; // ★ここで色を変更！
        }

        DamageTextController controller = inst.GetComponentInChildren<DamageTextController>();
        if (controller != null)
        {
            controller.autoDestroy = autoDestroy; 
        }
        return inst;
    }

    private void ApplyDamageToEnemy(int damage) 
    { 
        if (dataContainer.enemyData != null) 
        { 
            dataContainer.enemyData.currentHP -= damage; 
            if (dataContainer.enemyData.currentHP < 0) dataContainer.enemyData.currentHP = 0; 
            if (enemyAnimator != null) enemyAnimator.PlayHitAnimation(); 
            if (enemyDamageAnchor != null)
            {
                // 敵へのダメージ（最終ダメージ）は白で表示（コンボ時はここもオレンジにしたい場合は変えられます）
                ShowTextAtTargetPerfectly(enemyDamagePrefab, enemyDamageAnchor, damage.ToString(), Color.white, true);
            }
        } 
    }

    private void ApplyDamageToPlayer(int d) 
    { 
        if (dataContainer.playerData != null) 
        { 
            dataContainer.playerData.currentHP -= d; 
            if (dataContainer.playerData.currentHP < 0) dataContainer.playerData.currentHP = 0; 
            if (playerDamagePrefab != null && playerDamageAnchor != null) 
            { 
                ShowTextAtTargetPerfectly(playerDamagePrefab, playerDamageAnchor, d.ToString(), Color.white, true); 
            } 
        } 
    }

    // --- 既存ロジック ---
    public void StartBattle() { EnemyData s = EnemyData.GetFixedDataByID(1); if (s != null) dataContainer.enemyData = new EnemyBattleData(s); if (CardManager.Instance != null) { dataContainer.playerData.deck.Clear(); dataContainer.playerData.hand.Clear(); dataContainer.playerData.discardPile.Clear(); dataContainer.playerData.deck.AddRange(CardManager.Instance.mainDeckCardIDs); ShuffleDeckIDs(); } DrawCards(5); StartPlayerTurn(); }
    private void StartPlayerTurn() { isPlayerTurn = true; currentTurnCount++; if (currentTurnCount > 1) { int r = 5 - dataContainer.playerData.hand.Count; if (r > 0) DrawCards(r); if (dataContainer.playerData.hand.Count < MAX_HAND_SIZE) DrawCards(1); } UpdateUI(); }
    private void EndTurn() { isPlayerTurn = false; selectedCards.Clear(); UpdateUI(); StartCoroutine(EnemyTurnCoroutine()); }
    private IEnumerator EnemyTurnCoroutine() { yield return new WaitForSeconds(1.5f); ApplyDamageToPlayer(30); UpdateUI(); CheckGameEnd(); if (dataContainer.playerData.currentHP > 0 && dataContainer.enemyData.currentHP > 0) StartPlayerTurn(); }
    private void UpdateUI() { if (dataContainer == null) return; if (numberText != null) numberText.text = $"Turn: {currentTurnCount}"; if (playerHPText != null) playerHPText.text = $"{dataContainer.playerData.currentHP} / {dataContainer.playerData.maxHP}"; if (playerHPFillImage != null) playerHPFillImage.fillAmount = (float)dataContainer.playerData.currentHP / dataContainer.playerData.maxHP; if (dataContainer.enemyData != null && dataContainer.enemyData.enemyData != null) { enemyHPText.text = $"{dataContainer.enemyData.currentHP} / {dataContainer.enemyData.enemyData.MaxHP}"; enemyHPFillImage.fillAmount = (float)dataContainer.enemyData.currentHP / dataContainer.enemyData.enemyData.MaxHP; enemyNameText.text = dataContainer.enemyData.enemyData.EnemyName; } RefreshHandVisuals(); if (actionButton != null) actionButton.interactable = (isPlayerTurn && selectedCards.Count > 0); }
    private void RefreshHandVisuals() { CardUI[] cards = handArea.GetComponentsInChildren<CardUI>(); foreach (CardUI c in cards) { if (selectedCards.Contains(c)) { c.SetAvailableState(true); continue; } if (selectedCards.Count == 0) c.SetAvailableState(true); else { CardData last = selectedCards[selectedCards.Count - 1].GetCardData(); CardData curr = c.GetCardData(); bool connect = (curr.Number == last.Number || curr.Attribute == last.Attribute); c.SetAvailableState(connect); } } }
    private void DrawCards(int c) { for (int i = 0; i < c; i++) { if (dataContainer.playerData.hand.Count >= MAX_HAND_SIZE) break; if (dataContainer.playerData.deck.Count == 0) { if (dataContainer.playerData.discardPile.Count > 0) { dataContainer.playerData.deck.AddRange(dataContainer.playerData.discardPile); dataContainer.playerData.discardPile.Clear(); ShuffleDeckIDs(); } else break; } int id = dataContainer.playerData.deck[0]; dataContainer.playerData.deck.RemoveAt(0); CardData d = CardManager.Instance.GetCardDataByID(id); dataContainer.playerData.hand.Add(d); if (cardUIPrefab != null && handArea != null) { GameObject obj = Instantiate(cardUIPrefab, handArea); obj.GetComponent<CardUI>().SetupCard(d, this); } } }
    private void ShuffleDeckIDs() { List<int> d = dataContainer.playerData.deck; System.Random r = new System.Random(); int n = d.Count; while (n > 1) { n--; int k = r.Next(n + 1); int v = d[k]; d[k] = d[n]; d[n] = v; } }
    private void CheckGameEnd() { if (dataContainer.playerData.currentHP <= 0) EndGame("Lose"); else if (dataContainer.enemyData.currentHP <= 0) EndGame("Win"); }
    private void EndGame(string r) { isPlayerTurn = false; if (gameEndPanel != null) { gameEndPanel.SetActive(true); resultText.text = r; } }
    private void OnEndScreenClicked() { if (sceneLoader != null) sceneLoader.LoadHomeScene(); }
    public bool OnCardSelected(CardUI c) { if (!isPlayerTurn) return false; CardData n = c.GetCardData(); if (selectedCards.Count > 0) { CardData l = selectedCards[selectedCards.Count - 1].GetCardData(); if (n.Number != l.Number && n.Attribute != l.Attribute) return false; } selectedCards.Add(c); UpdateUI(); return true; }
    public void OnCardDeselected(CardUI c) { int i = selectedCards.IndexOf(c); if (i != -1) { for (int j = selectedCards.Count - 1; j >= i; j--) { selectedCards[j].ResetPosition(); selectedCards.RemoveAt(j); } } UpdateUI(); }
}
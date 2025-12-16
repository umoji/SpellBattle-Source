using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using System.Linq; 
using System; 

public class BattleManager : MonoBehaviour
{
    // -------------------------------------------------------------------
    // 1. UI 参照 
    // -------------------------------------------------------------------
    [Header("UI References")]
    public TextMeshProUGUI numberText; 
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText; 
    public TextMeshProUGUI enemyNameText; 
    public Image playerHPFillImage; 
    public Image enemyHPFillImage; 
    
    // 「選択したカードを消費して、攻撃する」ためのボタン
    public Button actionButton; 
    public RectTransform handArea; 

    [Header("Animation References")]
    public ButtonFlasher buttonFlasher; 
    public GameObject enemyDamagePrefab;   
    public GameObject playerDamagePrefab;  
    
    [Header("Enemy Components")]
    public EnemyAnimator enemyAnimator;

    [Header("Player Components")]
    public Transform playerDamageAnchor; 

    [Header("Card UI References")]
    public GameObject cardUIPrefab; 

    [Header("Game End UI")]
    public GameObject gameEndPanel;         
    public TextMeshProUGUI resultText;     
    public Button returnToHomeButton;       
    
    // -------------------------------------------------------------------
    // 2. 状態管理
    // -------------------------------------------------------------------
    private List<CardUI> selectedCards = new List<CardUI>(); 
    private int currentTurnCount = 0;
    private bool isPlayerTurn = false;

    [Header("Data & Manager References")]
    public BattleDataContainer dataContainer; 
    private SceneLoader sceneLoader; 
    private const int MAX_HAND_SIZE = 10; 

    // -------------------------------------------------------------------
    // 3. Unity ライフサイクル 
    // -------------------------------------------------------------------
    void Start()
    {
        if (dataContainer == null) return;
        sceneLoader = SceneLoader.Instance; 

        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (returnToHomeButton != null) returnToHomeButton.onClick.AddListener(OnEndScreenClicked);
        
        // ボタンの設定
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(ExecutePlayerAttack);
            var buttonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Attack!";
            actionButton.interactable = false; // 最初はカード未選択なので無効
        }

        // ★シーン開始時に即座にバトルをセットアップしてカードを配る
        StartBattle();
    }

    // -------------------------------------------------------------------
    // 4. コンボ選択ロジック
    // -------------------------------------------------------------------
    
    public bool OnCardSelected(CardUI cardUI)
    {
        if (!isPlayerTurn) return false;

        CardData newCard = cardUI.GetCardData();

        if (selectedCards.Count > 0)
        {
            CardData lastCard = selectedCards[selectedCards.Count - 1].GetCardData();
            // 数字か属性が一致すれば連鎖可能
            if (newCard.Number != lastCard.Number && newCard.Attribute != lastCard.Attribute)
            {
                return false;
            }
        }

        selectedCards.Add(cardUI);
        UpdateUI(); 
        return true;
    }

    public void OnCardDeselected(CardUI cardUI)
    {
        int index = selectedCards.IndexOf(cardUI);
        if (index != -1)
        {
            for (int i = selectedCards.Count - 1; i >= index; i--)
            {
                selectedCards[i].ResetPosition();
                selectedCards.RemoveAt(i);
            }
        }
        UpdateUI();
    }

    // ★ボタンから呼ばれる攻撃実行処理
    public void ExecutePlayerAttack()
    {
        if (selectedCards.Count == 0 || !isPlayerTurn) return;

        int totalDamage = 0;
        foreach (var cardUI in selectedCards)
        {
            CardData data = cardUI.GetCardData();
            totalDamage += data.Number * 10;
            
            if (dataContainer.playerData.hand.Remove(data))
            {
                dataContainer.playerData.discardPile.Add(data.CardID);
            }
            Destroy(cardUI.gameObject);
        }

        ApplyDamageToEnemy(totalDamage);
        selectedCards.Clear();
        UpdateUI();
        EndTurn(); 
    }

    // -------------------------------------------------------------------
    // 5. バトルフロー
    // -------------------------------------------------------------------
    public void StartBattle()
    {
        // 敵の初期化
        EnemyData staticEnemyData = EnemyData.GetFixedDataByID(1);
        if (staticEnemyData != null) dataContainer.enemyData = new EnemyBattleData(staticEnemyData);

        // デッキの準備
        if (CardManager.Instance != null)
        {
            dataContainer.playerData.deck.Clear();
            dataContainer.playerData.hand.Clear();
            dataContainer.playerData.discardPile.Clear();
            dataContainer.playerData.deck.AddRange(CardManager.Instance.mainDeckCardIDs);
            ShuffleDeckIDs(); 
        }

        // ★即座にドロー
        DrawCards(5); 
        StartPlayerTurn(); 
    }

    private void StartPlayerTurn()
    {
        isPlayerTurn = true;
        currentTurnCount++;
        
        // 2ターン目以降の補充ドロー
        if (currentTurnCount > 1)
        {
            int cardsToReplenish = 5 - dataContainer.playerData.hand.Count;
            if (cardsToReplenish > 0) DrawCards(cardsToReplenish); 
            if (dataContainer.playerData.hand.Count < MAX_HAND_SIZE) DrawCards(1); 
        }

        UpdateUI();
    }

    private void EndTurn()
    {
        isPlayerTurn = false;
        selectedCards.Clear();
        UpdateUI(); 
        StartCoroutine(EnemyTurnCoroutine());
    }

    private IEnumerator EnemyTurnCoroutine()
    {
        yield return new WaitForSeconds(1.5f); 
        ApplyDamageToPlayer(30); 
        UpdateUI();
        CheckGameEnd(); 
        if (dataContainer.playerData.currentHP > 0 && dataContainer.enemyData.currentHP > 0)
        {
            StartPlayerTurn();
        }
    }

    // -------------------------------------------------------------------
    // 6. ダメージ・UI処理 
    // -------------------------------------------------------------------
    private void UpdateUI()
    {
        if (dataContainer == null) return;
        
        if (numberText != null) numberText.text = $"Turn: {currentTurnCount}"; 
        if (playerHPText != null) playerHPText.text = $"{dataContainer.playerData.currentHP} / {dataContainer.playerData.maxHP}"; 
        if (playerHPFillImage != null) playerHPFillImage.fillAmount = (float)dataContainer.playerData.currentHP / dataContainer.playerData.maxHP; 
        
        if (dataContainer.enemyData != null && dataContainer.enemyData.enemyData != null)
        {
            enemyHPText.text = $"{dataContainer.enemyData.currentHP} / {dataContainer.enemyData.enemyData.MaxHP}";
            enemyHPFillImage.fillAmount = (float)dataContainer.enemyData.currentHP / dataContainer.enemyData.enemyData.MaxHP;
            enemyNameText.text = dataContainer.enemyData.enemyData.EnemyName;
        }

        // カードを1枚でも選んでいれば「Attack!」ボタンが押せる
        if (actionButton != null)
        {
            actionButton.interactable = (isPlayerTurn && selectedCards.Count > 0);
        }
    }

    // --- 補助メソッド（省略なし） ---
    private void ApplyDamageToEnemy(int damage) { if (dataContainer.enemyData != null) { dataContainer.enemyData.currentHP -= damage; if (dataContainer.enemyData.currentHP < 0) dataContainer.enemyData.currentHP = 0; if (enemyAnimator != null) enemyAnimator.PlayHitAnimation(); ShowDamageText(damage); } }
    private void ApplyDamageToPlayer(int damage) { if (dataContainer.playerData != null) { dataContainer.playerData.currentHP -= damage; if (dataContainer.playerData.currentHP < 0) dataContainer.playerData.currentHP = 0; ShowPlayerDamageText(damage); } }
    
    private void DrawCards(int count) 
    { 
        for (int i = 0; i < count; i++) 
        { 
            if (dataContainer.playerData.hand.Count >= MAX_HAND_SIZE) break; 
            if (dataContainer.playerData.deck.Count == 0) 
            { 
                if (dataContainer.playerData.discardPile.Count > 0) 
                { 
                    dataContainer.playerData.deck.AddRange(dataContainer.playerData.discardPile); 
                    dataContainer.playerData.discardPile.Clear(); 
                    ShuffleDeckIDs(); 
                } 
                else break; 
            } 
            int cardID = dataContainer.playerData.deck[0]; 
            dataContainer.playerData.deck.RemoveAt(0); 
            CardData drawnCard = CardManager.Instance.GetCardDataByID(cardID); 
            dataContainer.playerData.hand.Add(drawnCard); 
            if (cardUIPrefab != null && handArea != null) 
            { 
                GameObject cardObject = Instantiate(cardUIPrefab, handArea); 
                cardObject.GetComponent<CardUI>().SetupCard(drawnCard, this); 
            } 
        } 
    }

    private void ShowDamageText(int damage) { if (enemyDamagePrefab != null) { GameObject dt = Instantiate(enemyDamagePrefab); dt.GetComponentInChildren<DamageTextController>().SetDamageValue(damage); } }
    private void ShowPlayerDamageText(int damage) { if (playerDamagePrefab != null) { GameObject dt = Instantiate(playerDamagePrefab, playerDamageAnchor); dt.GetComponentInChildren<DamageTextController>().SetDamageValue(damage); } }
    private void ShuffleDeckIDs() { List<int> deck = dataContainer.playerData.deck; System.Random rng = new System.Random(); int n = deck.Count; while (n > 1) { n--; int k = rng.Next(n + 1); int value = deck[k]; deck[k] = deck[n]; deck[n] = value; } }
    private void CheckGameEnd() { if (dataContainer.playerData.currentHP <= 0) EndGame("Lose"); else if (dataContainer.enemyData.currentHP <= 0) EndGame("Win"); }
    private void EndGame(string result) { isPlayerTurn = false; if (gameEndPanel != null) { gameEndPanel.SetActive(true); resultText.text = result; } }
    private void OnEndScreenClicked() { if (sceneLoader != null) sceneLoader.LoadHomeScene(); }
}
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
    public TextMeshProUGUI costText; 
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText; 
    public TextMeshProUGUI enemyNameText; 
    public Image playerHPFillImage; 
    public Image enemyHPFillImage; 
    public Button endTurnButton; 
    public RectTransform handArea; 

    [Header("Animation References")]
    public ButtonFlasher buttonFlasher; 

    // --- カードUI参照 ---
    [Header("Card UI References")]
    public GameObject cardUIPrefab; 

    // ゲーム終了UI
    [Header("Game End UI")]
    public GameObject gameEndPanel;         
    public TextMeshProUGUI resultText;     
    public Button returnToHomeButton;       
    
    // -------------------------------------------------------------------
    // 2. データ / マネージャー参照 
    // -------------------------------------------------------------------
    [Header("Data & Manager References")]
    public BattleDataContainer dataContainer; 
    
    private SceneLoader sceneLoader; 

    // 手札上限
    private const int MAX_HAND_SIZE = 10; 

    // -------------------------------------------------------------------
    // 3. 状態変数 
    // -------------------------------------------------------------------
    private int currentTurnCount = 0;
    private bool isPlayerTurn = false;
    
    // -------------------------------------------------------------------
    // 4. Unity ライフサイクル 
    // -------------------------------------------------------------------
    void Start()
    {
        if (dataContainer == null) 
        {
            Debug.LogError("FATAL: BattleDataContainer is not assigned!");
            return;
        }
        
        sceneLoader = SceneLoader.Instance; 
        if (sceneLoader == null)
        {
            Debug.LogError("CRITICAL: SceneLoader.Instance is not available!");
        }

        if (gameEndPanel != null)
        {
            gameEndPanel.SetActive(false);
        }

        if (returnToHomeButton != null)
        {
            returnToHomeButton.onClick.AddListener(OnEndScreenClicked);
        }
        
        StartBattle();
    }

    // -------------------------------------------------------------------
    // 5. コアバトルフロー
    // -------------------------------------------------------------------
public void StartBattle()
    {
        Debug.Log("Battle Started.");
        
        // CardManagerのデッキが空の場合、再ロードを強制 (CleanupData()修正により、ここはデバッグ目的の保険)
        if (CardManager.Instance != null && CardManager.Instance.mainDeckCardIDs.Count == 0)
        {
            Debug.LogWarning("CardManager deck was empty on StartBattle. Forcing LoadAllGameData() again.");
            CardManager.Instance.LoadAllGameData();
        }
        
        // エネミーデータの初期化
        const int TARGET_ENEMY_ID = 1; 
        
        // 【★修正ポイント★：EnemyData.csの固定データを直接使用し、RED DRAGONを強制する】
        EnemyData staticEnemyData = EnemyData.GetFixedDataByID(TARGET_ENEMY_ID);
        
        if (staticEnemyData != null)
        {
            // BattleDataContainerにライブエネミーデータを設定
            dataContainer.enemyData = new EnemyBattleData(staticEnemyData);
            Debug.Log($"Enemy initialized: {staticEnemyData.EnemyName} (HP: {dataContainer.enemyData.currentHP})");
        }
        else
        {
            Debug.LogError($"FATAL: Fixed Enemy data (ID {TARGET_ENEMY_ID}) not available. Check EnemyData.cs static dictionary.");
            return;
        }

        // CardManagerからデッキをコピーし、シャッフル
        if (CardManager.Instance != null && CardManager.Instance.mainDeckCardIDs.Count > 0)
        {
            // バトル開始前にプレイヤーのライブデッキをクリア（念のため）
            dataContainer.playerData.deck.Clear();
            dataContainer.playerData.hand.Clear();
            dataContainer.playerData.discardPile.Clear();
            
            // dataContainer.playerData.deck に CardManagerのマスターデッキをコピー
            dataContainer.playerData.deck.AddRange(CardManager.Instance.mainDeckCardIDs);
            ShuffleDeckIDs(); 
            Debug.Log($"Loaded {dataContainer.playerData.deck.Count} cards from CardManager for battle.");
        }
        else
        {
            Debug.LogError("FATAL: CardManager deck is empty. Cannot start battle. (Check CardManager logs for CSV failure.)");
            return;
        }

        // ★★★ 確認ログ 1 ★★★: デッキコピー直後の確認
        Debug.Log($"--- BATTLE INIT CHECK 1 --- Deck Size after copy: {dataContainer.playerData.deck.Count}");
        
        DrawCards(5); // 初回は5枚ドロー

        // ★★★ 確認ログ 2 ★★★: ドロー後の確認
        Debug.Log($"--- BATTLE INIT CHECK 2 --- Deck Size after draw: {dataContainer.playerData.deck.Count}, Hand Size: {dataContainer.playerData.hand.Count}");

        StartPlayerTurn(); 
    }

    private void StartPlayerTurn()
    {
        if (!isPlayerTurn && currentTurnCount > 0 && (dataContainer.playerData.currentHP <= 0 || dataContainer.enemyData.currentHP <= 0))
        {
             return;
        }
        
        isPlayerTurn = true;
        currentTurnCount++;
        
        if (dataContainer.playerData != null)
        {
            dataContainer.playerData.currentCost = dataContainer.playerData.currentMaxCost; 
        }

        const int BASE_HAND_SIZE = 5;
        // dataContainer.playerData.hand を参照
        int cardsToReplenish = BASE_HAND_SIZE - dataContainer.playerData.hand.Count;

        if (cardsToReplenish > 0)
        {
            DrawCards(cardsToReplenish); 
        }
        
        // dataContainer.playerData.hand を参照
        if (dataContainer.playerData.hand.Count < MAX_HAND_SIZE)
        {
            DrawCards(1); 
        }

        UpdateUI();
        
        Debug.Log($"Player Turn {currentTurnCount} started. Cost: {dataContainer.playerData.currentCost}");
    }

    public void OnEndTurnButtonClicked()
    {
        if (isPlayerTurn) { EndTurn(); }
    }

    private void EndTurn()
    {
        isPlayerTurn = false;
        if (buttonFlasher != null) { buttonFlasher.StopFlashing(); }
        
        StartCoroutine(EnemyTurnCoroutine());
    }

    private IEnumerator EnemyTurnCoroutine()
    {
        if (dataContainer.playerData.currentHP <= 0 || dataContainer.enemyData.currentHP <= 0)
        {
            yield break;
        }
        
        Debug.Log("Enemy Turn Started. (Logic Placeholder)");
        
        const int ENEMY_DAMAGE = 30;
        
        yield return new WaitForSeconds(1.5f); 

        if (dataContainer != null && dataContainer.playerData != null)
        {
            ApplyDamageToPlayer(ENEMY_DAMAGE); 
        }
        
        UpdateUI();
        CheckGameEnd(); 
        
        Debug.Log("Enemy Turn Ended.");
        
        if (dataContainer.playerData.currentHP > 0 && dataContainer.enemyData.currentHP > 0)
        {
            StartPlayerTurn();
        }
    }

    private void ApplyDamageToPlayer(int damage)
    {
        if (dataContainer != null && dataContainer.playerData != null)
        {
            dataContainer.playerData.currentHP -= damage;
            if (dataContainer.playerData.currentHP < 0)
            {
                dataContainer.playerData.currentHP = 0;
            }
        }
    }
    
    // -------------------------------------------------------------------
    // 5.5. カードドロー処理 (手札上限、墓地シャッフルを含む)
    // -------------------------------------------------------------------
    private void DrawCards(int count)
    {
        int cardsToDraw = count;
        
        // dataContainer.playerData.hand を参照
        if (dataContainer.playerData.hand.Count >= MAX_HAND_SIZE)
        {
            return;
        }
        
        // dataContainer.playerData.hand を参照
        if (dataContainer.playerData.hand.Count + count > MAX_HAND_SIZE)
        {
            cardsToDraw = MAX_HAND_SIZE - dataContainer.playerData.hand.Count;
        }

        for (int i = 0; i < cardsToDraw; i++)
        {
            // dataContainer.playerData.deck を参照
            if (dataContainer.playerData.deck.Count == 0)
            {
                // dataContainer.playerData.discardPile を参照
                if (dataContainer.playerData.discardPile.Count > 0)
                {
                    dataContainer.playerData.deck.AddRange(dataContainer.playerData.discardPile);
                    dataContainer.playerData.discardPile.Clear(); 
                    ShuffleDeckIDs(); 
                    Debug.Log("Deck was empty. Shuffled discard pile back into deck.");
                }
                else
                {
                    return;
                }
            }

            // dataContainer.playerData.deck を参照
            int cardIDToDraw = dataContainer.playerData.deck[0];
            dataContainer.playerData.deck.RemoveAt(0);
            
            CardData drawnCard = null;
            if (CardManager.Instance != null)
            {
                drawnCard = CardManager.Instance.GetCardDataByID(cardIDToDraw);
            }
            
            if (drawnCard == null)
            {
                // CardDataが存在しない場合はスキップ (CSVエラー時に発生)
                Debug.LogError($"Failed to find CardData for ID: {cardIDToDraw}. Skipping draw.");
                continue; 
            }
            
            // dataContainer.playerData.hand に CardData を追加
            dataContainer.playerData.hand.Add(drawnCard);

            if (cardUIPrefab != null && handArea != null)
            {
                GameObject cardObject = Instantiate(cardUIPrefab, handArea);
                CardUI cardUI = cardObject.GetComponent<CardUI>();
                
                if (cardUI != null)
                {
                    cardUI.SetupCard(drawnCard, this); 
                }
            }
        }
    }

    /// <summary>
    /// バトルデッキのIDリストをシャッフルします。
    /// </summary>
    private void ShuffleDeckIDs() 
    { 
        // dataContainer.playerData.deck を参照
        List<int> deck = dataContainer.playerData.deck;
        System.Random rng = new System.Random();
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int value = deck[k];
            deck[k] = deck[n];
            deck[n] = value;
        }
    }
    
    // -------------------------------------------------------------------
    // 6. カード使用ロジック
    // -------------------------------------------------------------------
    
    public bool UseCard(GameObject cardObjectToDestroy, CardData cardData)
    { 
        if (!isPlayerTurn || dataContainer.playerData.currentCost < cardData.Cost)
        {
            return false;
        }

        dataContainer.playerData.currentCost -= cardData.Cost;
        ApplyCardEffect(cardData);
        
        // dataContainer.playerData.hand と discardPile を参照
        if (dataContainer.playerData.hand.Remove(cardData))
        {
            dataContainer.playerData.discardPile.Add(cardData.CardID);
        }

        Destroy(cardObjectToDestroy);
        UpdateUI();
        
        return true; 
    }

    private void ApplyCardEffect(CardData cardData)
    {
        // ダメージ処理の仮実装
        int damage = 10; 
        
        if (damage > 0 && dataContainer.enemyData != null)
        {
            dataContainer.enemyData.currentHP -= damage; 
            if (dataContainer.enemyData.currentHP < 0)
            {
                dataContainer.enemyData.currentHP = 0;
            }
        }
        CheckGameEnd();
    }
    
    // -------------------------------------------------------------------
    // 7. UI 更新メソッド
    // -------------------------------------------------------------------
    private void UpdateUI()
    {
        if (dataContainer == null) return;
        
        // --- プレイヤーUIの更新 ---
        if (costText != null) 
        { 
            costText.text = $"{dataContainer.playerData.currentCost} / {dataContainer.playerData.currentMaxCost}"; 
        }
        if (playerHPText != null) 
        { 
            playerHPText.text = $"{dataContainer.playerData.currentHP} / {dataContainer.playerData.maxHP}"; 
        }
        if (playerHPFillImage != null && dataContainer.playerData.maxHP > 0) 
        { 
            playerHPFillImage.fillAmount = (float)dataContainer.playerData.currentHP / dataContainer.playerData.maxHP; 
        }
        
        // エネミーUIの更新
        if (dataContainer.enemyData != null && dataContainer.enemyData.enemyData != null)
        {
            int maxHp = dataContainer.enemyData.enemyData.MaxHP; 
            int currentHp = dataContainer.enemyData.currentHP;
            string enemyName = dataContainer.enemyData.enemyData.EnemyName;
            
            if (enemyNameText != null) { enemyNameText.text = enemyName; }
            if (enemyHPText != null) { enemyHPText.text = $"{currentHp} / {maxHp}"; }
            
            if (enemyHPFillImage != null && maxHp > 0)
            {
                enemyHPFillImage.fillAmount = (float)currentHp / maxHp;
            }
        }
        
        // --- ターン終了ボタンの状態更新 ---
        if (endTurnButton != null && buttonFlasher != null)
        {
            endTurnButton.interactable = isPlayerTurn;
            
            // コストが残っているか、または手札を使い切ったかで点滅を切り替える
            if (isPlayerTurn && dataContainer.playerData.currentCost > 0)
            {
                buttonFlasher.StartFlashing();
            }
            else
            {
                buttonFlasher.StopFlashing();
            }
        }
    }

    // -------------------------------------------------------------------
    // 8. ゲーム終了判定 
    // -------------------------------------------------------------------
    private void CheckGameEnd()
    {
        if (dataContainer == null) return;

        string finalMessage = null;

        if (dataContainer.playerData.currentHP <= 0)
        {
            finalMessage = "Lose"; 
        }
        else if (dataContainer.enemyData.currentHP <= 0)
        {
            finalMessage = "Win"; 
        }

        if (finalMessage != null)
        {
            EndGame(finalMessage); 
        }
    }
    
    private void EndGame(string result)
    {
        Debug.Log($"Game Ended: {result}. Waiting for player input to return to HomeScene.");
        
        StopAllCoroutines(); 
        isPlayerTurn = false; 
        
        if (gameEndPanel != null)
        {
            gameEndPanel.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = result;
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }
    }

    /// <summary>
    /// 終了画面がクリック/タップされたときにHomeSceneに遷移します。
    /// returnToHomeButtonのonClickイベントに設定します。
    /// </summary>
	public void OnEndScreenClicked()
	{
		// シーン遷移前に、マネージャーのデータをリセットする

		if (CardManager.Instance != null)
		{
			// CardManagerのCleanupData()は、マスターデッキと所持カード数を保護するよう修正済み
			CardManager.Instance.CleanupData(); 
		}
		if (EnemyDataManager.Instance != null)
		{
			EnemyDataManager.Instance.CleanupData(); 
		}
		
        // 【★修正ポイント 2: ライブバトルデータのクリア★】
        // バトルで使用したライブデータ（山札、手札、墓地）をクリアする
        if (dataContainer != null && dataContainer.playerData != null)
        {
            dataContainer.playerData.deck.Clear();
            dataContainer.playerData.hand.Clear();
            dataContainer.playerData.discardPile.Clear();
            
            Debug.Log("BattleDataContainer player live data cleared (Deck, Hand, Discard).");
        }
        
        // ゲーム終了UIパネルを破棄（HomeSceneでの入力ブロックを防ぐ）
        if (gameEndPanel != null)
        {
            Destroy(gameEndPanel); 
        }
        
		if (sceneLoader != null)
		{
			Debug.Log("End screen clicked. Loading HomeScene...");
			sceneLoader.LoadHomeScene();
		}
		else
		{
			Debug.LogError("SceneLoader is missing. Cannot load HomeScene.");
		}
	}
    
    // -------------------------------------------------------------------
    // 9. データ取得メソッド
    // -------------------------------------------------------------------
    public int GetCurrentPlayerCost() { return dataContainer != null ? dataContainer.playerData.currentCost : 0; }
}
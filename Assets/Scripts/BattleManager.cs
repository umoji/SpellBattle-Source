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
    // ★修正点 1: costText を numberText に変更
    public TextMeshProUGUI numberText; // ターン数など、ゲームの状態を表示するテキスト
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText; 
    public TextMeshProUGUI enemyNameText; 
    public Image playerHPFillImage; 
    public Image enemyHPFillImage; 
    public Button endTurnButton; 
    public RectTransform handArea; 
    
    [Header("Animation References")]
    public ButtonFlasher buttonFlasher; 
    public GameObject enemyDamagePrefab;   
    public GameObject playerDamagePrefab;  
    
    [Header("Enemy Components")]
    public EnemyAnimator enemyAnimator;

    [Header("Player Components")]
    public Transform playerDamageAnchor; // プレイヤーダメージ位置のアンカー

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
        
        // エネミーデータの初期化
        const int TARGET_ENEMY_ID = 1; 
        
        // 【★EnemyData.csの固定データを直接使用し、RED DRAGONを強制する】
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

        DrawCards(5); // 初回は5枚ドロー

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
        
        // ★修正点 2: コスト回復ロジックを削除 (ナンバー制のため)
        /*
        if (dataContainer.playerData != null)
        {
            dataContainer.playerData.currentCost = dataContainer.playerData.currentMaxCost; 
        }
        */

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
        
        // ★修正点 3: ログからコスト表示を削除
        Debug.Log($"Player Turn {currentTurnCount} started.");
    }

    public void OnEndTurnButtonClicked()
    {
        if (isPlayerTurn) { EndTurn(); }
    }

    private void EndTurn()
    {
        isPlayerTurn = false;
        if (buttonFlasher != null) { buttonFlasher.StopFlashing(); }
        
        // dataContainer.playerData.hand を参照
        // ターン終了時に手札を全て捨てる処理 (ゲームデザインによっては不要)
        /*
        foreach(CardData card in dataContainer.playerData.hand.ToList())
        {
             dataContainer.playerData.discardPile.Add(card.CardID);
        }
        dataContainer.playerData.hand.Clear();
        // UI上からカードオブジェクトを削除する処理も必要
        */

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
            int oldHP = dataContainer.playerData.currentHP; 
            
            // 実際にダメージを適用
            dataContainer.playerData.currentHP -= damage;
            
            if (dataContainer.playerData.currentHP < 0)
            {
                dataContainer.playerData.currentHP = 0;
            }
            
            // HPが実際に減った場合（ダメージが0以上の場合）に処理を実行
            if (oldHP > dataContainer.playerData.currentHP)
            {
                Debug.Log("--- PLAYER HIT! Calling ShowPlayerDamageText. ---"); 
                
                ShowPlayerDamageText(damage);
            }
            else
            {
                 Debug.Log($"Player HP change: {oldHP} -> {dataContainer.playerData.currentHP}. Damage was {damage}. No damage text shown.");
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
        // ★修正点 4: ナンバー制に移行したため、コストチェックと消費ロジックを削除

        // ターン中であれば常にカード使用は成功と仮定
        if (!isPlayerTurn)
        {
            return false;
        }

        ApplyCardEffect(cardData);
        
        // dataContainer.playerData.hand と discardPile を参照
        if (dataContainer.playerData.hand.Remove(cardData))
        {
            dataContainer.playerData.discardPile.Add(cardData.CardID); // CardDataにCardIDプロパティがあると仮定
        }

        Destroy(cardObjectToDestroy);
        UpdateUI();
        
        return true; 
    }

    private void ApplyCardEffect(CardData cardData)
    {
        // ナンバー制に基づいた新しいゲームロジックを実装する
        int damage = cardData.Number * 10; // 仮の実装: ナンバー × 10 ダメージ
        
        if (damage > 0 && dataContainer.enemyData != null)
        {
            ApplyDamageToEnemy(damage);
        }
        CheckGameEnd();
    }
    
    // -------------------------------------------------------------------
    // 6.5. ダメージ適用とテキスト表示/アニメーション
    // -------------------------------------------------------------------
    private void ApplyDamageToEnemy(int damage)
    {
		
		Debug.Log($"[Damage Check] Attempting to apply {damage} damage."); 

        if (dataContainer.enemyData != null)
        {
            int oldHP = dataContainer.enemyData.currentHP;
            dataContainer.enemyData.currentHP -= damage; 
            
            if (dataContainer.enemyData.currentHP < 0)
            {
                dataContainer.enemyData.currentHP = 0;
            }

            // ダメージが実際に減少した場合
            if (oldHP > dataContainer.enemyData.currentHP)
            {
                Debug.Log($"[Damage Check] Passed check, attempting to SHOW text and run animation."); 
                
                // Enemy Animatorの再生
                if (enemyAnimator != null)
                {
                    enemyAnimator.PlayHitAnimation();
                }

                // ダメージテキストのポップアップを呼び出す
                if (enemyDamagePrefab != null) 
                {
                    ShowDamageText(damage);
                }
            }
        }
    }

    /// <summary>
    /// 敵のダメージテキストを画面上の固定位置に表示する (Screen Space対応)
    /// </summary>
    private void ShowDamageText(int damageAmount)
    {
        if (enemyDamagePrefab == null)
        {
            Debug.LogError("Enemy Damage PrefabがInspectorに設定されていません。");
            return;
        }

        // 親なしで生成（Screen Space Canvasプレハブのデフォルト動作に依存）
        GameObject damageTextInstance = Instantiate(enemyDamagePrefab); 
        
        // Controllerが子オブジェクトにあるため、GetComponentInChildrenで取得
        DamageTextController controller = damageTextInstance.GetComponentInChildren<DamageTextController>();
        
        if (controller == null)
        {
            Debug.LogError("生成されたEnemy Damage Prefabに DamageTextController コンポーネントが子オブジェクトも含めて見つかりません。");
            Destroy(damageTextInstance); // クリーンアップ
            return;
        }

        // 描画順序の強制設定
        Canvas textCanvas = damageTextInstance.GetComponentInChildren<Canvas>(true); 
        if (textCanvas != null)
        {
            textCanvas.overrideSorting = true;
            textCanvas.sortingOrder = 100; // 他のUIより高い値
        }

        // ダメージ値を設定し、アニメーションを開始させる
        controller.SetDamageValue(damageAmount);
    }
    
    /// <summary>
    /// プレイヤーのHPバー付近にダメージテキストを表示する (Screen Space対応)
    /// </summary>
    private void ShowPlayerDamageText(int damageAmount)
    {
        if (playerDamagePrefab == null)
        {
            Debug.LogError("Player Damage PrefabがInspectorに設定されていません。");
            return;
        }

        // プレイヤーダメージアンカーを親として使用
        Transform parentTransform = playerDamageAnchor != null ? playerDamageAnchor : null;

        // 親Transformを指定して生成
        GameObject damageTextInstance = Instantiate(playerDamagePrefab, parentTransform);
        
        RectTransform rt = damageTextInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
             // プレハブ設定の位置がアンカーからの相対位置になる
             rt.localPosition = Vector3.zero; 
             
             // Z軸を強制的に 0 にリセットし、描画問題を解決する
             rt.localRotation = Quaternion.identity;
             rt.localScale = Vector3.one;
             rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f); 
        }

        // Controllerが子オブジェクトにあるため、GetComponentInChildrenで取得
        DamageTextController controller = damageTextInstance.GetComponentInChildren<DamageTextController>();
        
        if (controller == null)
        {
            Debug.LogError("Player Damage Text Prefabに DamageTextController コンポーネントが見つかりません。");
            Destroy(damageTextInstance); // クリーンアップ
            return;
        }

        // 描画順序の強制設定
        Canvas textCanvas = damageTextInstance.GetComponentInChildren<Canvas>(true); 
        if (textCanvas != null)
        {
            textCanvas.overrideSorting = true;
            textCanvas.sortingOrder = 1000; // 確実に最前面に来るよう、値を極端に上げる
        }

        // ダメージ値を設定し、アニメーションを開始させる
        controller.SetDamageValue(damageAmount);
    }
    
    // -------------------------------------------------------------------
    // 7. UI 更新メソッド
    // -------------------------------------------------------------------
    private void UpdateUI()
    {
        if (dataContainer == null) return;
        
        // --- プレイヤーUIの更新 ---
        // ★修正点 5: numberTextにターン数を表示
        if (numberText != null) 
        { 
            numberText.text = $"Turn: {currentTurnCount}"; 
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
            
            // ナンバー制にはコスト制限がないため、点滅は常に停止
            buttonFlasher.StopFlashing(); 
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
		if (CardManager.Instance != null)
		{
			CardManager.Instance.CleanupData(); 
		}
		if (EnemyDataManager.Instance != null)
		{
			EnemyDataManager.Instance.CleanupData(); 
		}
		
        // 【ライブバトルデータのクリア】
        if (dataContainer != null && dataContainer.playerData != null)
        {
            dataContainer.playerData.deck.Clear();
            dataContainer.playerData.hand.Clear();
            dataContainer.playerData.discardPile.Clear();
            
            Debug.Log("BattleDataContainer player live data cleared (Deck, Hand, Discard).");
        }
        
        // ゲーム終了UIパネルを破棄
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
}
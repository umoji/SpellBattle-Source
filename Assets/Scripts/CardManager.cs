using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using System;
using System.Text.RegularExpressions;

// ゲームのデータを保持・管理するシングルトン
public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("デッキ設定")]
    [SerializeField]
    public List<int> mainDeckCardIDs = new List<int>(); // バトルで使用するライブデッキ/マスターデッキとして機能

    public int deckSizeLimit = 20; 

    [Header("静的データ")]
    public List<CardData> allCards = new List<CardData>(); // CSVからロードされる全カードの静的データ

    [Header("初期デッキ設定 (永続化)")]
    public List<int> initialDeckIDs = new List<int>(); 

    [Header("所持情報 (Inspector表示用リスト)")]
    [SerializeField]
    private List<CardCountEntry> ownedCardsList = new List<CardCountEntry>(); 
    
    // 所持カード数キャッシュ（永続化データ）
    private Dictionary<int, int> _ownedCardCountsCache = new Dictionary<int, int>();

    // CSVファイル名：「EnemyData.csv」を参照
    private const string CARD_CSV_FILE_NAME = "EnemyData"; 
    
    [Header("ガチャ排出設定")]
    [Range(0f, 100f)] public float SSR_RATE = 5.0f;  // 5%
    [Range(0f, 100f)] public float SR_RATE = 20.0f; // 20%
    [Range(0f, 100f)] public float R_RATE = 30.0f;  // 30%
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
            
            // データのロード
            LoadStaticCardDataFromCSV();
            
            // 初期デッキIDリストが空であれば、デフォルトを設定 (ID 1-20)
            if (initialDeckIDs.Count == 0 && allCards.Count >= 20)
            {
                 SetInitialDeckDefault();
            }
            
            // 所持カード数リストをロード
            LoadOwnedCardCountsFromList();

            // ★修正点★: 初回起動時の「所持カード」初期化ロジックを削除/コメントアウト
            // 初期デッキを所持カードとして設定する処理は、今回は行いません。
            /*
            if (_ownedCardCountsCache.Count == 0)
            {
                InitializeOwnedCards();
            }
            */

            // マスターデッキを構築するロジックは実行
            BuildMainDeckFromInitial();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // --- 初期化処理 (未使用にするが、定義は残す) ---
    private void InitializeOwnedCards()
    {
        // 今回はゲーム開始時にコレクションを0にするため、このメソッドはAwake()から呼ばない
        foreach (int cardID in initialDeckIDs)
        {
            if (_ownedCardCountsCache.ContainsKey(cardID))
            {
                _ownedCardCountsCache[cardID]++;
            }
            else
            {
                _ownedCardCountsCache.Add(cardID, 1);
            }
        }
        SaveOwnedCardCountsToList();
        Debug.LogWarning("Owned cards initialized from initialDeckIDs. (This should only run if explicitly needed)");
    }

    // --- 所持カード数リストのロード/セーブ ---
    
    private void LoadOwnedCardCountsFromList()
    {
        _ownedCardCountsCache.Clear();
        foreach (var entry in ownedCardsList)
        {
            // 既存のデータがあれば上書き
            _ownedCardCountsCache[entry.cardID] = entry.count;
        }
    }
    
    private void SaveOwnedCardCountsToList()
    {
        ownedCardsList.Clear();
        foreach (var kvp in _ownedCardCountsCache)
        {
            ownedCardsList.Add(new CardCountEntry { cardID = kvp.Key, count = kvp.Value });
        }
    }

    // --- メインデッキ管理 ---

    public void SetMainDeck(List<int> newDeckIDs)
    {
        mainDeckCardIDs = newDeckIDs;
    }

    public List<int> GetMainDeck()
    {
        return mainDeckCardIDs;
    }
    
    public int GetMainDeckCount()
    {
        return mainDeckCardIDs.Count;
    }
    
    public bool AddCardToMainDeck(int cardID)
    {
        // 所持数チェック: コレクションにカードがないとデッキに入れられない
        if (GetCardCount(cardID) <= GetMainDeckCardCountInDeck(cardID))
        {
            Debug.LogWarning($"Cannot add CardID {cardID}. Max owned count reached.");
            return false;
        }

        // デッキサイズ制限チェック
        if (mainDeckCardIDs.Count >= deckSizeLimit)
        {
            Debug.LogWarning("Deck size limit reached.");
            return false;
        }

        mainDeckCardIDs.Add(cardID);
        return true;
    }

    public bool RemoveCardFromMainDeck(int cardID)
    {
        bool removed = mainDeckCardIDs.Remove(cardID);
        if (removed)
        {
            // mainDeckCardIDs.Remove(cardID) は最初に見つかったものだけを削除します
        }
        return removed;
    }
    
    // デッキ内の特定のカードの現在の枚数を取得
    public int GetMainDeckCardCountInDeck(int cardID)
    {
        return mainDeckCardIDs.Count(id => id == cardID);
    }
    
    // --- 所持カード数管理 ---
    
    // 所持カード数の増減
    public void ChangeCardCount(int cardID, int amount)
    {
        if (_ownedCardCountsCache.ContainsKey(cardID))
        {
            _ownedCardCountsCache[cardID] += amount;
            
            if (_ownedCardCountsCache[cardID] < 0)
            {
                 _ownedCardCountsCache[cardID] = 0;
            }
        }
        else if (amount > 0)
        {
            // 所持カードリストにないが、追加する場合は追加する
            _ownedCardCountsCache.Add(cardID, amount);
        }

        // Inspector表示用のリストも更新
        SaveOwnedCardCountsToList(); 
        
        Debug.Log($"Card ID {cardID} count changed by {amount}. New count: {GetCardCount(cardID)}");
    }

    public void AddCardData(CardData data)
    {
        if (allCards.Find(c => c.CardID == data.CardID) == null)
        {
            allCards.Add(data);
        }
    }
    
    public CardData GetCardDataByID(int id)
    {
        if (allCards.Count == 0)
        {
            Debug.LogError("FATAL: allCards is empty! Static data load failed.");
            return null;
        }
        return allCards.FirstOrDefault(c => c.CardID == id);
    }

    public int GetCardCount(int cardID)
    {
        if (_ownedCardCountsCache.ContainsKey(cardID))
        {
            return _ownedCardCountsCache[cardID];
        }
        return 0;
    }
    
    // 所持しているカードIDと枚数の辞書を取得
    public Dictionary<int, int> GetAllOwnedCardCounts()
    {
        return _ownedCardCountsCache;
    }

    // --- データクリーンアップ（シーン切り替え時などに使用） ---
    public void CleanupData()
    {
        // mainDeckCardIDs はマスターデッキとして保護するためクリアしない
        
        Debug.Log("CardManager cleanup complete. Master Deck and Owned Cards preserved.");
    }
    
    // ------------------------------------------------------------------
    // ★★★ データロード/構築ロジック ★★★
    // ------------------------------------------------------------------
    
    /// <summary>
    /// CSVファイルから静的カードデータのみをロードし、allCardsに格納します。
    /// </summary>
    private void LoadStaticCardDataFromCSV()
    {
        allCards.Clear();
        
        TextAsset csvFile = Resources.Load<TextAsset>(CARD_CSV_FILE_NAME);

        if (csvFile == null)
        {
            Debug.LogError($"CRITICAL: CSVファイルが見つかりません: Resources/{CARD_CSV_FILE_NAME}.txt。データロード失敗。");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        int loadedCount = 0; 
        
        foreach (string line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            // 正規表現で CSV を安全に分割
            string[] fields = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            
            if (fields.Length < 12) continue;

            // fields[1] Rarity, fields[3] Attribute, fields[7] Number, fields[8] EffectType, fields[9] Power
				if (int.TryParse(fields[0].Trim(), out int id) && 
                System.Enum.TryParse<ElementType>(fields[3].Trim(), true, out ElementType attribute) &&
                System.Enum.TryParse<CardRarity>(fields[1].Trim(), true, out CardRarity rarity) &&
                System.Enum.TryParse<EffectType>(fields[8].Trim(), true, out EffectType effectType) && 
                
                // ★修正点 1: int.TryParse の変数名を cost から number に変更
                int.TryParse(fields[7].Trim(), out int number) &&
                
                int.TryParse(fields[9].Trim(), out int power))
            {
                CardData card = new CardData
                {
                    CardID = id,
                    Rarity = rarity,
                    CardName = fields[2].Trim(), 
                    Attribute = attribute,
                    
                    // ★修正点 2: Cost = cost を Number = number に変更
                    Number = number,
                    
                    EffectType = effectType,
                    Power = power,
                    EffectText = fields[11].Trim(),
                    visualAssetPath = fields[10].Trim() 
                };
                
                allCards.Add(card);
                loadedCount++;
            }
            else
            {
                Debug.LogWarning($"CardManager: Skipping CSV row due to parsing failure in line: {line}"); 
            }
        }
        
        Debug.Log($"CardManager: Successfully finished parsing. Loaded {loadedCount} card entries from {CARD_CSV_FILE_NAME}."); 
    }
    
    /// <summary>
    /// ID 1からID 20を初期デッキのマスターとして設定します。
    /// </summary>
    private void SetInitialDeckDefault()
    {
        initialDeckIDs.Clear();
        Debug.Log("CardManager: Setting default initial deck (ID 1-20).");
        
        for (int i = 1; i <= 20; i++)
        {
            CardData card = allCards.FirstOrDefault(c => c.CardID == i);
            if (card != null)
            {
                initialDeckIDs.Add(card.CardID);
            }
            else
            {
                Debug.LogWarning($"Card ID {i} not found in static data. Skipping.");
            }
        }
    }

    /// <summary>
    /// 記憶された初期デッキIDをメインデッキにコピーします。
    /// </summary>
    private void BuildMainDeckFromInitial()
    {
        mainDeckCardIDs.Clear();

        if (initialDeckIDs.Count > 0)
        {
            mainDeckCardIDs.AddRange(initialDeckIDs);
            Debug.Log($"Main deck rebuilt from initial deck. Size: {mainDeckCardIDs.Count}");
        } else {
             Debug.LogError("FATAL: Initial deck is empty. Cannot build main deck.");
        }
    }
    
    // ------------------------------------------------------------------
    // ★★★ ガチャ機能 ロジック ★★★
    // ------------------------------------------------------------------

    /// <summary>
    /// 指定されたレアリティの全カードデータをリストで返します。
    /// </summary>
    public List<CardData> GetCardsByRarity(CardRarity rarity)
    {
        return allCards.Where(c => c.Rarity == rarity).ToList();
    }

    /// <summary>
    /// 1回ガチャを引き、排出されたカードデータを返します。
    /// </summary>
    public CardData DrawGacha()
    {
        float roll = UnityEngine.Random.Range(0f, 100f);
        CardRarity drawnRarity;

        // 累積確率でレアリティを決定
        if (roll < SSR_RATE)
        {
            drawnRarity = CardRarity.SSR;
        }
        else if (roll < (SSR_RATE + SR_RATE))
        {
            drawnRarity = CardRarity.SR;
        }
        else
        {
            // 残り全てを R (Rare) とする
            drawnRarity = CardRarity.R;
        }

        List<CardData> rarityPool = GetCardsByRarity(drawnRarity);

        if (rarityPool.Count == 0)
        {
            // Rarity R にフォールバック
            Debug.LogError($"Gacha error: No cards found for rarity {drawnRarity}. Defaulting to R.");
            rarityPool = GetCardsByRarity(CardRarity.R);
            
            if (rarityPool.Count == 0)
            {
                Debug.LogError("FATAL: Rarity R cards are also missing. Cannot draw.");
                return null; 
            }
        }
        
        int index = UnityEngine.Random.Range(0, rarityPool.Count);
        CardData drawnCard = rarityPool[index];
        
        // 取得したカードを所持リストに追加 (コレクションに反映)
        ChangeCardCount(drawnCard.CardID, 1); 

        Debug.Log($"Gacha Draw: Rarity {drawnRarity}, Card: {drawnCard.CardName}");
        
        return drawnCard;
    }
    
}

[Serializable]
public class CardCountEntry
{
    public int cardID;
    public int count;
}
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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 起動時に全データと初期デッキをロード/再構築
        LoadAllGameData();
        
        LoadOwnedCardCountsFromList(); 
        
        Debug.Log($"CardManager initialized. Total cards: {allCards.Count}. Deck Count: {mainDeckCardIDs.Count}");
    }
    
    private void LoadOwnedCardCountsFromList()
    {
        _ownedCardCountsCache.Clear();
        foreach (var entry in ownedCardsList)
        {
            _ownedCardCountsCache.Add(entry.CardID, entry.Count); 
        }
    }

    private void SaveOwnedCardCountsToList()
    {
        ownedCardsList.Clear();
        foreach (var kvp in _ownedCardCountsCache)
        {
            ownedCardsList.Add(new CardCountEntry { CardID = kvp.Key, Count = kvp.Value });
        }
    }
    
    /// <summary>
    /// 全てのデータ（静的データとデッキ構成）をロード/再ロードします。
    /// </summary>
    public void LoadAllGameData()
    {
        // 1. ライブデータ（バトル中のデータ）のみクリア (CleanupDataで対応)
        // ※ ここでは mainDeckCardIDs.Clear() は呼ばない

        // 2. 静的データ（CSV）がまだロードされていない場合のみロード
        if (allCards.Count == 0)
        {
            LoadStaticCardDataFromCSV();
        }
        
        // 3. 初期デッキをまだ設定していない場合は設定（ID 1-20を記憶）
        if (initialDeckIDs.Count == 0 && allCards.Count >= 20)
        {
             SetInitialDeckDefault();
        } 
        
        // 4. 記憶された初期デッキから、バトル用のメインデッキを構築
        BuildMainDeckFromInitial();
        
        // 5. 所持カードカウントの初期化も行う
        LoadOwnedCardCountsFromCSV();
        
        Debug.Log($"CardManager data successfully reloaded. Total cards loaded: {allCards.Count}. Deck size: {mainDeckCardIDs.Count}");
    }
    
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
            string[] fields = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            
            if (fields.Length < 12) continue;

            if (int.TryParse(fields[0].Trim(), out int id) && 
                System.Enum.TryParse<ElementType>(fields[3].Trim(), true, out ElementType attribute) &&
                System.Enum.TryParse<CardRarity>(fields[1].Trim(), true, out CardRarity rarity) &&
                System.Enum.TryParse<EffectType>(fields[8].Trim(), true, out EffectType effectType) && 
                int.TryParse(fields[7].Trim(), out int cost) &&
                int.TryParse(fields[9].Trim(), out int power))
            {
                CardData card = new CardData
                {
                    CardID = id,
                    Rarity = rarity,
                    CardName = fields[2].Trim(), 
                    Attribute = attribute,
                    Cost = cost,
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
        
        for (int i = 1; i <= Math.Min(20, allCards.Count); i++)
        {
            initialDeckIDs.Add(allCards.FirstOrDefault(c => c.CardID == i)?.CardID ?? i);
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

    /// <summary>
    /// 所持カードカウントのロードロジック（通常はセーブデータからロード）
    /// </summary>
	private void LoadOwnedCardCountsFromCSV()
	{
		 _ownedCardCountsCache.Clear();
		 
		 // ID 1からID 49までの全カードを対象としていましたが、これを限定します。
		 
		 const int START_ID = 21;
		 const int END_ID = 25;

		 // 【★修正点★】ID 21から 25 のカードのみを1枚所持として初期設定する
		 foreach (var card in allCards)
		 {
			 if (card.CardID >= START_ID && card.CardID <= END_ID)
			 {
				 if (!_ownedCardCountsCache.ContainsKey(card.CardID))
				 {
					 _ownedCardCountsCache.Add(card.CardID, 1); 
				 }
			 }
			 // それ以外のカードは、所持数0のまま（またはキャッシュに追加しない）
		 }
		 SaveOwnedCardCountsToList();
	}

    /// <summary>
    /// バトル終了時にライブデータ（キャッシュ）のみをクリアします。
    /// 【修正済み】所持カードキャッシュは永続データのため、クリアしません。
    /// </summary>
    public void CleanupData()
    {
        // CardManagerが保持すべきデータ（マスターデッキ、所持カード数）はクリアしない。
        Debug.Log("CardManager retains all static and persistent data.");
    }
    
    // --- ゲッターとヘルパー ---
    
    /// <summary>
    /// 指定したIDのカードの所持数を指定量だけ増減させます。
    /// 【★新規追加メソッド★】
    /// </summary>
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
    
    public int GetMainDeckCount()
    {
        return mainDeckCardIDs.Count;
    }

    [Serializable]
    public class CardCountEntry
    {
        public int CardID; 
        public int Count;  
    }
}
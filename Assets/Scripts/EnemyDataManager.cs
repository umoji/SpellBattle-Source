using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using System; 

public class EnemyDataManager : MonoBehaviour
{
    public static EnemyDataManager Instance { get; private set; }
    
    public List<EnemyData> AllEnemyData { get; private set; } = new List<EnemyData>();

    private const string ENEMY_CSV_FILE_NAME = "EnemyData"; 

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
        LoadEnemyData(); 
    }
    
    // --- ★修正箇所: CSV読み込みロジックの統合とパース強化★ ---
    private void LoadEnemyData()
    {
        AllEnemyData.Clear();

        TextAsset csvFile = Resources.Load<TextAsset>(ENEMY_CSV_FILE_NAME);

        if (csvFile == null)
        {
            Debug.LogError($"CRITICAL: CSVファイルが見つかりません: Resources/{ENEMY_CSV_FILE_NAME}.txt。安全モックデータを使用します。");
            
            // 安全モックデータ
            CardData mockCard = new CardData { CardID = 1, CardName = "Safety Mock Card", Attribute = ElementType.Fire, Cost = 1, Power = 100, EffectType = EffectType.DamageTarget, EffectText = "Safety 100 Damage" };
            EnemyData mockEnemy = new EnemyData { EnemyID = 1, EnemyName = "Safety Mock", MaxHP = 5000, BaseAttackDamage = 30, Attribute = ElementType.Fire, CardData = mockCard };
            AllEnemyData.Add(mockEnemy);
            
            if (CardManager.Instance != null)
            {
                CardManager.Instance.AddCardData(mockCard);
            }
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        
        // ヘッダー行をスキップし、データ行を処理
        foreach (string line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] fields = line.Split(',');
            
            // CSVの列数が適切か確認（画像では少なくとも12列必要）
            if (fields.Length < 12) continue; 

            // --- データのトリミングと大文字化 ---
            // Enumのパースが失敗しないよう、Trim()し、大文字に変換して統一性を確保
            string rarityString = fields[1].Trim().ToUpperInvariant();
            string attributeString = fields[3].Trim().ToUpperInvariant();
            string effectTypeString = fields[8].Trim().ToUpperInvariant(); // ★修正: EffectTypeはインデックス8

            // データのパースとバリデーション (CSVインデックスに厳密に合わせる)
            if (int.TryParse(fields[0].Trim(), out int id) && 
                System.Enum.TryParse<ElementType>(attributeString, true, out ElementType attribute) && // fields[3]
                int.TryParse(fields[4].Trim(), out int maxHp) && // fields[4]
                int.TryParse(fields[5].Trim(), out int attackPower) && // fields[5]
                int.TryParse(fields[6].Trim(), out int patternId) && // fields[6]
                int.TryParse(fields[7].Trim(), out int cost) && // fields[7]
                
                System.Enum.TryParse<CardRarity>(rarityString, true, out CardRarity rarity) && // fields[1]
                System.Enum.TryParse<EffectType>(effectTypeString, true, out EffectType effectType) && // fields[8]
                
                int.TryParse(fields[9].Trim(), out int power)) // ★修正: fields[9]はPower
            {
                // CardDataオブジェクトの構築
                CardData card = new CardData
                {
                    CardID = id,
                    CardName = fields[2].Trim(), 
                    visualAssetPath = fields[10].Trim(), // ★修正: AssetPathはfields[10]
                    Attribute = attribute,
                    Cost = cost,
                    Rarity = rarity,
                    EffectType = effectType,
                    Power = power,
                    EffectText = fields[11].Trim() 
                };
                
                // EnemyDataオブジェクトの構築
                EnemyData enemy = new EnemyData
                {
                    EnemyID = id, 
                    EnemyName = fields[2].Trim(), 
                    visualAssetPath = fields[10].Trim(), // ★修正: AssetPathはfields[10]
                    Rarity = fields[1].Trim(), // Rarityは文字列のまま保持
                    MaxHP = maxHp,
                    BaseAttackDamage = attackPower,
                    AttackPatternID = patternId,
                    Attribute = attribute,
                    CardData = card 
                };
                
                AllEnemyData.Add(enemy);
                
                if (CardManager.Instance != null)
                {
                    CardManager.Instance.AddCardData(card);
                }
            }
            else
            {
                // エラーの原因となっている行の内容を正確に出力
                Debug.LogWarning($"Skipping row due to parsing failure: {line}"); 
            }
        }

        Debug.Log($"✅ EnemyDataManager: CSVから {AllEnemyData.Count} 体のエネミーデータを正常にロードしました。");
    }
    
    public EnemyData GetEnemyDataByID(int id)
    {
        return AllEnemyData.FirstOrDefault(e => e.EnemyID == id);
    }
	
	public void CleanupData()
	{
		if (AllEnemyData != null)
		{
			AllEnemyData.Clear();
		}
		
		// EnemyData.cs の静的辞書もクリアすべきですが、今回は外部からアクセスしないため省略可
		
		Debug.Log("EnemyDataManager data cleaned up successfully.");
	}
	
}
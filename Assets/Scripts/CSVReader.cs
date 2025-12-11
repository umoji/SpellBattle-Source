using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class CSVReader
{
    // ★修正箇所★ ファイルパスを EnemyData に変更
    private const string DataPath = "EnemyData"; 

    public static List<CardData> LoadAllCardsFromCsv()
    {
        List<CardData> cardList = new List<CardData>();

        TextAsset csvFile = Resources.Load<TextAsset>(DataPath);

        if (csvFile == null)
        {
            Debug.LogError($"CRITICAL: CSV File not found at Resources/{DataPath}.csv");
            return cardList;
        }

        string[] lines = csvFile.text.Split('\n');

        // ヘッダー行をスキップ
        foreach (string line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] fields = line.Split(',');

            // エネミーCSVの列数 (少なくとも12列必要)
            if (fields.Length < 12)
            {
                Debug.LogWarning($"Skipping row due to insufficient fields: {line}");
                continue;
            }

            // --- データのトリミングと大文字化 ---
            string attributeString = fields[3].Trim().ToUpperInvariant();
            string effectTypeString = fields[8].Trim().ToUpperInvariant(); // EffectTypeはfields[8]

            // --- パースロジック ---
            // ID (0), Cost (7), Attribute (3), EffectType (8), Power (9)
            if (int.TryParse(fields[0].Trim(), out int id) && 
                int.TryParse(fields[7].Trim(), out int cost) && // ★修正: Costはインデックス7
                int.TryParse(fields[9].Trim(), out int power) && // ★修正: Powerはインデックス9
                System.Enum.TryParse<ElementType>(attributeString, true, out ElementType attribute) && // Attributeはインデックス3
                System.Enum.TryParse<EffectType>(effectTypeString, true, out EffectType effectType)) // EffectTypeはインデックス8
            {
                CardData card = new CardData
                {
                    CardID = id,
                    CardName = fields[2].Trim(), // ★修正: CardNameはインデックス2
                    Cost = cost,
                    EffectText = fields[11].Trim(), // ★修正: EffectTextはインデックス11
                    visualAssetPath = fields[10].Trim(), // ★修正: AssetPathはインデックス10
                    Attribute = attribute,
                    EffectType = effectType,
                    Power = power
                    // Rarityはfields[1]だが、CardDataにはRarity Enumが必要なため省略し、デフォルト値を使用
                };
                cardList.Add(card);
            }
            else
            {
                Debug.LogWarning($"Skipping row due to parsing failure: {line}");
            }
        }

        return cardList;
    }
}
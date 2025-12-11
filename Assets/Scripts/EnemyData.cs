using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// [Serializable]を付けると、このクラスをUnityエディタでデータとして保存・編集できるようになります。
[Serializable]
public class EnemyData
{
    // --- 静的データ（カードと共通のビジュアルID） ---
    public int EnemyID;             // このIDは、対応するカードIDと一致します。
    public string EnemyName = "Unknown Enemy"; // デフォルト名
    public string visualAssetPath; // カードと共通のビジュアルアセットのファイル名やパス

    // 【追加箇所】カードと共通のレアリティ情報をCSVから読み込むため追加
    public string Rarity;
    
    // --- バトルデータ ---
    public int MaxHP = 100;               // 最大HP
    public ElementType Attribute;    // エネミーの属性（GameEnums.csで定義）
    public int BaseAttackDamage;     // エネミーの通常攻撃ダメージ
    public int AttackPatternID;      // エネミーの行動パターンを制御するID
    
    // --- ドロップ設定 ---
    public float BaseDropRate;       // 自身のカードをドロップする基本確率

    // 敵が倒された際に得られるカードのデータへの参照
    public CardData CardData;


    // =================================================================
    // ★新規追加★: 固定エネミーデータ管理のための静的辞書と初期化メソッド
    // =================================================================
    
    // 静的辞書。一度ロードされたデータがここにキャッシュされます。
    private static Dictionary<int, EnemyData> _fixedEnemyDataCache;

    /// <summary>
    /// IDに基づいて固定エネミーデータを返します。
    /// </summary>
    public static EnemyData GetFixedDataByID(int enemyID)
    {
        // 辞書がまだ構築されていなければ構築する (遅延初期化)
        if (_fixedEnemyDataCache == null)
        {
            InitializeFixedData();
        }

        if (_fixedEnemyDataCache.ContainsKey(enemyID))
        {
            return _fixedEnemyDataCache[enemyID];
        }

        Debug.LogError($"Fixed Enemy Data ID {enemyID} not found in the cache.");
        return null;
    }

    /// <summary>
    /// 3体の固定エネミーデータを辞書に定義します。
    /// </summary>
    private static void InitializeFixedData()
    {
        _fixedEnemyDataCache = new Dictionary<int, EnemyData>();

        // ID 1: レッドドラゴン (高HP、中攻撃)
        _fixedEnemyDataCache.Add(1, new EnemyData 
        { 
            EnemyID = 1, 
            EnemyName = "RED DRAGON", 
            MaxHP = 300, 
            BaseAttackDamage = 25, 
            Attribute = ElementType.Fire,
            visualAssetPath = "dragon_red" 
        });

        // ID 2: アイスゴーレム (高HP、低攻撃)
        _fixedEnemyDataCache.Add(2, new EnemyData 
        { 
            EnemyID = 2, 
            EnemyName = "ICE GOLEM", 
            MaxHP = 500, 
            BaseAttackDamage = 15, 
            Attribute = ElementType.Water,
            visualAssetPath = "golem_ice" 
        });

        // ID 3: シャドウナイト (低HP、高攻撃)
        _fixedEnemyDataCache.Add(3, new EnemyData 
        { 
            EnemyID = 3, 
            EnemyName = "SHADOW KNIGHT", 
            MaxHP = 200, 
            BaseAttackDamage = 40, 
            Attribute = ElementType.Dark,
            visualAssetPath = "knight_shadow" 
        });
        
        Debug.Log("Fixed Enemy Data initialized with 3 entries.");
    }
}
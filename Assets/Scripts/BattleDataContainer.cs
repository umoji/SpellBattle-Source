using UnityEngine;
using System.Collections.Generic;
using System;

// MonoBehaviourを継承して、BattleControllerにアタッチします。
public class BattleDataContainer : MonoBehaviour
{
    // --- ライブデータへの参照 ---
    
    // プレイヤーの戦闘データ（初期化済みのインスタンスを保持）
    [SerializeField]
    public PlayerBattleData playerData = new PlayerBattleData();
    
    // エネミーの戦闘データ（BattleManagerが適切なEnemyDataを読み込み、初期化します）
    [SerializeField]
    public EnemyBattleData enemyData; 
}


// ==========================================================
// 内部クラス定義
// ==========================================================

// プレイヤーのライブデータ構造
[Serializable]
public class PlayerBattleData
{
    // --- プレイヤーの基本ステータス ---
    public int maxHP = 1000;      // 最大HP（初期設定値）
    public int currentHP;         // 現在のHP
    
    // --- コスト管理 ---
    public int currentMaxCost;    // 現在の最大コスト（初期値3、最大10）
    public int currentCost;       // 現在使用可能なコスト
    
    // --- カード管理 ---
    // ★修正箇所★: 山札はID (int) で管理
    public List<int> deck = new List<int>();            // 山札 (カードIDのリスト)
    
    // 手札はUI表示や効果処理のためにCardDataオブジェクトで管理
    public List<CardData> hand = new List<CardData>();          // 手札 (CardDataのリスト)
    
    // ★修正箇所★: 墓地はID (int) で管理
    public List<int> discardPile = new List<int>();     // 墓地 (カードIDのリスト)
    
    // --- 状態異常管理 ---
    public List<StatusEffect> statusEffects = new List<StatusEffect>();  

    // コンストラクタ（初期化処理）
    public PlayerBattleData()
    {
        currentHP = maxHP;
        currentMaxCost = 3; 
        currentCost = 3;
    }
}


// エネミーのライブデータ構造
[Serializable]
public class EnemyBattleData
{
    // エネミーの静的データへの参照
    public EnemyData enemyData; 
    
    // --- エネミーの基本ステータス ---
    public int currentHP; // 現在のHP
    
    // --- 攻撃パターン管理 ---
    public bool isCharging;         // チャージ攻撃中かどうかのフラグ
    public int chargeCounter = 0;   // チャージ中のターンカウンター
    
    // --- 状態異常管理 ---
    public List<StatusEffect> statusEffects = new List<StatusEffect>();  

    // コンストラクタ（初期化処理）
    // BattleManagerが、どのエネミーと戦うか（EnemyData）を指定して生成します。
    public EnemyBattleData(EnemyData data)
    {
        enemyData = data;
        this.currentHP = data.MaxHP; // 現在のHPを最大HPで初期化
        this.isCharging = false;
        this.chargeCounter = 0;
    }
    
    // デフォルトコンストラクタ（UnityのInspector表示と初期化用）
    public EnemyBattleData()
    {
        // 意図的に空
    }
}
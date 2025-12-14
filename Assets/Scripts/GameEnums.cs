using UnityEngine;

// 属性の定義
public enum ElementType
{
    None,       // 属性なし
    Fire,       // 火
    Water,      // 水
    Earth,      // 土
    Wind,       // 風
    Thunder,    // 雷
    Light,      // 光
    Dark        // 闇
}

// レアリティの定義
public enum CardRarity
{
    // N (Normal) を削除
    R,      // 30枚
    SR,     // 21枚
    SSR     // 7枚
}

// カードが実行する効果の種類
public enum EffectType
{
    None,            // 効果なし（基本攻撃など）
    DamageTarget,    // 敵にダメージを与える
    HealPlayer,      // 自身のHPを回復する
    ApplyStatusToEnemy, // 敵に状態異常を付与する
    ApplyStatusToPlayer, // 自身に状態異常を付与する
    GainCost,        // コストを回復/増加させる
    DrawCard,        // カードを引く（ドロー）
    ClearStatus,     // 状態異常を解除する
    // ... 必要に応じて今後追加
}

// 状態異常の種類
public enum StatusEffectType
{
    Poison,     // 毒：ターン終了時に継続ダメージ
    Paralyze,   // 麻痺：次のターン行動不能
    Silence,    // 沈黙：次のターン魔法カード使用不可
    Vulnerable, // 脆弱：受けるダメージが増加
    Barrier,    // バリア：受けるダメージを軽減（状態異常ではないが、同じ仕組みで管理）
}
using UnityEngine;
using System;

[Serializable]
public class StatusEffect
{
    // 状態異常の種類
    public StatusEffectType type; 
    
    // 残り持続ターン数（ターン終了時に1減少）
    public int duration;
    
    // 効果の強度や倍率（例：毒のダメージ量、脆弱のダメージ増加倍率）
    public float value;

    // どのオブジェクトに付与されているかを識別するID（ゲーム実行時に設定）
    [NonSerialized] // Unityエディタでは表示しない
    public int targetEntityID; 
}
using UnityEngine;
using System; // [Serializable]属性を使うために必要

// このクラスのデータはUnityエディタでシリアライズ（保存・表示）されます
[Serializable]
public class CardData
{
    // --- 静的データ（カードの基本情報） ---
    public int CardID;
    public string CardName; 
    public string visualAssetPath; // エネミー画像と共通のビジュアルパスを格納
    
    // --- ゲームバランスデータ ---
    public ElementType Attribute; // 属性（火、水、風など）- GameEnums.csで定義
    public int Number;             // Number
    public CardRarity Rarity;    // レアリティ (N, R, SR, SSR) - GameEnums.csで定義

    // --- 効果データ ---
    public EffectType EffectType; // カードの効果の種類
    public string EffectText;    // 効果の説明文
    
    // 汎用的な数値として保持（ダメージ量、回復量、コスト回復量など）
    public int Power; 
}
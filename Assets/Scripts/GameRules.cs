using UnityEngine;

// static クラスは、ゲームオブジェクトにアタッチせず、どこからでも呼び出せる関数を定義するために使います。
public static class GameRules
{
    // 攻撃側の属性と防御側の属性を受け取り、ダメージ倍率を返す
    public static float GetDamageMultiplier(ElementType attacker, ElementType defender)
    {
        float multiplier = 1.0f; // デフォルトは1倍
        
        // 攻撃側を基準に判定
        switch (attacker)
        {
            case ElementType.Water:
                if (defender == ElementType.Fire) multiplier = 1.5f; // 水 > 火
                if (defender == ElementType.Thunder) multiplier = 0.5f; // 水 < 雷
                break;
            case ElementType.Fire:
                if (defender == ElementType.Wind) multiplier = 1.5f; // 火 > 風
                if (defender == ElementType.Water) multiplier = 0.5f; // 火 < 水
                break;
            case ElementType.Wind:
                if (defender == ElementType.Earth) multiplier = 1.5f; // 風 > 土
                if (defender == ElementType.Fire) multiplier = 0.5f; // 風 < 火
                break;
            case ElementType.Earth:
                if (defender == ElementType.Thunder) multiplier = 1.5f; // 土 > 雷
                if (defender == ElementType.Wind) multiplier = 0.5f; // 土 < 風
                break;
            case ElementType.Thunder:
                if (defender == ElementType.Water) multiplier = 1.5f; // 雷 > 水
                if (defender == ElementType.Earth) multiplier = 0.5f; // 雷 < 土
                break;

            case ElementType.Light:
                if (defender == ElementType.Dark) multiplier = 1.5f; // 光 > 闇
                break;
            case ElementType.Dark:
                if (defender == ElementType.Light) multiplier = 1.5f; // 闇 > 光
                break;
        }

        return multiplier;
    }
}
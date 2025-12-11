using UnityEngine;
using UnityEngine.EventSystems;

// このコンポーネントは、カード使用のドロップ領域をマークするために使用されます。
// ドラッグ&ドロップの処理自体は CardUI.cs の OnEndDrag で行われます。
public class UseZone : MonoBehaviour
{
    // 何もロジックは持ちませんが、タグとして機能します
    // このコンポーネントがアタッチされた領域が、カードを使用するエリアになります。
}
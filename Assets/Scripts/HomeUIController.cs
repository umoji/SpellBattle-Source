using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HomeUIController : MonoBehaviour
{
    [Header("Gacha UI References")]
    public Button gachaButton;
    public GameObject gachaResultPanel; 
    public TextMeshProUGUI resultCardNameText;
    public TextMeshProUGUI resultCardRarityText;
    public Button closeResultButton; 
	public Button drawAgainButton;

    // ★修正点 1: 古い Image 参照を削除し、Prefabと親Transformを追加★
    [Header("Card Prefabs")]
    public GameObject cardUIPrefab; // InventoryUIControllerで使用しているものと同じPrefab
	
	[Header("Gacha Prefabs")]
	public GameObject gachaResultCardPrefab;

    [Header("Gacha Result Display")]
    public Transform cardDisplayParent; // CardUIのインスタンスを置く場所
    
    private GameObject currentCardInstance = null; // 生成したCardUIインスタンスを保持

    void Start()
    {
        if (CardManager.Instance == null)
        {
            Debug.LogError("CardManager Instance is missing. Cannot initialize Home UI.");
            return;
        }

        SetupGachaListeners();
    }
    
/// <summary>
    /// ガチャボタンと閉じるボタン、そして「もう一回！」ボタンのリスナーを登録します。
    /// </summary>
    private void SetupGachaListeners()
    {
        // ガチャボタンにリスナーを登録
        if (gachaButton != null)
        {
            gachaButton.onClick.RemoveAllListeners(); 
            gachaButton.onClick.AddListener(OnGachaButtonClicked); 
        }
        
        // 閉じるボタンにリスナーを登録
        if (closeResultButton != null)
        {
            closeResultButton.onClick.RemoveAllListeners();
            closeResultButton.onClick.AddListener(CloseGachaResult);
        }
        
        // 「もう一回！」ボタンにリスナーを登録
        if (drawAgainButton != null)
        {
            drawAgainButton.onClick.RemoveAllListeners();
            // 既存の OnGachaButtonClicked メソッドを呼び出すことで、再度ガチャを引く
            drawAgainButton.onClick.AddListener(OnGachaButtonClicked); 
        }
        
        // 初期状態では結果パネルを非表示
        if (gachaResultPanel != null)
        {
            gachaResultPanel.SetActive(false);
        }
    }

    /// <summary>
    /// ガチャボタンが押されたときの処理：ガチャを引き、結果を表示する。
    /// </summary>
    public void OnGachaButtonClicked()
    {
        if (CardManager.Instance == null) return;
        
        // TODO: ここにコスト消費ロジック（通貨やチケット）を追加
        
        CardData resultCard = CardManager.Instance.DrawGacha();
        
        if (resultCard != null)
        {
            // 結果のUIを表示
            ShowGachaResult(resultCard);
        }
    }

    /// <summary>
    /// ガチャ結果パネルを表示し、内容を更新する
    /// </summary>
    private void ShowGachaResult(CardData card)
    {
        if (gachaResultPanel != null)
        {
            gachaResultPanel.SetActive(true);
        }
        
        // 既存のインスタンスのクリーンアップ
        if (currentCardInstance != null)
        {
            Destroy(currentCardInstance);
            currentCardInstance = null;
        }

        // --------------------------------------------------------
        // ★CardUI Prefabをインスタンス化し、初期化するロジック★
        // --------------------------------------------------------
		if (gachaResultCardPrefab != null && cardDisplayParent != null) // ★Prefab参照名を修正★
        {
            // 1. Prefabを親オブジェクトの下に生成 (★使用するPrefabを変更★)
            GameObject cardObject = Instantiate(gachaResultCardPrefab, cardDisplayParent); 
            currentCardInstance = cardObject;

            // 2. CardUIコンポーネントを取得し、データを設定
            CardUI cardUI = cardObject.GetComponent<CardUI>();

            if (cardUI != null)
            {
                // CardDataを設定 (BattleManagerは不要なので null)
                cardUI.SetupCard(card, null);
            }
            else
            {
                Debug.LogError("Instantiated cardPrefab does not have CardUI component!");
            }
        }
        // --------------------------------------------------------
        
        // テキスト情報を設定
        if (resultCardNameText != null)
        {
            resultCardNameText.text = card.CardName;
        }
        
        if (resultCardRarityText != null)
        {
            string rarityString = card.Rarity.ToString();
            resultCardRarityText.text = rarityString;
            
            // レアリティに応じて色を調整
            Color rarityColor = Color.white;
            switch (card.Rarity)
            {
                case CardRarity.SSR:
                    rarityColor = Color.yellow; 
                    break;
                case CardRarity.SR:
                    rarityColor = new Color(0.1f, 0.7f, 1f); 
                    break;
                case CardRarity.R:
                    rarityColor = Color.green; 
                    break;
            }
            resultCardRarityText.color = rarityColor;
        }
        
        // ★修正点 3: 古い resultCardImage を使用するロジックは全て削除されたため、CS0103エラーは解消されます。
    }


    /// <summary>
    /// ガチャ結果パネルを閉じる (クリーンアップを追加)
    /// </summary>
    public void CloseGachaResult()
    {
        if (gachaResultPanel != null)
        {
            gachaResultPanel.SetActive(false);
        }
        
        // 閉じるときにインスタンスを破棄してメモリを解放
        if (currentCardInstance != null)
        {
            Destroy(currentCardInstance);
            currentCardInstance = null;
        }
    }
}
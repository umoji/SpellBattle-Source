using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections;
using System.Collections.Generic;

public class SceneLoader : MonoBehaviour
{
    // ★★★ シングルトンインスタンス ★★★
    public static SceneLoader Instance { get; private set; }

    // HomeSceneでボタンを動的検索するために使用する定数名
    private const string BATTLE_BUTTON_NAME = "StageButton";     
    private const string INVENTORY_BUTTON_NAME = "InventoryButton";
    
    // InventoryScene内のHOMEボタンのGameObject名を "HomeButton" に設定
    private const string INVENTORY_HOME_BUTTON_NAME = "HomeButton"; 
    
    // ★修正点★: GachaButton関連の定数は HomeUIController に移譲するため削除
    
    private const string HOME_SCENE_NAME = "HomeScene";
    private const string BATTLE_SCENE_NAME = "BattleScene";
    private const string INVENTORY_SCENE_NAME = "InventoryScene"; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // シーンをまたいでも破棄されないようにする
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // シーンがロードされたときにイベントを購読
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 最初のシーン（HomeSceneであると仮定）のイベントをセットアップ
        SetupSceneButtons(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        // オブジェクトが破棄される前にイベントの購読を解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// シーンロード完了時に呼ばれるコールバック
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 常にボタンを再セットアップ
        SetupSceneButtons(scene);
    }

    /// <summary>
    /// 各シーンのボタンイベントをコードで動的に割り当てます。
    /// </summary>
    private void SetupSceneButtons(Scene scene)
    {
        if (scene.name == HOME_SCENE_NAME)
        {
            Debug.Log("HomeScene loaded. Attempting to re-initialize buttons dynamically.");

            // 1. BattleSceneへのボタン（StageButton）の検索と再登録
            GameObject stageButtonObj = GameObject.Find(BATTLE_BUTTON_NAME);
            if (stageButtonObj != null)
            {
                Button stageButton = stageButtonObj.GetComponent<Button>();
                if (stageButton != null)
                {
                    stageButton.onClick.RemoveAllListeners(); 
                    stageButton.onClick.AddListener(LoadBattleScene); 
                    Debug.Log($"{BATTLE_BUTTON_NAME} event re-initialized.");
                }
            }
            else
            {
                Debug.LogError($"CRITICAL: {BATTLE_BUTTON_NAME} GameObject not found in HomeScene. Check name.");
            }
            
            // 2. InventorySceneへのボタン（InventoryButton）の検索と再登録
            GameObject inventoryButtonObj = GameObject.Find(INVENTORY_BUTTON_NAME);
            if (inventoryButtonObj != null)
            {
                Button inventoryButton = inventoryButtonObj.GetComponent<Button>();
                if (inventoryButton != null)
                {
                    inventoryButton.onClick.RemoveAllListeners(); 
                    inventoryButton.onClick.AddListener(LoadInventoryScene); 
                    Debug.Log($"{INVENTORY_BUTTON_NAME} event re-initialized.");
                }
            }
            else
            {
                Debug.LogError($"CRITICAL: {INVENTORY_BUTTON_NAME} GameObject not found in HomeScene. Check name.");
            }
            
            // ★修正点★: GachaButtonのイベント登録は HomeUIController に移譲するため、ここにはロジックを追加しない。
        }
        
        // InventorySceneのHomeボタン設定ロジック
        else if (scene.name == INVENTORY_SCENE_NAME) 
        {
            Debug.Log("InventoryScene loaded. Setting up Home button listener.");
            
            // Homeボタンを検索し、HomeSceneへの遷移を設定
            GameObject homeButtonObj = GameObject.Find(INVENTORY_HOME_BUTTON_NAME);
            
            if (homeButtonObj != null)
            {
                Button homeButton = homeButtonObj.GetComponent<Button>();
                if (homeButton != null)
                {
                    homeButton.onClick.RemoveAllListeners();
                    homeButton.onClick.AddListener(LoadHomeScene); // LoadHomeSceneを設定
                    Debug.Log($"{INVENTORY_HOME_BUTTON_NAME} event re-initialized successfully.");
                }
                else
                {
                    Debug.LogError($"Inventory Home Button found, but Button component is missing on: {INVENTORY_HOME_BUTTON_NAME}.");
                }
            }
            else
            {
                Debug.LogError($"CRITICAL: Inventory Scene Home Button ('{INVENTORY_HOME_BUTTON_NAME}') not found. Check GameObject name in Hierarchy.");
            }
        }
        
        // バトルシーンに入ったとき、古いHomeSceneのボタン参照をクリア (メモリ管理のため)
        else if (scene.name == BATTLE_SCENE_NAME)
        {
             Debug.Log("Entered BattleScene. Clearing Home button references.");
        }
    }

    // -------------------------------------------------------------------
    // シーン遷移メソッド
    // -------------------------------------------------------------------

    public void LoadHomeScene()
    {
        Debug.Log("Loading Home Scene...");
        StartCoroutine(LoadSceneAsync(HOME_SCENE_NAME)); 
    }

    public void LoadBattleScene()
    {
        Debug.Log("Loading Battle Scene...");
        StartCoroutine(LoadSceneAsync(BATTLE_SCENE_NAME));
    }
    
    public void LoadInventoryScene()
    {
        Debug.Log("Loading Inventory Scene...");
        StartCoroutine(LoadSceneAsync(INVENTORY_SCENE_NAME));
    }

    // 非同期ロードのコルーチン
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is empty.");
            yield break;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        Debug.Log($"Scene '{sceneName}' loaded successfully.");
    }
}
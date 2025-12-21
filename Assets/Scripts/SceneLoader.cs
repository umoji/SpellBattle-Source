using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections;
using System.Collections.Generic;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private const string BATTLE_BUTTON_NAME = "StageButton";     
    private const string INVENTORY_BUTTON_NAME = "InventoryButton";
    private const string INVENTORY_HOME_BUTTON_NAME = "HomeButton"; 
    
    private const string HOME_SCENE_NAME = "HomeScene";
    private const string BATTLE_SCENE_NAME = "BattleScene";
    private const string INVENTORY_SCENE_NAME = "InventoryScene"; 

    // ★追加: 二重ロードを防ぐためのフラグ
    private bool isLoading = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SetupSceneButtons(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ロード完了したのでフラグを下ろす
        isLoading = false;
        SetupSceneButtons(scene);
    }

    private void SetupSceneButtons(Scene scene)
    {
        if (scene.name == HOME_SCENE_NAME)
        {
            Debug.Log("HomeScene loaded. Re-initializing buttons.");

            GameObject stageButtonObj = GameObject.Find(BATTLE_BUTTON_NAME);
            if (stageButtonObj != null)
            {
                Button stageButton = stageButtonObj.GetComponent<Button>();
                if (stageButton != null)
                {
                    stageButton.onClick.RemoveAllListeners(); 
                    stageButton.onClick.AddListener(LoadBattleScene); 
                }
            }
            
            GameObject inventoryButtonObj = GameObject.Find(INVENTORY_BUTTON_NAME);
            if (inventoryButtonObj != null)
            {
                Button inventoryButton = inventoryButtonObj.GetComponent<Button>();
                if (inventoryButton != null)
                {
                    inventoryButton.onClick.RemoveAllListeners(); 
                    inventoryButton.onClick.AddListener(LoadInventoryScene); 
                }
            }
        }
        else if (scene.name == INVENTORY_SCENE_NAME) 
        {
            GameObject homeButtonObj = GameObject.Find(INVENTORY_HOME_BUTTON_NAME);
            if (homeButtonObj != null)
            {
                Button homeButton = homeButtonObj.GetComponent<Button>();
                if (homeButton != null)
                {
                    homeButton.onClick.RemoveAllListeners();
                    homeButton.onClick.AddListener(LoadHomeScene); 
                }
            }
        }
    }

    // -------------------------------------------------------------------
    // シーン遷移メソッド
    // -------------------------------------------------------------------

    public void LoadHomeScene() => SafeLoadScene(HOME_SCENE_NAME);
    public void LoadBattleScene() => SafeLoadScene(BATTLE_SCENE_NAME);
    public void LoadInventoryScene() => SafeLoadScene(INVENTORY_SCENE_NAME);

    // ★安全にロードを開始するための共通メソッド
    private void SafeLoadScene(string sceneName)
    {
        if (isLoading) return; // すでにロード中なら無視
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is empty.");
            yield break;
        }

        isLoading = true; // ロード開始フラグ
        Debug.Log($"Starting Async Load: {sceneName}");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // ★asyncLoadがNullでないことを確認
        if (asyncLoad == null)
        {
            Debug.LogError($"Failed to start LoadSceneAsync for {sceneName}");
            isLoading = false;
            yield break;
        }

        // ロード完了まで待機
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        Debug.Log($"Scene '{sceneName}' load process finished.");
    }
}
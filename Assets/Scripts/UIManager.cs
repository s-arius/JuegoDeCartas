using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Elements")]
    public TMP_Text turnText;
    public TMP_Text gameOverText;
    public Transform scoresParent;
    public GameObject scoreEntryPrefab; // Prefab con TMP_Text para nombre y puntuación

    [Header("Network Buttons")]
    public Button hostButton;
    public Button clientButton;
    public Button disconnectButton;

    private Dictionary<ulong, GameObject> scoreEntries = new Dictionary<ulong, GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Conectar botones a funciones públicas
        if (hostButton != null) hostButton.onClick.AddListener(StartHost);
        if (clientButton != null) clientButton.onClick.AddListener(StartClient);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(Disconnect);

        gameOverText.gameObject.SetActive(false);

        // Crear score entries para clientes ya conectados
        if (NetworkManager.Singleton != null)
        {
            foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
            {
                CreateOrUpdateScoreEntry(c.ClientId);
            }

            NetworkManager.Singleton.OnClientConnectedCallback += (id) => CreateOrUpdateScoreEntry(id);
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
            {
                if (scoreEntries.ContainsKey(id))
                {
                    Destroy(scoreEntries[id]);
                    scoreEntries.Remove(id);
                }
            };
        }
    }

    // =================== Botones de red ===================
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
    }

    // =================== UI de juego ===================
    void CreateOrUpdateScoreEntry(ulong clientId)
    {
        if (scoreEntries.ContainsKey(clientId)) return;

        GameObject go = Instantiate(scoreEntryPrefab, scoresParent);
        TMP_Text[] texts = go.GetComponentsInChildren<TMP_Text>();
        if (texts.Length >= 2)
        {
            texts[0].text = $"Player {clientId}";
            texts[1].text = "Score: 0";
        }

        scoreEntries.Add(clientId, go);
    }

    public void UpdateScoreDisplay(ulong clientId, int score)
    {
        if (!scoreEntries.ContainsKey(clientId)) CreateOrUpdateScoreEntry(clientId);

        GameObject go = scoreEntries[clientId];
        TMP_Text[] texts = go.GetComponentsInChildren<TMP_Text>();
        if (texts.Length >= 2)
        {
            texts[1].text = $"Score: {score}";
        }
    }

    public void SetCurrentTurn(ulong clientId)
    {
        if (turnText != null)
            turnText.text = $"Turno: Player {clientId}";
    }

    public void ShowGameOver()
    {
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "Juego terminado!";
        }
    }
}

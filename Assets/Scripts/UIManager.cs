using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Elements")]
    public Text turnText;
    public Text gameOverText;
    public Transform scoresParent;
    public GameObject scoreEntryPrefab; // prefab with Text components: name + score

    private Dictionary<ulong, GameObject> scoreEntries = new Dictionary<ulong, GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        gameOverText.gameObject.SetActive(false);
        // create score entries for connected players
        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            CreateOrUpdateScoreEntry(c.ClientId);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += (id) => CreateOrUpdateScoreEntry(id);
        NetworkManager.Singleton.OnClientDisconnectCallback += (id) => {
            if (scoreEntries.ContainsKey(id))
            {
                Destroy(scoreEntries[id]);
                scoreEntries.Remove(id);
            }
        };
    }

    void CreateOrUpdateScoreEntry(ulong clientId)
    {
        if (scoreEntries.ContainsKey(clientId)) return;

        GameObject go = Instantiate(scoreEntryPrefab, scoresParent);
        var texts = go.GetComponentsInChildren<Text>();
        if (texts.Length >= 2)
        {
            texts[0].text = $"Player {clientId}";
            texts[1].text = "Score: 0";
        }
        scoreEntries.Add(clientId, go);
    }

    public void UpdateScoreDisplay(NetworkPlayer player)
    {
        ulong clientId = player.OwnerClientId;
        if (!scoreEntries.ContainsKey(clientId)) CreateOrUpdateScoreEntry(clientId);

        var go = scoreEntries[clientId];
        var texts = go.GetComponentsInChildren<Text>();
        if (texts.Length >= 2)
        {
            texts[1].text = $"Score: {player.score.Value}";
        }
    }

    public void SetCurrentTurn(ulong clientId)
    {
        if (turnText != null)
            turnText.text = $"Turno: Player {clientId}";
    }

    public void ShowGameOver()
    {
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = "Juego terminado!";
    }
}

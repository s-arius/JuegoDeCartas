using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Score UI")]
    public TMP_Text p1ScoreText;
    public TMP_Text p2ScoreText;

    [Header("Winner Panel")]
    public GameObject winnerPanel;
    public TMP_Text winnerText;

    [Header("Network Buttons")]
    public Button hostButton;
    public Button clientButton;

    [Header("Otros Paneles")]
    public GameObject ScorePanel;
    public GameObject Panel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (hostButton != null) hostButton.onClick.AddListener(StartHost);
        if (clientButton != null) clientButton.onClick.AddListener(StartClient);

        if (winnerPanel != null) winnerPanel.SetActive(false);

        UpdateScores(0, 0);
    }

    public void StartHost() => NetworkManager.Singleton.StartHost();
    public void StartClient() => NetworkManager.Singleton.StartClient();

    public void UpdateScores(int p1, int p2)
    {
        if (p1ScoreText != null) p1ScoreText.text = $"P1: {p1}";
        if (p2ScoreText != null) p2ScoreText.text = $"P2: {p2}";
    }

    public void ShowWinner(int p1Score, int p2Score)
    {
        if (winnerPanel == null || winnerText == null) return;

        if (ScorePanel != null) ScorePanel.SetActive(false);
        if (Panel != null) Panel.SetActive(false);
        if (hostButton != null) hostButton.gameObject.SetActive(false);
        if (clientButton != null) clientButton.gameObject.SetActive(false);

        winnerPanel.SetActive(true);

        if (p1Score > p2Score)
            winnerText.text = "¡Gana Player 1!";
        else if (p2Score > p1Score)
            winnerText.text = "¡Gana Player 2!";
        else
            winnerText.text = "¡Empate!";
    }
}

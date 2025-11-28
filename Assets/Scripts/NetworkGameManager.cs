using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class NetworkGameManager : NetworkBehaviour
{
    [Header("Board")]
    public GameObject cardPrefab;
    public Transform boardParent;
    public int columns = 4;
    public int rows = 3;
    public float spacingX = 1.5f;
    public float spacingY = 2f;

    public Vector3 boardStartPosition = new Vector3(-3f, 3f, 0f);

    [Header("Game")]
    public float mismatchRevealTime = 1.0f;

    public NetworkVariable<int> scoreP1 = new NetworkVariable<int>(0);
    public NetworkVariable<int> scoreP2 = new NetworkVariable<int>(0);

    public static NetworkGameManager Instance;

    private List<NetworkCard> spawnedCards = new List<NetworkCard>();

    private Dictionary<ulong, NetworkCard> firstCard = new Dictionary<ulong, NetworkCard>();
    private Dictionary<ulong, NetworkCard> secondCard = new Dictionary<ulong, NetworkCard>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartCoroutine(SetupBoardRoutine());

        scoreP1.OnValueChanged += (_, __) => UpdateUI();
        scoreP2.OnValueChanged += (_, __) => UpdateUI();
    }

    void UpdateUI()
    {
        UIManager.Instance?.UpdateScores(scoreP1.Value, scoreP2.Value);
    }

    IEnumerator SetupBoardRoutine()
    {
        yield return null;
        SetupBoard();
    }

    void SetupBoard()
    {
        int total = columns * rows;
        if (total % 2 != 0)
        {
            Debug.LogError("Total cards must be even.");
            return;
        }

        List<int> ids = new List<int>();
        int pairs = total / 2;

        for (int i = 0; i < pairs; i++)
        {
            ids.Add(i);
            ids.Add(i);
        }

        ids = ids.OrderBy(x => Random.value).ToList();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int index = r * columns + c;
                int id = ids[index];

                Vector3 pos = boardStartPosition + new Vector3(c * spacingX, -r * spacingY, 0);

                GameObject go = Instantiate(cardPrefab, pos, Quaternion.identity, boardParent);
                NetworkObject no = go.GetComponent<NetworkObject>();
                no.Spawn(true);

                NetworkCard nc = go.GetComponent<NetworkCard>();
                nc.SetCardIdServerRpc(id);

                spawnedCards.Add(nc);
            }
        }
    }

    public void CardFlipped(NetworkCard card, ulong clientId)
    {
        if (!IsServer) return;
        if (card == null || card.isFaceUp.Value || card.isMatched.Value) return;

        if (secondCard.ContainsKey(clientId) && secondCard[clientId] != null)
            return;

        card.RevealServerRpc();

        if (!firstCard.ContainsKey(clientId) || firstCard[clientId] == null)
            firstCard[clientId] = card;
        else
        {
            secondCard[clientId] = card;
            StartCoroutine(CheckMatchRoutine(clientId));
        }
    }

    IEnumerator CheckMatchRoutine(ulong clientId)
    {
        yield return new WaitForSeconds(0.2f);

        NetworkCard a = firstCard[clientId];
        NetworkCard b = secondCard[clientId];

        if (a == null || b == null)
            yield break;

        if (a.cardId.Value == b.cardId.Value)
        {
            AddScoreToPlayerSimple(clientId);

            a.SetMatchedServerRpc();
            b.SetMatchedServerRpc();
        }
        else
        {
            yield return new WaitForSeconds(mismatchRevealTime);
            a.HideServerRpc();
            b.HideServerRpc();
        }

        firstCard[clientId] = null;
        secondCard[clientId] = null;

        if (spawnedCards.All(c => c.isMatched.Value))
        {
            Debug.Log("Juego terminado");
            GameOverClientRpc(scoreP1.Value, scoreP2.Value);
        }
    }

    void AddScoreToPlayerSimple(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId && IsServer)
            scoreP1.Value++;
        else
            scoreP2.Value++;
    }

    [ClientRpc]
    void GameOverClientRpc(int finalScoreP1, int finalScoreP2)
    {
        UIManager.Instance?.ShowWinner(finalScoreP1, finalScoreP2);
    }
}

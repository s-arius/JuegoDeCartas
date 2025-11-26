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

    [Header("Game")]
    public float mismatchRevealTime = 1.0f;

    // Server-side state
    private List<NetworkCard> spawnedCards = new List<NetworkCard>();
    private NetworkCard firstFlipped = null;
    private NetworkCard secondFlipped = null;

    // Keep track of which client is current turn (optional)
    private ulong currentTurnClientId = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(SetupBoardRoutine());
        }
    }

    IEnumerator SetupBoardRoutine()
    {
        yield return null; // let network initialize
        SetupBoard();
        // assign first turn to host
        if (NetworkManager.ConnectedClientsList.Count > 0)
            currentTurnClientId = NetworkManager.ConnectedClientsList[0].ClientId;
        UpdateTurnClientRpc(currentTurnClientId);
    }

    void SetupBoard()
    {
        int total = columns * rows;
        if (total % 2 != 0)
        {
            Debug.LogError("Total cards must be even.");
            return;
        }

        // create list of ids (pairs)
        List<int> ids = new List<int>();
        int pairs = total / 2;
        for (int i = 0; i < pairs; i++)
        {
            ids.Add(i);
            ids.Add(i);
        }

        // shuffle ids
        System.Random rng = new System.Random();
        ids = ids.OrderBy(a => rng.Next()).ToList();

        // spawn cards in grid
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int index = r * columns + c;
                int id = ids[index];
                Vector3 pos = new Vector3(c * spacingX, -r * spacingY, 0);
                GameObject go = Instantiate(cardPrefab, pos, Quaternion.identity, boardParent);
                var no = go.GetComponent<NetworkObject>();
                no.Spawn(true);

                NetworkCard nc = go.GetComponent<NetworkCard>();
                nc.SetCardIdServerRpc(id);
                spawnedCards.Add(nc);
            }
        }
    }

    // Called by cards when a client requests flip
    public void RequestFlip(NetworkCard card, ulong clientId)
    {
        if (!IsServer) return;
        // Only allow flip if it's that client's turn (optional rule). Remove check if you want simultaneous play:
        if (currentTurnClientId != clientId)
            return;

        if (firstFlipped == null)
        {
            firstFlipped = card;
            card.RevealClientRpc();
        }
        else if (secondFlipped == null && card != firstFlipped)
        {
            secondFlipped = card;
            card.RevealClientRpc();
            StartCoroutine(CheckMatchRoutine(clientId));
        }
    }

    IEnumerator CheckMatchRoutine(ulong clientId)
    {
        yield return new WaitForSeconds(0.2f); // small buffer for visuals

        if (firstFlipped.cardId.Value == secondFlipped.cardId.Value)
        {
            // match: award points to clientId
            AddScoreToPlayer(clientId, 1); // 1 punto por pareja (ajustable)

            // disable both cards
            firstFlipped.SetMatchedClientRpc();
            secondFlipped.SetMatchedClientRpc();
        }
        else
        {
            // mismatch: reveal for a bit then hide
            yield return new WaitForSeconds(mismatchRevealTime);
            firstFlipped.HideClientRpc();
            secondFlipped.HideClientRpc();

            // optionally change turn to other player
            SwitchTurn();
        }

        firstFlipped = null;
        secondFlipped = null;

        // check endgame: all matched?
        if (spawnedCards.All(c => c.isMatched.Value))
        {
            // handle end of game
            GameOverClientRpc();
        }
    }

    void AddScoreToPlayer(ulong clientId, int amount)
    {
        // Find player's NetworkPlayer component and add score via ServerRpc
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var go = client.PlayerObject != null ? client.PlayerObject.GameObject : null;
            if (go != null)
            {
                var np = go.GetComponent<NetworkPlayer>();
                if (np != null)
                    np.AddScoreServerRpc(amount);
            }
        }
    }

    void SwitchTurn()
    {
        // simple: pick next connected client
        var clients = NetworkManager.ConnectedClientsList;
        if (clients.Count < 2) return;
        int idx = clients.FindIndex(c => c.ClientId == currentTurnClientId);
        idx = (idx + 1) % clients.Count;
        currentTurnClientId = clients[idx].ClientId;
        UpdateTurnClientRpc(currentTurnClientId);
    }

    [ClientRpc]
    void UpdateTurnClientRpc(ulong clientId)
    {
        UIManager.Instance?.SetCurrentTurn(clientId);
    }

    [ClientRpc]
    void GameOverClientRpc()
    {
        UIManager.Instance?.ShowGameOver();
    }
}

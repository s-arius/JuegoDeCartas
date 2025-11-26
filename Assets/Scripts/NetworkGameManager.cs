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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(SetupBoardRoutine());
        }
    }

    IEnumerator SetupBoardRoutine()
    {
        yield return null; // Espera a que la red inicialice
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

        // Crear IDs para pares
        List<int> ids = new List<int>();
        int pairs = total / 2;
        for (int i = 0; i < pairs; i++)
        {
            ids.Add(i);
            ids.Add(i);
        }

        // Mezclar IDs
        System.Random rng = new System.Random();
        ids = ids.OrderBy(a => rng.Next()).ToList();

        // Spawn cartas en la grilla
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int index = r * columns + c;
                int id = ids[index];
                Vector3 pos = new Vector3(c * spacingX, -r * spacingY, 0);
                GameObject go = Instantiate(cardPrefab, pos, Quaternion.identity, boardParent);
                NetworkObject no = go.GetComponent<NetworkObject>();
                no.Spawn(true);

                NetworkCard nc = go.GetComponent<NetworkCard>();
                nc.SetCardIdServerRpc(id);
                spawnedCards.Add(nc);
            }
        }
    }

    // Método que llama NetworkPlayer al tocar una carta
    public void CardFlipped(NetworkCard card, ulong clientId)
    {
        if (!IsServer || card == null || card.isFaceUp.Value || card.isMatched.Value) return;

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
        yield return new WaitForSeconds(0.2f);

        if (firstFlipped.cardId.Value == secondFlipped.cardId.Value)
        {
            // Coincidencia: sumar puntos al jugador
            AddScoreToPlayer(clientId, 1);

            // Marcar cartas como emparejadas
            firstFlipped.SetMatchedClientRpc();
            secondFlipped.SetMatchedClientRpc();
        }
        else
        {
            // No coinciden: mostrar un momento y luego ocultar
            yield return new WaitForSeconds(mismatchRevealTime);
            firstFlipped.HideClientRpc();
            secondFlipped.HideClientRpc();
        }

        firstFlipped = null;
        secondFlipped = null;

        // Comprobar fin de juego
        if (spawnedCards.All(c => c.isMatched.Value))
        {
            GameOverClientRpc();
        }
    }

    void AddScoreToPlayer(ulong clientId, int amount)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var go = client.PlayerObject != null ? client.PlayerObject.gameObject : null;
            if (go != null)
            {
                var np = go.GetComponent<NetworkPlayer>();
                if (np != null)
                    np.score.Value += amount; // NetworkVariable actualiza automáticamente en todos los clientes
            }
        }
    }

    [ClientRpc]
    void GameOverClientRpc()
    {
        UIManager.Instance?.ShowGameOver();
    }
}

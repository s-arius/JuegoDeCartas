using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<string> playerName = new NetworkVariable<string>("Player");

    private NetworkGameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<NetworkGameManager>();

        if (IsOwner)
        {
            // set local name if you want
            // playerName.Value = "P" + NetworkManager.Singleton.LocalClientId;
        }

        score.OnValueChanged += (oldv, newv) => {
            UIManager.Instance?.UpdateScoreDisplay(this);
        };
    }

    // Called by client to request flip of a card with given NetworkObjectId
    [ServerRpc(RequireOwnership = false)]
    public void RequestFlipServerRpc(ulong cardNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        // find card by id
        var no = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardNetworkObjectId];
        if (no != null)
        {
            var card = no.GetComponent<NetworkCard>();
            if (card != null && !card.isFaceUp.Value && !card.isMatched.Value)
            {
                gameManager.RequestFlip(card, rpcParams.Receive.SenderClientId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(int amount)
    {
        // only server writes the NetworkVariable, but this ServerRpc runs on server
        score.Value += amount;
        // update UI on clients via UIManager listening to NetworkVariable change
    }
}

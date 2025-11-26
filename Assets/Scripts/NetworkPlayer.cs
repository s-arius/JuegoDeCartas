using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;

    [Header("Puntuación")]
    public NetworkVariable<int> score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Movimiento top-down
        Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        dir.Normalize();
        transform.position += dir * velocidad * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return;

        NetworkCard card = other.GetComponent<NetworkCard>();
        if (card != null && !card.isFaceUp.Value && !card.isMatched.Value)
        {
            RequestFlipServerRpc(card.NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestFlipServerRpc(ulong cardNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        var no = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardNetworkObjectId];
        if (no != null)
        {
            NetworkCard card = no.GetComponent<NetworkCard>();
            if (card != null && !card.isFaceUp.Value && !card.isMatched.Value)
            {
                NetworkGameManager gm = FindObjectOfType<NetworkGameManager>();
                if (gm != null)
                    gm.CardFlipped(card, rpcParams.Receive.SenderClientId);
            }
        }
    }
}

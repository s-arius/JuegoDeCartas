using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NetworkPlayer : NetworkBehaviour
{
    public float velocidad = 5f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        dir.Normalize();
        transform.position += dir * velocidad * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return;

        NetworkCard card = other.GetComponent<NetworkCard>();
        if (card != null)
        {
            RequestFlipServerRpc(card.NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestFlipServerRpc(ulong cardNetworkId, ServerRpcParams rpcParams = default)
    {
        var obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardNetworkId];
        if (obj != null)
        {
            NetworkCard card = obj.GetComponent<NetworkCard>();
            if (card != null)
            {
                NetworkGameManager gm = FindObjectOfType<NetworkGameManager>();
                gm.CardFlipped(card, rpcParams.Receive.SenderClientId);
            }
        }
    }
}

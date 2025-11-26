using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
public class NetworkCard : NetworkBehaviour
{
    public NetworkVariable<int> cardId = new NetworkVariable<int>(-1);
    public NetworkVariable<bool> isFaceUp = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isMatched = new NetworkVariable<bool>(false);

    public Sprite frontSprite; // assign in prefab or at spawn
    public Sprite backSprite;  // assigned in prefab

    private SpriteRenderer sr;
    private NetworkGameManager gameManager;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gameManager = FindObjectOfType<NetworkGameManager>();
        UpdateVisual();
        isFaceUp.OnValueChanged += (oldv, newv) => UpdateVisual();
        isMatched.OnValueChanged += (oldv, newv) => { if (newv) DisableInteraction(); };
    }

    void UpdateVisual()
    {
        if (isFaceUp.Value)
        {
            // for demo: front sprite could depend on cardId, in full project you'd map id->sprite
            sr.sprite = frontSprite != null ? frontSprite : backSprite;
            // if you want different sprites per id, implement a sprite manager and request sprite by id (clientside)
        }
        else
        {
            sr.sprite = backSprite;
        }
    }

    // Called by client when they click the card
    void OnMouseDown()
    {
        if (!IsOwner && !IsServer)
        {
            // Request flip through Player's authority: we need to call a ServerRpc that indicates client wants to flip this card
            var localPlayer = FindObjectOfType<NetworkPlayer>();
            if (localPlayer != null && !isMatched.Value && !isFaceUp.Value)
            {
                localPlayer.RequestFlipServerRpc(this.NetworkObjectId);
            }
        }
        else if (IsServer)
        {
            // local server flip (host)
            if (!isMatched.Value && !isFaceUp.Value)
            {
                gameManager?.RequestFlip(this, NetworkManager.LocalClientId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCardIdServerRpc(int id)
    {
        cardId.Value = id;
        // Optionally tell clients which front sprite to use based on id (not included here)
    }

    [ClientRpc]
    public void RevealClientRpc()
    {
        isFaceUp.Value = true;
    }

    [ClientRpc]
    public void HideClientRpc()
    {
        isFaceUp.Value = false;
    }

    [ClientRpc]
    public void SetMatchedClientRpc()
    {
        isMatched.Value = true;
        isFaceUp.Value = true;
        DisableInteraction();
    }

    void DisableInteraction()
    {
        // disable collider so it can't be clicked
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }
}

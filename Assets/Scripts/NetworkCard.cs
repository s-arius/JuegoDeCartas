using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(SpriteRenderer))]
public class NetworkCard : NetworkBehaviour
{
    public NetworkVariable<int> cardId = new NetworkVariable<int>(-1);
    public NetworkVariable<bool> isFaceUp = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isMatched = new NetworkVariable<bool>(false);

    public Sprite frontSprite;
    public Sprite backSprite;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        UpdateVisual();
        isFaceUp.OnValueChanged += (oldv, newv) => UpdateVisual();
        isMatched.OnValueChanged += (oldv, newv) => { if (newv) DisableInteraction(); };
    }

    void UpdateVisual()
    {
        sr.sprite = isFaceUp.Value ? frontSprite : backSprite;
    }

    void DisableInteraction()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCardIdServerRpc(int id)
    {
        cardId.Value = id;
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
}

using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class NetworkCard : NetworkBehaviour
{
    public NetworkVariable<int> cardId = new NetworkVariable<int>(-1);
    public NetworkVariable<bool> isFaceUp = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isMatched = new NetworkVariable<bool>(false);

    public Sprite backSprite;
    public Sprite[] cardFaces;

    private SpriteRenderer sr;
    private Collider2D col;

    private Vector3 originalScale;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        originalScale = transform.localScale;
    }

    public override void OnNetworkSpawn()
    {
        UpdateVisual();

        isFaceUp.OnValueChanged += (oldValue, newValue) => UpdateVisual();
        cardId.OnValueChanged += (oldId, newId) => UpdateVisual();
        isMatched.OnValueChanged += (oldValue, newValue) =>
        {
            if (newValue) DisableInteraction();
        };
    }

    void UpdateVisual()
    {
        if (isFaceUp.Value && cardId.Value >= 0)
            sr.sprite = cardFaces[cardId.Value];
        else
            sr.sprite = backSprite;
    }

    void DisableInteraction()
    {
        if (col != null) col.enabled = false;
    }

    private IEnumerator FlipRoutine()
    {
        float halfTime = 0.12f;
        float t = 0f;

        while (t < halfTime)
        {
            t += Time.deltaTime;

            float s = Mathf.Lerp(1f, 0f, t / halfTime);
            transform.localScale = new Vector3(
                originalScale.x * s,
                originalScale.y,
                originalScale.z
            );

            yield return null;
        }

        UpdateVisual();

        t = 0f;
        while (t < halfTime)
        {
            t += Time.deltaTime;

            float s = Mathf.Lerp(0f, 1f, t / halfTime);
            transform.localScale = new Vector3(
                originalScale.x * s,
                originalScale.y,
                originalScale.z
            );

            yield return null;
        }

        transform.localScale = originalScale;
    }

    [ClientRpc]
    void PlayFlipClientRpc()
    {
        StartCoroutine(FlipRoutine());
    }


    [ServerRpc(RequireOwnership = false)]
    public void RevealServerRpc()
    {
        isFaceUp.Value = true;
        PlayFlipClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void HideServerRpc()
    {
        isFaceUp.Value = false;
        PlayFlipClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCardIdServerRpc(int id)
    {
        cardId.Value = id;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMatchedServerRpc()
    {
        isMatched.Value = true;
        isFaceUp.Value = true;

        PlayFlipClientRpc();
    }
}

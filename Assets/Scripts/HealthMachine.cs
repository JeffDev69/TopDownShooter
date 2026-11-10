using TMPro;
using UnityEngine;

// Herbruikbaar genezingsstation: betaal munten om alle verloren hartjes terug te krijgen.
public class HealthMachine : MonoBehaviour
{
    public int healPrice = 200;
    public float interactionDistance = 2f;
    public SpriteRenderer machineRenderer;

    private PlayerHealth playerHealth;
    private TextMeshPro prompt;
    private float messageUntil;

    private void Start()
    {
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (machineRenderer == null) machineRenderer = GetComponent<SpriteRenderer>();

        GameObject label = new GameObject("Health Machine Prompt");
        label.transform.SetParent(transform, false);
        prompt = label.AddComponent<TextMeshPro>();
        prompt.fontSize = 3f;
        prompt.color = Color.white;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
        prompt.GetComponent<MeshRenderer>().sortingOrder = 100;
        prompt.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerHealth == null || playerHealth.currentHealth <= 0 ||
            !playerHealth.gameObject.activeInHierarchy)
        {
            if (prompt != null) prompt.gameObject.SetActive(false);
            return;
        }

        Vector3 nearest = machineRenderer != null
            ? machineRenderer.bounds.ClosestPoint(playerHealth.transform.position) : transform.position;
        bool nearby = Vector2.Distance(playerHealth.transform.position, nearest)
            <= Mathf.Max(0.1f, interactionDistance);
        prompt.gameObject.SetActive(nearby);
        if (!nearby) return;
        PlacePrompt();

        bool fullHealth = playerHealth.currentHealth >= playerHealth.maxHealth;
        if (Time.time >= messageUntil)
            prompt.text = fullHealth
                ? "<color=#73FF91>HEALTH FULL</color>"
                : GameInput.InteractHint + " HEALTH STATION - " + healPrice + " COINS\nFULL HEAL";

        if (fullHealth || Time.time < messageUntil || !GameInput.TryInteract()) return;

        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendCoins(healPrice))
        {
            prompt.text = "<color=#FF7373>NEED " + healPrice + " COINS</color>";
            messageUntil = Time.time + 2f;
            return;
        }

        playerHealth.HealToFull();
        prompt.text = "<color=#73FF91>FULLY HEALED!</color>";
        messageUntil = Time.time + 2f;
    }

    private void PlacePrompt()
    {
        Vector3 scale = transform.lossyScale;
        prompt.transform.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f);
        Vector3 position = transform.position + Vector3.down * 1.2f;
        if (machineRenderer != null)
            position = new Vector3(machineRenderer.bounds.center.x, machineRenderer.bounds.min.y - 0.8f, 0f);
        prompt.transform.SetPositionAndRotation(position, Quaternion.identity);
    }

    private void OnDisable()
    {
        if (prompt != null) prompt.gameObject.SetActive(false);
    }
}

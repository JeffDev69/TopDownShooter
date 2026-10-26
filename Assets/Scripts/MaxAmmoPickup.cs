using TMPro;
using UnityEngine;

// Maakt collectable Max Ammo, Insta-Kill en Nuke drops zonder aparte prefab setup.
public class MaxAmmoPickup : MonoBehaviour
{
    public enum PowerUpType { MaxAmmo, InstaKill, Nuke }
    public PowerUpType powerUpType;
    public float pickupRadius = 1f;
    public float lifetime = 20f;
    private PlayerShoot player;
    private PlayerHealth playerHealth;
    private float expiresAt;
    private TextMeshPro label;

    public static void Spawn(Vector3 position, PowerUpType type = PowerUpType.MaxAmmo)
    {
        GameObject drop = new GameObject(type + " Pickup");
        drop.transform.position = new Vector3(position.x, position.y, 0f);
        drop.AddComponent<MaxAmmoPickup>().powerUpType = type;
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerShoot>();
        if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
        expiresAt = Time.time + lifetime;
        label = gameObject.AddComponent<TextMeshPro>();
        label.text = "<b>MAX\nAMMO</b>";
        label.fontSize = 4f;
        label.color = new Color(0.35f, 1f, 0.45f);
        if (powerUpType == PowerUpType.InstaKill)
        {
            label.text = "<b>INSTA\nKILL</b>";
            label.color = new Color(1f, 0.35f, 0.35f);
        }
        else if (powerUpType == PowerUpType.Nuke)
        {
            label.text = "<b>NUKE</b>";
            label.color = new Color(1f, 0.85f, 0.15f);
        }
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(2f, 1f);
        label.GetComponent<MeshRenderer>().sortingOrder = 60;
    }

    private void Update()
    {
        if (Time.time >= expiresAt)
        {
            Destroy(gameObject);
            return;
        }

        if (label != null)
            label.alpha = 0.75f + 0.25f * Mathf.Sin(Time.time * 5f);

        if (player == null || !player.isActiveAndEnabled ||
            (playerHealth != null && playerHealth.currentHealth <= 0)) return;

        if (Vector2.Distance(transform.position, player.transform.position) <= pickupRadius)
        {
            GameAudio.Play(GameAudio.Cue.PowerUp);
            switch (powerUpType)
            {
                case PowerUpType.MaxAmmo: player.RefillAllAmmo(); break;
                case PowerUpType.InstaKill: player.ActivateInstaKill(); break;
                case PowerUpType.Nuke: player.DetonateNuke(); break;
            }
            Destroy(gameObject);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Regelt twee weapon slots, shooting, ammo, reloads, melee en de weapon HUD text.
public class PlayerShoot : MonoBehaviour
{
    public enum WeaponType { Empty = -1, Pistol = 0, Shotgun, SMG, Sniper, AssaultRifle, DoubleBarrel, Minigun, BurstRifle, RocketLauncher, RayGun, Flamethrower }
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.3f;

    [Header("Weapon UI")]
    public TextMeshProUGUI weaponText;

    [Header("Ammo")]
    public float reloadDuration = 1.5f;
    private int damageMultiplier = 1;
    private readonly int[] magazineAmmo = new int[2];
    private readonly int[] reserveAmmo = new int[2];
    private int reserveMagazineMultiplier = 5;
    private bool reloading;
    private float reloadEndsAt;
    private float maxAmmoMessageUntil;
    private bool showingMaxAmmo;
    private string powerUpMessage = "MAX AMMO!";

    [Header("Insta-Kill")]
    public float instaKillDuration = 30f;
    private static float instaKillUntil;
    private int displayedInstaKillSeconds;
    public static bool InstaKillActive => Time.time < instaKillUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPowerUps()
    {
        instaKillUntil = 0f;
    }

    [Header("Gun Bash (V or Right Click)")]
    public int meleeDamage = 2;
    public float meleeRange = 1.5f;
    public float meleeRadius = 0.65f;
    public float meleeCooldown = 0.6f;
    private float nextMeleeTime;
    private float meleeMessageUntil;
    private bool showingMelee;

    [Header("Shotgun")]
    [HideInInspector] public bool shotgunUnlocked;
    public int shotgunPellets = 5;
    public float shotgunSpread = 24f;
    public float shotgunFireRate = 0.8f;

    [Header("SMG")]
    public float smgFireRate = 0.1f;
    public float smgSpread = 5f;
    public int smgDamage = 1;

    [Header("Sniper")]
    public float sniperFireRate = 1.2f;
    public int sniperDamage = 6;
    public float sniperSpeedMultiplier = 3f;

    [Header("Assault Rifle")]
    public float assaultRifleFireRate = 0.18f;
    public int assaultRifleDamage = 2;
    public float assaultRifleSpread = 3f;

    [Header("Double Barrel")]
    public int doubleBarrelPellets = 8;
    public float doubleBarrelSpread = 36f;
    public float doubleBarrelFireRate = 1.2f;

    [Header("Minigun")]
    public float minigunFireRate = 0.06f;
    public float minigunSpread = 10f;
    public int minigunDamage = 1;

    [Header("Burst Rifle")]
    public int burstRifleDamage = 2;
    public int burstShotCount = 3;
    public float burstShotDelay = 0.09f;
    public float burstRifleCooldown = 0.55f;

    [Header("Rocket Launcher")]
    public int rocketDamage = 8;
    public float rocketExplosionRadius = 2.2f;
    public float rocketFireRate = 1.4f;
    public float rocketSpeedMultiplier = 0.8f;

    [Header("Ray Gun")]
    public int rayGunDamage = 4;
    public float rayGunFireRate = 0.22f;
    public float rayGunSpeedMultiplier = 2f;

    [Header("Flamethrower")]
    public int flamethrowerDamage = 1;
    public float flamethrowerRange = 5f;
    public float flamethrowerConeAngle = 42f;
    public float flamethrowerFireRate = 0.12f;

    private float nextShotTime;
    private bool bursting;
    private GunMuzzleFlash muzzleFlash;
    private FlamethrowerVisual flamethrowerVisual;
    private readonly WeaponType[] weaponSlots = { WeaponType.Pistol, WeaponType.Empty };
    private int activeSlot;
    public WeaponType CurrentWeapon => weaponSlots[activeSlot];
    public string LoadoutText => "1: " + GetWeaponName(weaponSlots[0])
        + " | 2: " + GetWeaponName(weaponSlots[1]);

    public static string GetWeaponName(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.AssaultRifle: return "ASSAULT RIFLE";
            case WeaponType.DoubleBarrel: return "DOUBLE BARREL";
            case WeaponType.BurstRifle: return "BURST RIFLE";
            case WeaponType.RocketLauncher: return "ROCKET LAUNCHER";
            case WeaponType.RayGun: return "RAY GUN";
            default: return weapon.ToString().ToUpperInvariant();
        }
    }

    void Start()
    {
        muzzleFlash = GetComponent<GunMuzzleFlash>();
        if (muzzleFlash == null) muzzleFlash = gameObject.AddComponent<GunMuzzleFlash>();
        flamethrowerVisual = GetComponent<FlamethrowerVisual>();
        if (flamethrowerVisual == null) flamethrowerVisual = gameObject.AddComponent<FlamethrowerVisual>();
        FillSlotAmmo(0);
        UpdateWeaponText();
    }

    void Update()
    {
        int instaKillSeconds = Mathf.Max(0, Mathf.CeilToInt(instaKillUntil - Time.time));
        if (instaKillSeconds != displayedInstaKillSeconds)
        {
            displayedInstaKillSeconds = instaKillSeconds;
            UpdateWeaponText();
        }
        int previousSlot = activeSlot;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                activeSlot = 0;

            if (Keyboard.current.digit2Key.wasPressedThisFrame && weaponSlots[1] != WeaponType.Empty)
                activeSlot = 1;
        }

        if (previousSlot != activeSlot)
        {
            reloading = false;
            UpdateWeaponText();
        }

        if (reloading && Time.time >= reloadEndsAt)
        {
            int needed = MagazineSize(CurrentWeapon) - magazineAmmo[activeSlot];
            int transferred = Mathf.Min(needed, reserveAmmo[activeSlot]);
            magazineAmmo[activeSlot] += transferred;
            reserveAmmo[activeSlot] -= transferred;
            reloading = false;
            UpdateWeaponText();
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            BeginReload();

        if ((Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
            GunBash();

        if (showingMelee && Time.time >= meleeMessageUntil)
        {
            showingMelee = false;
            UpdateWeaponText();
        }

        if (showingMaxAmmo && Time.time >= maxAmmoMessageUntil)
        {
            showingMaxAmmo = false;
            UpdateWeaponText();
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time >= nextShotTime)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (reloading || magazineAmmo[activeSlot] <= 0 || CurrentWeapon == WeaponType.Empty)
            return;
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet Prefab is not assigned!");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogError("FirePoint is not assigned!");
            return;
        }

        if (CurrentWeapon == WeaponType.BurstRifle)
        {
            if (!bursting) StartCoroutine(FireBurst(activeSlot));
            return;
        }

        if (CurrentWeapon == WeaponType.Flamethrower)
        {
            FireFlamethrower();
            return;
        }

        // A shotgun volley consumes one shell, not one round per pellet.
        magazineAmmo[activeSlot]--;
        UpdateWeaponText();
        if (muzzleFlash != null) muzzleFlash.Show(firePoint, CurrentWeapon);
        bool usingShotgun = CurrentWeapon == WeaponType.Shotgun;
        bool usingDoubleBarrel = CurrentWeapon == WeaponType.DoubleBarrel;
        int bulletCount = usingShotgun ? Mathf.Max(1, shotgunPellets)
            : usingDoubleBarrel ? Mathf.Max(1, doubleBarrelPellets) : 1;
        float spread = usingDoubleBarrel ? doubleBarrelSpread : shotgunSpread;
        var ignoredColliders = new List<Collider2D>(GetComponentsInChildren<Collider2D>());

        // Ignore older bullets as well as pellets from this shot.
        foreach (Bullet existingBullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
            ignoredColliders.AddRange(existingBullet.GetComponentsInChildren<Collider2D>());

        for (int i = 0; i < bulletCount; i++)
        {
            float angleOffset = 0f;

            if (CurrentWeapon == WeaponType.SMG)
                angleOffset = Random.Range(-Mathf.Abs(smgSpread) * 0.5f, Mathf.Abs(smgSpread) * 0.5f);
            else if (CurrentWeapon == WeaponType.AssaultRifle)
                angleOffset = Random.Range(-Mathf.Abs(assaultRifleSpread) * 0.5f, Mathf.Abs(assaultRifleSpread) * 0.5f);
            else if (CurrentWeapon == WeaponType.Minigun)
                angleOffset = Random.Range(-Mathf.Abs(minigunSpread) * 0.5f, Mathf.Abs(minigunSpread) * 0.5f);

            if (bulletCount > 1)
            {
                float step = spread / (bulletCount - 1);
                angleOffset = -spread * 0.5f + step * i;
            }

            GameObject projectile = Instantiate(
                bulletPrefab,
                firePoint.position,
                firePoint.rotation * Quaternion.Euler(0f, 0f, angleOffset)
            );

            // Configure this instance, never the shared bullet prefab.
            Bullet bullet = projectile.GetComponent<Bullet>();
            if (bullet != null)
            {
                if (CurrentWeapon == WeaponType.SMG)
                    bullet.damage = Mathf.Max(1, smgDamage);
                else if (CurrentWeapon == WeaponType.AssaultRifle)
                    bullet.damage = Mathf.Max(1, assaultRifleDamage);
                else if (CurrentWeapon == WeaponType.Minigun)
                    bullet.damage = Mathf.Max(1, minigunDamage);
                else if (CurrentWeapon == WeaponType.RocketLauncher)
                {
                    bullet.damage = Mathf.Max(1, rocketDamage);
                    bullet.speed *= Mathf.Max(0.1f, rocketSpeedMultiplier);
                    bullet.explosionRadius = Mathf.Max(0.2f, rocketExplosionRadius);
                }
                else if (CurrentWeapon == WeaponType.RayGun)
                {
                    bullet.damage = Mathf.Max(1, rayGunDamage);
                    bullet.speed *= Mathf.Max(0.1f, rayGunSpeedMultiplier);
                }
                else if (CurrentWeapon == WeaponType.Sniper)
                {
                    bullet.damage = Mathf.Max(1, sniperDamage);
                    bullet.speed *= Mathf.Max(0.1f, sniperSpeedMultiplier);
                }
                bullet.damage = Mathf.Max(1, bullet.damage * damageMultiplier);
                if (CurrentWeapon == WeaponType.RocketLauncher)
                    bullet.explosionDamage = bullet.damage;
            }


            if (CurrentWeapon == WeaponType.RocketLauncher)
                ProjectileVisuals.Apply(projectile, new Color(1f, 0.35f, 0.04f, 1f), 1.8f, 0.35f);
            else if (CurrentWeapon == WeaponType.RayGun)
                ProjectileVisuals.Apply(projectile, new Color(0.25f, 1f, 0.35f, 1f), 1.35f, 0.22f);

            Collider2D[] projectileColliders = projectile.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D projectileCollider in projectileColliders)
            {
                foreach (Collider2D ignoredCollider in ignoredColliders)
                {
                    if (ignoredCollider != null && ignoredCollider != projectileCollider)
                        Physics2D.IgnoreCollision(projectileCollider, ignoredCollider);
                }
            }
            ignoredColliders.AddRange(projectileColliders);
        }

        float delay = fireRate;
        switch (CurrentWeapon)
        {
            case WeaponType.Shotgun: delay = shotgunFireRate; break;
            case WeaponType.SMG: delay = smgFireRate; break;
            case WeaponType.Sniper: delay = sniperFireRate; break;
            case WeaponType.AssaultRifle: delay = assaultRifleFireRate; break;
            case WeaponType.DoubleBarrel: delay = doubleBarrelFireRate; break;
            case WeaponType.Minigun: delay = minigunFireRate; break;
            case WeaponType.RocketLauncher: delay = rocketFireRate; break;
            case WeaponType.RayGun: delay = rayGunFireRate; break;
        }
        nextShotTime = Time.time + Mathf.Max(0.05f, delay);
    }

    private IEnumerator FireBurst(int slot)
    {
        bursting = true;
        nextShotTime = Time.time + Mathf.Max(0.1f, burstRifleCooldown);
        int shots = Mathf.Max(1, burstShotCount);

        for (int i = 0; i < shots; i++)
        {
            if (activeSlot != slot || weaponSlots[slot] != WeaponType.BurstRifle ||
                reloading || magazineAmmo[slot] <= 0) break;

            magazineAmmo[slot]--;
            UpdateWeaponText();
            if (muzzleFlash != null) muzzleFlash.Show(firePoint, WeaponType.BurstRifle);

            GameObject projectile = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Bullet bullet = projectile.GetComponent<Bullet>();
            if (bullet != null)
                bullet.damage = Mathf.Max(1, burstRifleDamage * damageMultiplier);
            IgnoreProjectileCollisions(projectile);

            if (i < shots - 1)
                yield return new WaitForSeconds(Mathf.Max(0.02f, burstShotDelay));
        }

        bursting = false;
    }

    private void FireFlamethrower()
    {
        magazineAmmo[activeSlot]--;
        nextShotTime = Time.time + Mathf.Max(0.05f, flamethrowerFireRate);
        UpdateWeaponText();

        Vector2 origin = firePoint.position;
        Vector2 direction = ((Vector2)firePoint.right).normalized;
        float range = Mathf.Max(0.5f, flamethrowerRange);
        float halfAngle = Mathf.Clamp(flamethrowerConeAngle * 0.5f, 5f, 80f);
        float minimumDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        var damagedEnemies = new HashSet<EnemyHealth>();

        foreach (Collider2D target in Physics2D.OverlapCircleAll(origin + direction * range * 0.5f, range * 0.7f))
        {
            EnemyHealth enemy = target.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !damagedEnemies.Add(enemy)) continue;

            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
            float distance = toEnemy.magnitude;
            if (distance > range || distance < 0.01f || Vector2.Dot(direction, toEnemy / distance) < minimumDot)
                continue;
            if (MeleeBlocked(origin, target.ClosestPoint(origin))) continue;

            enemy.TakeDamage(Mathf.Max(1, flamethrowerDamage * damageMultiplier));
        }

        if (flamethrowerVisual != null)
            flamethrowerVisual.Show(firePoint, range, flamethrowerConeAngle);
    }

    private void IgnoreProjectileCollisions(GameObject projectile)
    {
        Collider2D[] projectileColliders = projectile.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D projectileCollider in projectileColliders)
        {
            foreach (Collider2D playerCollider in GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(projectileCollider, playerCollider);

            foreach (Bullet existingBullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
            {
                if (existingBullet.gameObject == projectile) continue;
                foreach (Collider2D otherBulletCollider in existingBullet.GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(projectileCollider, otherBulletCollider);
            }
        }
    }

    public void UnlockShotgun()
    {
        GiveWeapon(WeaponType.Shotgun);
    }

    // Public so a UI button can also call this attack later.
    public void GunBash()
    {
        if (!isActiveAndEnabled || firePoint == null || Time.time < nextMeleeTime) return;
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.currentHealth <= 0) return;

        nextMeleeTime = Time.time + Mathf.Max(0.1f, meleeCooldown);
        nextShotTime = Mathf.Max(nextShotTime, Time.time + 0.25f);
        reloading = false;
        showingMelee = true;
        meleeMessageUntil = Time.time + 0.25f;
        UpdateWeaponText();

        Vector2 origin = transform.position;
        Vector2 direction = ((Vector2)firePoint.right).normalized;
        float range = Mathf.Max(0.2f, meleeRange);
        float radius = Mathf.Clamp(meleeRadius, 0.1f, range * 0.5f);
        Vector2 center = origin + direction * (range - radius);
        var hitEnemies = new HashSet<EnemyHealth>();
        foreach (Collider2D target in Physics2D.OverlapCircleAll(center, radius))
        {
            EnemyHealth enemy = target.GetComponentInParent<EnemyHealth>();
            if (enemy == null || hitEnemies.Contains(enemy)) continue;
            Vector2 hitPoint = target.ClosestPoint(origin);
            if (Vector2.Dot(hitPoint - origin, direction) < 0f) continue;
            if (MeleeBlocked(origin, hitPoint)) continue;
            hitEnemies.Add(enemy);
            enemy.TakeDamage(Mathf.Max(1, meleeDamage));
        }
    }

    private bool MeleeBlocked(Vector2 origin, Vector2 target)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(origin, target))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || collider.transform.IsChildOf(transform)) continue;
            if (collider.GetComponentInParent<EnemyHealth>() != null ||
                collider.GetComponentInParent<Bullet>() != null) continue;
            return true;
        }
        return false;
    }

    public WeaponType GiveRandomMysteryWeapon()
    {
        WeaponType reward = (WeaponType)Random.Range(1, (int)WeaponType.Flamethrower + 1);
        GiveWeapon(reward);
        return reward;
    }

    private void GiveWeapon(WeaponType weapon)
    {
        // First reward fills slot two. Later rewards replace only the held gun.
        if (weaponSlots[1] == WeaponType.Empty)
            activeSlot = 1;

        weaponSlots[activeSlot] = weapon;
        reloading = false;
        FillSlotAmmo(activeSlot);
        UpdateWeaponText();
    }

    private static int MagazineSize(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Pistol: return 12;
            case WeaponType.Shotgun: return 6;
            case WeaponType.SMG: return 30;
            case WeaponType.Sniper: return 5;
            case WeaponType.AssaultRifle: return 30;
            case WeaponType.DoubleBarrel: return 2;
            case WeaponType.Minigun: return 100;
            case WeaponType.BurstRifle: return 24;
            case WeaponType.RocketLauncher: return 4;
            case WeaponType.RayGun: return 20;
            case WeaponType.Flamethrower: return 60;
            default: return 0;
        }
    }

    private void FillSlotAmmo(int slot)
    {
        int capacity = MagazineSize(weaponSlots[slot]);
        magazineAmmo[slot] = capacity;
        reserveAmmo[slot] = capacity * reserveMagazineMultiplier;
    }

    private void BeginReload()
    {
        if (reloading || bursting || reserveAmmo[activeSlot] <= 0 ||
            magazineAmmo[activeSlot] >= MagazineSize(CurrentWeapon)) return;
        reloading = true;
        reloadEndsAt = Time.time + Mathf.Max(0.1f, reloadDuration);
        UpdateWeaponText();
    }

    public void RefillAllAmmo()
    {
        reloading = false;
        for (int slot = 0; slot < weaponSlots.Length; slot++) FillSlotAmmo(slot);
        powerUpMessage = "MAX AMMO!";
        showingMaxAmmo = true;
        maxAmmoMessageUntil = Time.time + 2f;
        UpdateWeaponText();
    }

    public void ApplyFastReloadPerk()
    {
        reloadDuration = Mathf.Max(0.25f, reloadDuration * 0.5f);
    }

    public void ApplyDoubleDamagePerk()
    {
        damageMultiplier = 2;
    }

    public void ApplyRapidFirePerk()
    {
        const float multiplier = 0.75f;
        fireRate *= multiplier;
        shotgunFireRate *= multiplier;
        smgFireRate *= multiplier;
        sniperFireRate *= multiplier;
        assaultRifleFireRate *= multiplier;
        doubleBarrelFireRate *= multiplier;
        minigunFireRate *= multiplier;
        burstShotDelay *= multiplier;
        burstRifleCooldown *= multiplier;
        rocketFireRate *= multiplier;
        rayGunFireRate *= multiplier;
        flamethrowerFireRate *= multiplier;
    }

    public void ApplyAmmoBeltPerk()
    {
        reserveMagazineMultiplier = Mathf.Max(reserveMagazineMultiplier, 8);
        RefillAllAmmo();
    }

    public void ActivateInstaKill()
    {
        // A second pickup refreshes the duration instead of stacking unlimited time.
        instaKillUntil = Time.time + Mathf.Max(1f, instaKillDuration);
        displayedInstaKillSeconds = Mathf.CeilToInt(instaKillDuration);
        UpdateWeaponText();
    }

    public void DetonateNuke()
    {
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            enemy.KillFromNuke();
        powerUpMessage = "NUKE!";
        showingMaxAmmo = true;
        maxAmmoMessageUntil = Time.time + 2f;
        UpdateWeaponText();
    }

    private void UpdateWeaponText()
    {
        if (weaponText == null)
            return;

        weaponText.richText = true;
        weaponText.color = Color.white;
        weaponText.margin = Vector4.zero;
        weaponText.alignment = TextAlignmentOptions.MidlineLeft;
        weaponText.enableAutoSizing = true;
        weaponText.fontSizeMin = 18f;
        weaponText.fontSizeMax = 36f;
        weaponText.text = FormatWeaponSlot(0) + "\n" + FormatWeaponSlot(1);
        if (showingMaxAmmo)
            weaponText.text += "\n<size=75%><color=#176B35><b>" + powerUpMessage + "</b></color></size>";
        if (InstaKillActive)
            weaponText.text += "\n<size=75%><color=#9B2323><b>INSTA-KILL: "
                + Mathf.CeilToInt(instaKillUntil - Time.time) + "s</b></color></size>";

    }

    private string FormatWeaponSlot(int slot)
    {
        bool selected = slot == activeSlot;
        string color = selected ? "#17202E" : "#535969";
        string marker = selected ? "> " : "  ";
        string label = marker + "[" + (slot + 1) + "]  " + GetWeaponName(weaponSlots[slot]);
        if (weaponSlots[slot] != WeaponType.Empty)
        {
            label += "  " + magazineAmmo[slot] + "/" + reserveAmmo[slot];
            if (selected && showingMelee) label += "  MELEE!";
            else if (selected && reloading) label += "  RELOADING";
            else if (selected && magazineAmmo[slot] == 0)
                label += reserveAmmo[slot] > 0 ? "  [R] RELOAD" : "  [V] MELEE";
        }
        return "<color=" + color + ">" + (selected ? "<b>" + label + "</b>" : label) + "</color>";
    }
}

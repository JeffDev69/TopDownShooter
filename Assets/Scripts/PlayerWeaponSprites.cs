using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerWeaponSprites
{
    private readonly Dictionary<PlayerShoot.WeaponType, Sprite> weapons =
        new Dictionary<PlayerShoot.WeaponType, Sprite>();

    public Sprite Body { get; }
    public Vector2 WeaponPosition => new Vector2(0.095f, -0.105f) * WeaponScale;
    public float WeaponScale { get; } = 1f;

    public PlayerWeaponSprites(Sprite original)
    {
        if (original == null) return;
        Body = Resources.Load<Sprite>("PlayerWeapons/Imported/SurvivorBody");
        WeaponScale = 100f / original.pixelsPerUnit;
    }

    public Sprite Get(PlayerShoot.WeaponType weapon)
    {
        if (Body == null || weapon == PlayerShoot.WeaponType.Empty) return null;
        if (!weapons.TryGetValue(weapon, out Sprite sprite))
        {
            sprite = Resources.Load<Sprite>("PlayerWeapons/Imported/" + weapon);
            weapons.Add(weapon, sprite);
        }
        return sprite;
    }
}

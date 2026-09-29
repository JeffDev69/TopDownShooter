# Weapon balance pass

The starting M1911 stays at 1 damage, a 0.30-second shot interval, 12 rounds and a
1.5-second reload. Fast zombies start with 3 HP, tanks with 8 HP, and normal round
scaling adds only 1 HP every two rounds, so the largest gun outliers and upgrade
multipliers have been reduced without increasing enemy health.

## Base weapons

These are theoretical unupgraded damage-per-second values while firing, before
reloads, misses, armor, frame timing or splash falloff. Shotgun values assume every
pellet hits; burst values include the pause between bursts.

| Weapon | Previous DPS | New DPS | Main tradeoff |
| --- | ---: | ---: | --- |
| M1911 | 3.33 | 3.33 | Starter unchanged |
| AA-12 | 27.27 | 7.27 | 4 pellets, 0.55s interval, 10-shell drum, 6-unit reach |
| Ray Gun | 18.18 | 5.00 | 3 damage, 0.60s interval, 12-round magazine |
| M134 Minigun | 16.67 | 6.67 | Wider spread, 80 rounds, 4.2s reload |
| FAMAS | 16.36 | 6.35 | 2 damage per round; fast three-shot burst with recovery |
| M4A1 | 12.50 | 6.67 | 0.30s interval and 2.1s reload |
| AK-47 | 11.11 | 5.88 | 0.34s interval and wider spread |
| MP5 | 10.00 | 5.56 | 0.18s interval and 1.8s reload |
| M60 | 9.09 | 5.26 | 2-damage heavy rounds at 0.38s intervals; 3.6s reload |
| Remington 870 | 6.25 | 4.21 | 4 pellets, wider spread, 7.5-unit reach |
| L96A1 | 5.00 | 3.87 | Keeps 6 damage for precise single shots; 1.55s interval |

Other automatic guns, handguns, shotguns and sniper variants also receive longer
shot intervals. The Uzi trades accuracy for speed, P90 keeps its larger magazine,
and Barrett hits harder than the L96A1 but cycles more slowly. The double barrel
keeps its six-pellet close-range burst. All shotgun pellets expire after 6-8 world
units rather than traveling across the arena.

Flamethrower ticks are now 0.24s apart, with a 36-degree cone, 40 fuel and a 3s
reload; the scene's 3.4-unit range is retained. Rockets deal 6 damage at the center,
have a 1.8-unit blast radius, a 1.8s interval and a 3s reload. Blasts lose damage
with distance and are blocked by solid cover. Heavy guns now have longer reloads;
Quick Reload still halves the chosen weapon's reload duration.

## Progression

- Rarity damage bonuses: Rare +10%, Epic +20%, Legendary +35% (previously +20%,
  +40%, +70%).
- Each Pack-a-Punch tier adds 35% of base damage and 20% of base magazine size.
  Tier III therefore gives 2.05x damage and 1.6x magazine size, versus the old
  4x and 2.5x. Each tier reduces firing delays by 5%, versus 15% previously.
- Double Damage still doubles damage. Rapid Fire consistently gives 25% more
  shots per second for every variant, including burst spacing and flame ticks.
  Reapplying Rapid Fire does not stack it again.
- Legendary + PAP III + Double Damage averages 5.535x base damage, down from
  13.6x. With Rapid Fire, firing DPS scales by about 8.07x before reloads, down
  from about 29.53x on weapons that previously received Rapid Fire.
- Fractional damage carries forward per weapon slot. Ten Rare 1-damage bullets
  now deal 11 total damage, rather than rounding every bullet up to 2. Pellet
  bonuses are distributed across the volley; all enemies in a flame tick receive
  the same tick damage. Buying a replacement weapon resets that slot's carry.

The serialized PlayerShoot values in `Assets/Scenes/game.unity` match the new
defaults. Historical recovery scenes are unchanged. Verify the feel in normal
play through early rounds, a special wave and a boss round; the DPS figures are
balancing guides rather than a substitute for playtesting.

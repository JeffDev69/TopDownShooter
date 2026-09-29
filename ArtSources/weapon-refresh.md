# Weapon sprite refresh

The Ray Gun, Remington 870 (Shotgun), and AK-47 (AssaultRifle) were refreshed with the built-in imagegen tool on 2026-09-29, using the existing Quaternius-based top-down renders as edit references. The original source attribution still applies to those references.

Game assets:

- `Assets/Resources/PlayerWeapons/Imported/RayGun.png`
- `Assets/Resources/PlayerWeapons/Imported/Shotgun.png`
- `Assets/Resources/PlayerWeapons/Imported/AssaultRifle.png`

The PNGs keep real alpha transparency. Import settings retain the original GUIDs, grip pivots and world lengths so existing weapon loading and muzzle placement continue to work. The import pixels-per-unit values match the new image widths. No generated files outside the project are needed at runtime.

## Prompts

### RayGun

Use case: precise-object-edit. Asset type: transparent PNG weapon sprite for an existing Unity top-down zombie shooter. Input image is the edit target, the current ray gun sprite. Redesign and polish this ray gun: a compact retro-futuristic gunmetal receiver, warm brass barrel rails, a clearly visible luminous green energy chamber, three clean green cooling rings and a compact green emitter muzzle. Keep a chunky, clean low-poly/toon-render aesthetic, limited large color blocks and sharp readable bevels that remain clear at tiny gameplay scale. Preserve the EXACT orthographic directly-overhead camera and horizontal left-to-right direction in the reference: rear at left, muzzle pointing straight right, barrel centerline horizontally centered. Absolutely no side view, no perspective, no diagonal tilt. The gun should be slimmer than a quarter of its length. Show ONE complete gun filling most of a wide canvas with a small transparent margin. Real alpha transparency, no shadow outside the gun, no glow haze outside the silhouette, no floor, no hands, no text, no labels, no logo, no watermark. Render the weapon only.

### Shotgun

Use case: precise-object-edit. Asset type: transparent PNG weapon sprite for an existing Unity top-down zombie shooter. Input image is the edit target, the current pump shotgun sprite. Improve this pump shotgun into a readable polished dark steel and warm walnut weapon. Distinct tapered walnut shoulder stock at left, a squared blue-black receiver with a silver top edge, a ribbed walnut pump fore-end and a long dark barrel with a silver muzzle collar at far right. Add a small top sight, clean restrained grooves and material separation. Keep a chunky clean low-poly/toon-render aesthetic with broad color blocks and subtle bevels that remain clear at tiny gameplay scale. Preserve the EXACT orthographic directly-overhead camera and horizontal left-to-right direction in the reference: stock at left, muzzle pointing straight right, barrel centerline horizontally centered. Absolutely no side view, no visible downward pistol grip, no perspective, no diagonal tilt. Long slim silhouette, total width about one sixth of length. ONE complete gun filling most of a wide canvas with small transparent margin. Real alpha transparency, no shadow outside gun, no floor, no hands, no text, no labels, no logo, no watermark. Render the weapon only.

Final camera correction:

Correct the camera angle of this weapon sprite to a TRUE DIRECTLY OVERHEAD TOP VIEW. The current edit is mistakenly almost a side profile. Use bilateral symmetry above and below the horizontal barrel centerline. The camera looks straight down at the TOP surface of the gun; we cannot see its left or right sides. The barrel is a single central horizontal cylinder. Top sights sit on that same horizontal centerline and must NOT project upward out of the silhouette. No side ejection port, no visible side face. For the shotgun, put the pump directly AROUND the main barrel centerline, not below a second barrel. For the assault rifle, put the gas tube ABOVE the barrel in 3D, therefore overlapping it in this overhead projection, not a parallel lower barrel in the sprite. Preserve the attractive walnut and steel colors, clean chunky low-poly shading, complete silhouette, left-stock/right-muzzle orientation, transparent background and no other objects or text.

### AssaultRifle

Use case: precise-object-edit. Asset type: transparent PNG weapon sprite for an existing Unity top-down zombie shooter. Input image is the edit target, the current assault rifle sprite. Improve this AK-style assault rifle with a recognizable warm walnut stock at left, blue-black stamped steel receiver, a small silver bolt detail and rear sight on top, a walnut fore-end with two dark ventilation cuts, exposed gas tube and stepped barrel ending in a dark muzzle brake at far right. Clean strong silhouette and readable material separation. Keep a chunky clean low-poly/toon-render aesthetic with limited broad color blocks and subtle bevels that remain clear at tiny gameplay scale. Preserve the EXACT orthographic directly-overhead camera and horizontal left-to-right direction in the reference: stock at left, muzzle pointing straight right, barrel centerline horizontally centered. Absolutely no side view, no downward-visible magazine, no perspective, no diagonal tilt. Slim silhouette, widest part about one sixth total length. ONE complete gun filling most of a wide canvas with small transparent margin. Real alpha transparency, no shadow outside gun, no floor, no hands, no text, no labels, no logo, no watermark. Render weapon only.

Final camera correction:

Correct the camera angle of this weapon sprite to a TRUE DIRECTLY OVERHEAD TOP VIEW. The current edit is mistakenly almost a side profile. Use bilateral symmetry above and below the horizontal barrel centerline. The camera looks straight down at the TOP surface of the gun; we cannot see its left or right sides. The barrel is a single central horizontal cylinder. Top sights sit on that same horizontal centerline and must NOT project upward out of the silhouette. No side ejection port, no visible side face. For the shotgun, put the pump directly AROUND the main barrel centerline, not below a second barrel. For the assault rifle, put the gas tube ABOVE the barrel in 3D, therefore overlapping it in this overhead projection, not a parallel lower barrel in the sprite. Preserve the attractive walnut and steel colors, clean chunky low-poly shading, complete silhouette, left-stock/right-muzzle orientation, transparent background and no other objects or text.


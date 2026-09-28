# Speler- en wapenassets

De game gebruikt de PNG's in `Assets/Resources/PlayerWeapons/Imported`.
De bestaande wapenwaarden, slots en mystery-box-beloningen zijn behouden.

De speler komt uit Kenney's originele vectorbestand. In
`survivor_body.svg` is alleen de ingebouwde wapenlaag verwijderd. De PNG
is op achtvoudige resolutie gerenderd, met 800 pixels per Unity-unit, zodat
de speler dezelfde afmetingen en schiethouding houdt.

De wapens zijn bovenaanzichten van bestaande 3D-assets. Het zijn gestileerde
vervangers, geen exacte modellen van alle merknamen op de HUD.
`weapon-source-map.csv` koppelt elk wapentype aan zijn bronmodel, lengte,
grippunt en renderrotatie. De twee raketwerpers gebruiken hetzelfde model
met een eigen kleur en afmeting.

Alle credits en licenties staan in `Assets/StreamingAssets/AssetCredits.txt`.
Unity neemt dat bestand mee in de build. De minigun is CC BY 3.0 en vereist
naamsvermelding; de overige nieuwe bronnen zijn CC0. De credits gelden voor
de afbeeldingbestanden, niet als licentie voor de eigen gamecode.

Oudere wapenafbeeldingen zijn niet verwijderd, maar worden niet meer door
`PlayerWeaponSprites` geladen. Er is geen beeldbewerking of 3D-rendering
tijdens het spelen: het wisselen gebruikt gewone, gecachte Sprite-assets.

## Risky Artifacts
Adds 7 artifacts to ruin your run with.

- **Artifact of Origination**
	- Bosses invade the map every 10 minutes.
	- Invaders gain +50% movement speed, +30% attack speed, and +20% damage, and -30% cooldown reduction,.
	- Invaders drop items when killed, with a chance to drop their respective boss item.
	- Boss spawn count scales based on playercount and loop count.
	- Config allows for Classic Origin, where only Imp Overlords spawn.

- **Artifact of Arrogance**
	- Mountain Shrines are permanent.
	
- **Artifact of Expansion**
	- Teleporter radius is mapwide, but takes longer to charge.
	- Doubles Moon/Void field holdout radius but increases their charge time.

- **Artifact of Conformity**
	- Prevents scrappers and printers from spawning.
	
- **Artifact of Warfare**
	- Boosts monster movement speed, attack speed, and projectile speed by 50%.
	
- **Artifact of Primacy**
	- The Primordial Teleporter spawns on every stage after looping. 
	
- **Artifact of the Phantom**
	- The Phantom King of Nothing hunts you down if you spend too long on a stage.
		- Spawns after spending 4.5min-6min on a stage.
		- 12000 HP, 200% damage
		- Immune to Void Fog and fall damage.
		- Can die to Void Reaver bubbles.
		- Drops an Irradiant Pearl and 5 Lunar Coins when killed.
		- Begins ravaging the map with explosions after death.
		
- **Artifact of Cruelty**
	- Enemies can spawn with multiple elite affixes.
	
## For Mod Devs
If you are making a custom boss and want to add it to the Artifact of Origination spawn pool, add a softdependency to com.Moffein.RiskyArtifacts then do:
`Risky_Artifacts.Artifacts.Origin.AddSpawnCard(Spawncard, BossTier);`

Boss Tier affects when the boss can spawn.

Stage 1-2: 100% chance T1

Stage 3-4: 30% chance T1, 70% chance T2

Stage 5: 25% chance T1, 65% chance T2, 10% chance T3

Stage 5+: 5% chance of Scavengers, 95% chance of Stage 5's boss pool


T1: Beetle Queen (disabled), Titan, Vagrant, Dunestrider

T2: Imp Overlord, Magma Worm, Solus Control Unit, Grovetender, Xi Construct, Void Devastator

T3: Grandparent (disabled)

## Credits

Main Dev - Moffein

Warfare Icon - MinHui12g

Icons for Conformity, Arrogance, Expansion, Origin - KoobyKarasu

Chinese Translation - JunJun_w

Ukrainian Translation - Damglador

Spanish Translation - Juhnter

Portuguese Translation - Kauzok

Cruelty - RyanPallesen's FloodWarning Artifacts

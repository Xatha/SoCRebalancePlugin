# Shrine of Chance Rebalancing Mod

This mod aims to rebalance how the Shrine of Chance works in Risk of Rain 2. 
In the early - mid game the Shrine of Chance is frankly the worst economic choice. You spend as much money as chest prices on the shrine, with the same drop chances as a normal chest... with the little caveat that you can also get *nothing at all*. I felt this was an unrewarding system, and sought to make my own spin on it. This mods rewards you and your crippling gambling addictions, by upping the chance of getting rarer items the *more times you fail in a row*. Now, failing four times in a row isn't a waste of time and money, but it can now *potentially* give you that sweet legendary you need to win your run!.. 

But.. not Captain hacking beacons allowed.. 

## Changelog

**1.1.0**
* Configs introduced:
    * MaximumPurchaseCount can now be configured.
        * `refreshTimer` can now be configured.
        * `costMultiplier` can now be configured.
        * `deactivateIfRedItemDrops` and `isBeaconHackingAllowed` can now be disabled.
        * Configs can be hot-reloaded with the console command: `SoC_reload_config`. This also works in multiplayer games. 
* Changed how the interaction effects work
    * The colour of the effect on interaction is now depending on the item rarity dropped.
* Cleaned up a lot of code.
    
**1.0.2**
Fixed missing effect and sound when interacting with the shrine. 

**1.0.1**
Fixed some leftover code used for debugging.

**1.0.0**

First release of the mod. The more you fail, the better loot you can potentially get. The shrine now automatically turns off after getting a legendary. There's also a check that turns this mechanic off if the Shrine has been hacked Captain's hacking beacons. Sorry no Captain players allowed here! 

* The base chances are the same, it only changes the more you fail;
* The more times you fail in a row, the higher the chances get for a green/red tier item. The failure chance is unchanged.
* If you fail 6 times in a row, you are "guaranteed" a legendary!
* No Captain hacking beacons allowed! If the Shrine is hacked by a beacon, it returns back to its normal base-game behaviour. 

This mod is *not* final. I am planning on making this as balanced as possible, but it'll take a while to get it just right. I am working on making it so you can change the mod's function as you like through config files. (Yes, even an option removing the no Captain cheese).

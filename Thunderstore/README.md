# Description

## A mod that encourages dietary variety through diminishing returns.

Makes it so that eating the same thing over and over will give you fewer and fewer food benefits (health, stamina, eitr)
restored each time.

`Version checks with itself. If installed on the server, it will kick clients who do not have it installed.`

`This mod uses ServerSync, if installed on the server and all clients, it will sync all configs to client`

`This mod uses a file watcher. If the configuration file is not changed with BepInEx Configuration manager, but changed in the file directly on the server, upon file save, it will sync the changes to all clients.`

![](https://i.imgur.com/TDiauJV.png)

##
`Mod made for tangofrags. Special thank you to him for testing it and the idea. Please report all bugs to me using my discord link below. This mod was made fairly quickly`

---

## Configuration Details

1. **Lock Configuration**: Prevents changes to the mod's settings unless done by server admins.
2. **Diminishing Factor**: Sets the rate at which food benefits diminish with repeated consumption.
3. **Consumption Threshold**: Number of times a food can be eaten before its benefits start diminishing.
4. **History Length**: The number of unique food consumptions tracked for diminishing returns.

## Detailed Mod Information

### For End Users (Players)

- **Key Concept**: Eating the same food over and over reduces its benefits.
- **Threshold**: After eating the same food a certain number of times (e.g., 3 times), it starts giving less health or
  stamina.
- **Reset Mechanism**: If you diversify your diet and eat different foods, the counter for any previously overeaten food
  resets, and it will give full benefits when you eat it again.
- **Example**: If you eat raspberries four times in a row, the fourth time will give you less benefit. But if you switch
  to other foods and don't eat raspberries for a while, raspberries will be effective again when you return to them.

### For Power Users/Admins

- **Consumption Threshold**: Configurable setting that determines how many times a food can be eaten before diminishing
  returns start. Lowering this number makes the game more challenging.
- **History Length**: Determines the number of unique food consumptions tracked. A longer history encourages more
  variety in diet over an extended period but might be hard for vanilla (especially) early game.
- **Diminishing Factor**: Adjusts the severity of the diminishing returns. A lower factor means a more significant
  decrease in benefits.
- **Customization**: These settings allow you to tailor the difficulty and strategy related to food consumption in the
  game, adding depth to survival mechanics.

---

## New: "I'm getting sick of it" System Messages

By default, once you reach your configured **Consumption Threshold** for a given food, the mod will display an **above
player** message such as:

- *“I’m starting to get sick of it.”*
- *“I need a change of flavors...”*
- *“I'm sick of this food!”*

These lines are selected at random and can be localized. They show up only on the **local player**’s screen, so other
players won't be spammed with messages.

# Technical and possibly boring read. Skip to the example scenario if you don't care much about technicals.

The way the food consumption and queue work in your "The Spice of Life" mod can be understood in terms of both the
consumption threshold and the history length. Let's clarify how these mechanisms interact:

### Consumption Threshold

The consumption threshold determines how many times a player can consume the same food item before its benefits start
diminishing. This is a count specific to each food item.

- **Counting Up**: Each time a player consumes a food item, a counter for that specific item is increased.
- **Reaching Threshold**: Once this counter exceeds the threshold value, the benefits of the food start diminishing. For
  example, if the threshold is set to 3, the benefits diminish on the 4th consumption of the same food item.

### History Length and Queue Mechanism

The history length determines how many unique food consumptions are remembered in the queue.

- **Queue Behavior**: As the player eats different food items, these are added to the queue. Once the queue reaches its
  maximum size (defined by the history length), the oldest food item in the queue is removed when a new item is added.
- **Resetting the Counter**: If a food item is removed from the queue (i.e., it hasn't been consumed recently and other
  foods have been eaten instead), its consumption counter is reset. This means that when it is eaten again, it will
  provide full benefits until the threshold is reached again.

### Interaction Between Threshold and Queue

- **Threshold First, Then Queue**: The diminishing returns on a specific food item are first governed by the consumption
  threshold. As long as the food item's consumption count is below this threshold, it provides full benefits.
- **Queue Determines Reset**: The history length and queue come into play for resetting the consumption count of a food
  item. If a food item is not part of the recent consumption history (falls out of the queue), its consumption count is
  reset.

### Example Scenario

- **Threshold = 3, History Length = 5**:
    - A player eats raspberries (count goes to 1), then eats them again two more times (count goes to 3). On the 4th
      time, benefits start diminishing.
    - Meanwhile, the player eats four different foods. Now, raspberries are no longer in the recent five foods eaten (
      queue length), so their count is reset.
    - The next time raspberries are eaten, they provide full benefits again until consumed three more times.

<details>
<summary><b>Installation Instructions</b></summary>

***You must have BepInEx installed correctly! I can not stress this enough.***

### Manual Installation

`Note: (Manual installation is likely how you have to do this on a server, make sure BepInEx is installed on the server correctly)`

1. **Download the latest release of BepInEx.**
2. **Extract the contents of the zip file to your game's root folder.**
3. **Download the latest release of TheSpiceOfLife from Thunderstore.io.**
4. **Extract the contents of the zip file to the `BepInEx/plugins` folder.**
5. **Launch the game.**

### Installation through r2modman or Thunderstore Mod Manager

1. **Install [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/)
   or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).**

   > For r2modman, you can also install it through the Thunderstore site.
   ![](https://i.imgur.com/s4X4rEs.png "r2modman Download")

   > For Thunderstore Mod Manager, you can also install it through the Overwolf app store
   ![](https://i.imgur.com/HQLZFp4.png "Thunderstore Mod Manager Download")
2. **Open the Mod Manager and search for "TheSpiceOfLife" under the Online
   tab. `Note: You can also search for "Azumatt" to find all my mods.`**

   `The image below shows VikingShip as an example, but it was easier to reuse the image.`

   ![](https://i.imgur.com/5CR5XKu.png)

3. **Click the Download button to install the mod.**
4. **Launch the game.**

</details>

<details>
<summary><b>Localizing the mod</b></summary>

TheSpiceOfLife supports localization. This means that you can create language files for different languages. For
example, to add a Korean translation to this mod, a user could create a TheSpiceOfLife.Korean.yml file inside the
BepInEx/config folder and add Korean translations there.

</details>

<br>
<br>

`Feel free to reach out to me on discord if you need manual download assistance.`

# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/

For Questions or Comments, find me in my discord, click the icon below.

<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>

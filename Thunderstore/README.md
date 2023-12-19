# AzuDevMod

**Important**: This mod is specifically designed to assist with issues related
to `ZNetScene.RemoveObjects`, `UnpatchAll`, `AssetBundle`, and `prefab` loading errors. It's not intended for casual
play. Install it primarily if you're facing challenges in these areas (then remove after you found your issue).

**Note**: While this mod is mainly for the client side, you can install it on a server to access logs there. However,
most issues it addresses are likely to occur on clients running the same set of mods.

**Credit where credit is due**: This mod builds upon Blaxxun's DevMod(s), initially available on the OdinPlus discord.
Her versions that probably do more than this are private so I am not sure if something better is out there.
While Blaxxun's work primarily addressed `ZNetScene/ZNetView/GameObject` destruction and `UnpatchAll` issues, I've
unified and
enhanced its functionalities as well as made it available on TS. Now, it not only offers clearer error outputs (like
specifying mod & bundle names) but
also introduces AssetBundle and prefab loading features from my side.

Please note that this doesn't mask the problems or "fix" them like ZnetScene RemoveObjects SpamKiller by sbtoonz. It's
meant to find the issues and have them reported. His mod is good if you wish to use the mods and the author hasn't or
doesn't want to fix the issues. Or, whatever the reasons may be. I'm not here to judge.

---

AzuDevMod offers specialized debugging tools related to asset loading in Valheim and corrects GameObject destruction
processes. It serves both mod developers and users:

- **For Mod Developers**:
    - Debug asset or assetbundle loading issues more efficiently.
        - Got a lot of prefabs? It will tell you which you're attempting to load that doesn't exist in the bundle.
        - Got a lot of bundles? It will tell you which you're attempting to load that doesn't exist in your mod.
        - Just have fat fingers and typed wrong? It will print the bundle and prefab information you're attempting to
          load. Helping you
          identify the issue faster.
    - Debug issues related to GameObject destruction. It will tell you what is and isn't registered in the ZNetScene or
      being destroyed properly.

- **For Users**:
    - Debug common errors from mods, especially those that arise from improper (or absent) object registrations in
      Valheim's ZNetScene, or from mods that call `UnpatchAll` during game shutdown. This helps to prevent the
      notorious `ZNetScene.RemoveObjects` error spam and losing data due to UnpatchAll being called inside of mods (
      without
      specifying a particular mod GUID). So you can report the issue to the mod developer and help them fix it.

<details>
<summary><b>Installation Instructions</b></summary>

***You must have BepInEx installed correctly! I can not stress this enough.***

### Manual Installation

`Note: (Manual installation is likely how you have to do this on a server, make sure BepInEx is installed on the server correctly)`

1. **Download the latest release of BepInEx.**
2. **Extract the contents of the zip file to your game's root folder.**
3. **Download the latest release of AzuDevMod from Thunderstore.io.**
4. **Extract the contents of the zip file to the `BepInEx/plugins` folder.**
5. **Launch the game.**

### Installation through r2modman or Thunderstore Mod Manager

1. **Install [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/)
   or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).**

   > For r2modman, you can also install it through the Thunderstore site.
   ![](https://i.imgur.com/s4X4rEs.png "r2modman Download")

   > For Thunderstore Mod Manager, you can also install it through the Overwolf app store
   ![](https://i.imgur.com/HQLZFp4.png "Thunderstore Mod Manager Download")
2. **Open the Mod Manager and search for "AzuDevMod" under the Online
   tab. `Note: You can also search for "Azumatt" to find all my mods.`**

   `The image below shows VikingShip as an example, but it was easier to reuse the image.`

   ![](https://i.imgur.com/5CR5XKu.png)

3. **Click the Download button to install the mod.**
4. **Launch the game.**

</details>

<br>


<details>
<summary><b>Configuration</b></summary>

### General User Configurations

- #### Log Destroyed ZNetViews
    - **Description:** Logs destroyed ZNetViews to the console. Useful for identifying mods that have ZNetViews
      destroyed
      without going through the ZNetScene.
        - Default Value: On

- #### Log Unregistered ZNetViews

    - **Description:** Logs unregistered ZNetViews to the console. Useful for identifying mods that have ZNetViews with
      prefabs not registered in the ZNetScene.
        - Default Value: On

- ### Log Unpatch All

    - **Description:** Logs mods that call UnpatchAll to the console. Useful for finding mods that are unpatching
      all patches at game close causing issues with other mods.
        - Default Value: On

- ### Log Asset Bundle Issues

    - **Description:** Logs asset bundle issues to the console. Useful for identifying mods that load asset bundles
      incorrectly or attempt to retrieve prefabs from a bundle that doesn't contain them.
        - Default Value: On

### Mod Developer Configurations

- #### Log Duplicate GameObject Additions

    - **Description:** Logs duplicate GameObject additions to the console. Mainly intended for mod developer debugging.
      Note
      that this might not work if your mod is obfuscated. Use this on a clean version of your mod. Useful for finding
      duplicate key issues for ZNetScene, such as attempting to add duplicate GameObjects to ZNetScene's prefab list.
        - Default Value: Off

</details>

`Feel free to reach out to me on discord if you need manual download assistance.`

# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/

For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)
<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>
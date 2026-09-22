# Project Auga
##### by RandyKnapp / n4
##### Port and Update by FNGApex and Claude, I have a SWE Job, I have a CS Degree, I don't have time to manualy port hunderds of screen but I can make AI do it.
##### I am not the Voulenteer from the Discord, if you found this lurking feel free to spread the news, I have not posted this on any channels and do not intent to untill it's PR ready, do not trust people posting this pretending to be me. My github is linked to my discord profile and is visible.
> ## WORK IN PROGRESS: Valheim 1.0 migration - broken, do not use for play
>
> This fork is an **in-progress migration of Project Auga to Valheim 1.0 ("Deep North", Unity 6)**.
> It is **still broken and does not work** as a finished mod: screens are missing, others are half-ported,
> and it can crash or freeze the game. There is no release build.
>
> **Test at your own risk, and only if you are a developer who can read a BepInEx log and file a bug report.**
> Upstream (RandyKnapp/Auga) has not been updated for 1.0; nothing here is endorsed by the original authors.
> Port notes live in `PORT_1.0.md`, the audits in `AUDIT_1.0.md` / `AUDIT2_1.0.md`, and the upstream issue review in `ISSUES_1.0.md`.

Project Auga is a completely re-imagined, modder-friendly UI-overhaul for Valheim. Every last piece of UI was considered and reworked from the ground-up to create a more helpful and immersive player experience, all while remaining familiar to Valheim veterans.

## What's Changed?

Basically everything:
  * New Player HUD
  * Redesigned Player & Container inventories
  * New Consolidated Player Panels
  * Improved Crafting Panels
  * Expanded Character Select & New Character Screens
  * Overhauled Loading Screens
  * Auga-Style EVERYTHING

SEE SCREENSHOTS HERE: https://github.com/RandyKnapp/Auga/tree/main/Auga/Screenshots

## How to Install

  1. Install [BepInEx for Valheim﻿](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
  1. Download the zip file from here on Thunderstore
  1. Install with your mod manager
  1. OR - Extract the contents of the `files` folder inside the zip file to <Your Valheim Installation Directory>\BepInEx\plugins\Auga

## Mod Compatibility

Does it work with...

  * EpicLoot: **YES**
  * Equipment & Quick Slots: **YES**
  * _Message me if your mod is compatible, I'll add it to this list! - RandyKnapp_

Project Auga drastically changes many parts of the Valheim UI. It will most likely not be compatible with other mods that modify the UI.

Please report bugs and mod conflicts on the [GitHub Issues Page](https://github.com/RandyKnapp/Auga/issues)﻿!

## For Modders

Project Auga comes with an API that allows other mods to easily access its features and create UI elements in the Auga style. It's also open-source on GitHub.

Auga API: https://github.com/RandyKnapp/Auga/wiki/Auga-API
Source: https://github.com/RandyKnapp/Auga

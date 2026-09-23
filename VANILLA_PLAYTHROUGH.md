# Auga on vanilla Valheim 1.0: what is left for a full playthrough

> Compiled 2026-09-22 from LEDGER, AUDIT_1.0 (A), AUDIT2_1.0 (A2), ISSUES_1.0 (I), PORT_1.0, the research notes and the
> ClaudeHeim scenarios. Deferred mod-compatibility work lives in STRETCH.md, not here. Two claims were spot-checked in
> code: the Achievements panel is hidden (`PlayerInventory_Setup.cs`), and world modifiers / crossplay are hidden
> (`MainMenu_Setup.cs`).

**Headline:** no known blocker. Nobody has played a vanilla game start to finish with the port yet. AUDIT2 has no real
open findings; what is left comes from AUDIT_1.0's deferred missing/cosmetic block, the ISSUES "likely still applies"
rows, main-menu fields that are hidden on purpose, and screens no scenario has visited.

Fixed 2026-09-22, not in the tables below: the HP bar stall under per-frame healing. Also the fresh-install startup
crash, found by the peer's Linux run: `Auga.Awake` touched `Localization.instance` before Steam was up, which threw
when no language pref was saved, so the whole Auga UI never loaded.

## A. Open bugs and gaps

Severity: Br = broken, C = cosmetic. Seen: V = seen in game, S = suspected from source, BC = fixed but only build-checked.

| id | description | sev | seen | source |
|---|---|---|---|---|
| auga-lib-4, widgets-4 | Item tooltip hides 10 of 1.0's 11 equipment modifiers (stamina, heat, max adrenaline, base modifier): trinket and armour stats are missing | Br | S | A:741, A:894 |
| widgets-8 | Tooltip value order inverted vs 1.0; stack weight split dropped | Br | S | A:1117 |
| #225, #227, auga-lib-10 | Tooltip repeats the set-effect block and shows raw tokens (`$item_fulladrenaline`); 1.0 subtitle line and "cheated item" marker dropped | Br | S | I:55-56, A:1053 |
| auga-lib-7 | Damage box leaves out the NonPlayer damage channel | C | S | A:883 |
| pause-texts-10 | The 1.0 Achievements panel has no way in (root/Info is a hidden donor) | Br | S, code-checked | A:680, PlayerInventory_Setup.cs:180 |
| pause-texts-8, harmony-6 | Compendium has no 1.0 stats page (player stats, worlds, kills) | Br | S | A:650, A:710 |
| main menu | World panel hides world modifiers (`m_serverOptions*`) and the crossplay / same-platform toggles; changelog, EULA/log, merch and the Cinematics entry are hidden too | Br | S, code-checked | MainMenu_Setup.cs:231, 502 |
| main-menu list prefabs | Vanilla world/server rows sit inside Auga's list; vanilla JoinPanel moved into Auga's panel | C | V | MainMenu_Setup.cs:432, 509 |
| hud-bars-2 | World-generation progress dial invisible (in the hidden LoadingBlack donor): a first world load looks hung | Br | S | A:720 |
| minimap-2 | Double-click map ping dead (middle-click works) | Br | S | A:731 |
| minimap-4 | Large-map key hints never show | C | S | A:906 |
| hud-build-3 | 1.0 build-menu key hints ("Close Build Menu", "Favorite") never show | C | S | A:660 |
| hud-build-6 | Station row shows untranslated "None" and flashes red under `nocraftcost` | C | S | A:948 |
| hud-bars-5, hud-build-9, harmony-12 | Build-category counts stay vanilla yellow (transpiler no longer matches) | C | S | A:1032 |
| widgets-6, pause-texts-14 | `TabHandler.m_tabKeyInput` defaults to true on Auga prefabs: Tab cycles compendium / new-character tabs | Br | S | A:606, A:1171 |
| pause-texts-12 | Compendium's initial tab selection is cancelled | C | S | A:1150 |
| #44 | Compendium unguarded `drop.m_prefab` dereference (possible NRE, seasonal entries) | Br | S (opened fine) | I:21 |
| #153 | Trophy page: only picked-up trophies, first per creature, skips creatures without a CharacterDrop trophy | Br | S | I:40 |
| #186, auga-lib-6 | Trophy list can't be scrolled over empty space; bestiary shows "-" for SlightlyWeak/Resistant and a bogus NonPlayer row | C | S | I:46, A:991 |
| #209 | Skills tab has no "+N" from skill-modifying effects | Br | S | I:51 |
| #59 | HUD food panels have no remaining-time text | C | S | I:23 |
| #100 | Crafting: previous item's icon stays when the next selection has no ItemData, and on the max-quality branch | Br | S | I:32, AugaCraftingPanel.cs:124, 236 |
| crafting-5 | Upgrader stations force the max-quality panel, hiding 1.0's upgrade-past-max (is any vanilla station an upgrader? unknown) | Br | S | A:916 |
| crafting-8 | Station with `m_hasCraftTab=false` shows the upgrade list under "Craft" (vanilla station with the flag: unproven) | Br | S | A:594 |
| crafting-10 | Crafting info doesn't show the multi-craft batch amount | C | S | A:1138 |
| #189 | Ship-controls HUD can't be repositioned | C | S | I:47, Hud_Setup.cs:267 |
| #93 | Ship wind ring gives no favourable-wind cue | C | S | I:31 |
| #185 | Tamed-animal "Rename" hover line keeps the old key style | C | inconclusive | I:45 |
| text-chat-3 | Two empty NPC dialog frames may flash at load | C | S | A:1022 |
| text-chat-6 | `showDespiteHiddenHUD` not honoured; top-left postfix has no null guard | C | S | A:1107 |
| harmony-10/11/15 | Message log misses seasonal and some crafted entries, duplicates biome entries; possible NRE on a null `m_item` | C | S | A:785, 795, 1097 |
| auga-lib-9, widgets-9 | Key labels are raw Input System names; gamepad bindings print literal `<sprite=...>` | C | S | A:1001, A:1193 |
| pause-texts-7 | No Invite Friends entry in the pause menu | Br | S | A:690 |
| gamepad group | widgets-3, inventory-5, pause-texts-4, main-menu-3, crafting-7, #10, #80 | Br (gamepad only) | S | A:871, 765, 670, 616, 960; I:19, 28 |
| hud-5 | Minimap boss/death filter icons fixed by a code stopgap; prefab fix pending | C | V | A2:289 |
| leftover | Pause menu has no dark backdrop | C | V | memory note 2026-09-20 |
| leftover | Dark box behind the selected-piece readout (maybe the TMP outline dilation Morgott describes) | C | V? | A2:305, NOTES_UBERMORGOTT_FORK.md |
| build-check only | hud-9 mount HUD (needs a saddled Lox), menus-5 blocked players, menus-3 gamepad pause nav, hud-4, text-8 | ? | BC | A2:282-614 |
| no Auga skin | UnifiedPopup, SessionPlayerList, JoinCodeOverlay, ConnectionPanel, LongPressRadial, radial menu frame, LavaWarning, HoveredPieceAuthor, Achievements panel/popup, GamepadMap, Feedback, Cinematics canvas, Console, EndCredits (inferred) | C | V (ui-census) | LEDGER, A2 |

Run and not reproduced, so not listed: #13, #17, #84, #99, #108, #111, #181/#192, #212, #214.

## B. Playthrough coverage (screens no scenario visits yet)

| surface | Auga? |
|---|---|
| World creation with modifiers, manage saves, changelog, cinematics entry | Auga world panel (modifiers hidden) |
| Loading screen and world-gen progress | Auga (progress dial missing) |
| First spawn / Valkyrie intro, Hugin tutorials (large dialog) | Auga text/NPC dialog; needs a fresh character |
| Other crafting stations (cauldron, stonecutter, artisan, black forge, galdr table...) | Auga crafting panel (inferred) |
| Blast furnace, spinning wheel, windmill, eitr refinery, fermenter, beehive, cooking station, oven | Auga hover rows (inferred) |
| Variant (style) dialog | Auga |
| Piece author window | vanilla |
| Carts, ship storage, ship HUD / sailing | Auga |
| Cartography table | Auga shared toggle |
| Hildir, Bog Witch, pocket upgrades | Auga store (inferred) |
| Boss altars, boss health bar, guardian power HUD, event/raid bar, stagger, damage text | Auga (altars = hover rows) |
| Sleep, dreams | Auga LoadingBlack; dream text path unknown |
| Mistlands eitr and staffs, Ashlands lava warning, Deep North additions | eitr Auga; lava warning vanilla |
| Boss cinematics, end credits | vanilla |
| Achievements panel and popup | vanilla, unreachable |
| Emotes, feasts (radial) | vanilla radial |
| Key rebinding | Auga skin |
| Logout, save and quit | Auga |
| Multiplayer join, password, connecting, player list | mixed; needs a server |
| In-game barber | Auga |

Covered today: main menu and character screens (mainmenu); HUD, food, adrenaline, effects (auga-tour, food-respawn,
hp-regen); hotbar, hover rows (augafy, stations); inventory and split (auga-tour, augafy, s5-04, issue-62); workbench,
forge, smelter, kiln (stations); build menus (s5-11); chests (s5-07); portal tag, signs (s5-05); map and pins (s5-09);
Haldor (s5-13); enemy HUD, stars, tamed bar (texts-enemies, issues); messages, runes (audit3); death and respawn
(food-respawn); skills, compendium (auga-tour); radial hints (audit3); settings (audit3, s5-43); pause menu (audit3).

## C. Work list

**1. Fix known bugs**
1. Item tooltip 1.0 parity: modifiers, value order, NonPlayer damage, subtitle and cheated marker, no duplicated text. L
2. Achievements entry, compendium stats page. M
3. Main menu: world-modifiers button and crossplay toggle in Auga's world panel; optionally changelog and cinematics. M
4. World-gen loading indicator. S
5. Compendium: `m_tabKeyInput=false`, initial tab, #44 guard, #186 scroll raycast, bestiary labels. S-M
6. Map ping and large-map key hints. M
7. Crafting: #100 stale icon; check crafting-5/-8 against vanilla stations; batch amount. M
8. Skills "+N" (#209), food time text (#59), ship-controls offset (#189). S each
9. Build-menu hints and the "None" / nocraftcost row. S

**2. New scenarios**
10. `worldflow.chs`: new world with modifiers, manage saves, loading, logout / save and quit. M
11. `sailing.chs`: Karve, ship HUD and wind, ship storage, cart. M
12. `bosses.chs`: altar hover, Eikthyr boss bar, guardian power, raid event bar, stagger. M
13. `stations2.chs`: every remaining station UI and processing hover, variant dialog. M
14. `traders.chs`: Hildir, Bog Witch, pocket-upgrade purchase. S
15. `biomes.chs`: bed/sleep/dream/rested, eitr and staffs, lava warning, saddled Lox mount HUD, damage text. M
16. `extras.chs`: achievement popup, emotes and feast, boss cinematic, Hugin large dialog, trophies (#153). M
17. Needs the user: first-spawn intro with a throwaway character (breaks the RETEP-only rule); multiplayer join /
    password / player list (needs a server). L

**3. Cosmetic polish**
18. Pause menu backdrop, readout box, hud-5 prefab fix at the next bundle rebuild, build-count colour. M
19. Auga art for UnifiedPopup, SessionPlayerList, Achievements, piece author window, radial frame. M
20. Key-label prettifying; gamepad pass only if gamepad is in scope. L

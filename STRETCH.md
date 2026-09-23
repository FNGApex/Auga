# Stretch goals before the first alpha

These items are deferred on purpose (user decision, 2026-09-22). They stay out of `LEDGER.md` and
`HANDOFF_AUGA_SESSION.md`. Only pick them up when the user asks. The details and sources are in
`NOTES_UBERMORGOTT_FORK.md`, section 1. Those findings are Morgott's, so the credit notice at the top of that file applies.

## Mod compatibility
- [ ] **EAQS 3.1.1 slot offset.** EAQS's slots sit (+131.5, -180) off Auga's paper doll. `Panel_Create` centres the
      pivot, but EAQS assumes top-left. The bug is on EAQS's side; our options are to pin its slot root or special-case
      the pivot for it.
- [ ] **EAQS hidden rows.** Our `InventoryGrid_UpdateGui` patch re-parents every PlayerGrid element, so it fights EAQS for
      its extra rows. Re-parent only the elements still under `m_gridRoot`, and run after EAQS.
- [ ] **AAACrafting binding.** It binds through blaxxun's APIManager, so `APIManager.Patcher.Patch()` has to come back.
      The patcher also has a `MethodDefinition` bug that must be guarded.
- [ ] **EpicLoot 0.14.2.** `HasAuga` is never set, so EpicLoot always takes its vanilla path. Its UI probably lands inside
      our hidden crafting donor. Unverified.
- [ ] **VNEI 0.17.6 and AdventureBackpacks.** Both detect Auga by GUID. Check the `MainVneiHandlerAuga` NRE, and whether
      AdventureBackpacks skipping its 54 px durability-bar width is right under Auga.
- [ ] **StarLevelSystem and MonsterModifiers.** Our full EnemyHud replace breaks their `EnemyHud.Awake` postfixes. Keep
      the vanilla EnemyHud object, swap only its templates, and keep the vanilla star shape.

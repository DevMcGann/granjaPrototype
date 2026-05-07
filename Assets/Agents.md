Project Architecture Rules (Unity)

GENERAL:
- Never delete or recreate files unless explicitly requested
- Always preserve .meta files
- Prefer modifying existing scripts over creating duplicates
- Keep code modular and decoupled

FOLDER STRUCTURE:
- Scripts are organized by responsibility, not type
- Player logic → Scripts/Player
- Interaction system → Scripts/Interaction
- Gameplay features → Scripts/Gameplay
    - Farming → Gameplay/Farming
    - Animals → Gameplay/Animals
    - Systems → Gameplay/Systems
- Save system → Scripts/SaveSystem
- ScriptableObjects → ScriptableObjects/

INTERACTION SYSTEM:
- All interactable objects must implement IInteractable
- Player never references concrete classes (SowTile, ChickenCoop, etc)
- Interaction is handled via InteractionZone

GAMEPLAY RULES:
- Use state machines for gameplay logic (no multiple booleans)
- Avoid hardcoded references (use ScriptableObjects when possible)
- All timings must be configurable via serialized fields

SAVE SYSTEM:
- All gameplay objects must be serializable into data classes
- Do not store direct GameObject references in save data
- Use IDs (string) for linking data

CODE STYLE:
- PascalCase for classes and methods
- Interfaces must start with "I"
- Keep methods small and readable
- Avoid Update() unless necessary

UNITY RULES:
- Do not break prefab references
- Do not move assets outside Unity (must preserve .meta)
- Use inspector configuration instead of hardcoding

THINK BEFORE CHANGING STRUCTURE.
Always explain plan before applying changes.

----------------------------------------
AGENT HANDOFF CONTEXT

IMPORTANT FOR ANY NEW AGENT:
- Read this entire file before making changes
- Do not re-plan the project from scratch without first checking the current implementation
- Assume the current architecture below is the intended baseline unless the user explicitly changes direction

PROJECT STATUS SUMMARY:
- The project was reorganized toward this structure:
    - Scripts/Interaction
    - Scripts/Player
    - Scripts/Gameplay/Farming
    - Scripts/Gameplay/Animals
    - Scripts/Gameplay/Systems
    - Scripts/SaveSystem
    - ScriptableObjects/Crops
    - Prefabs/Gameplay
- SampleScene was wired to be playable without deleting existing scene objects
- The flat cube tiles already in SampleScene were repurposed as sow tiles instead of creating duplicate gameplay prefabs
- The farming system is now in transition from the old SowTile / SowField prototype flow to the new prefab-based CropField flow

CURRENT GAMEPLAY ARCHITECTURE:
- Interaction is proximity-based and mobile-first
- IInteractable exists in Scripts/Interaction/IInteractable.cs
- InteractionZone exists in Scripts/Interaction/InteractionZone.cs
- PlayerMovement exists in Scripts/Player/PlayerMovement.cs
- PlayerInteraction exists in Scripts/Player/PlayerInteraction.cs
- PlayerActionType exists in Scripts/Player/PlayerActionType.cs
- PlayerActionController exists in Scripts/Player/PlayerActionController.cs
- WorldContextMenu exists in Scripts/Interaction/WorldContextMenu.cs
- WorldContextMenuBillboard exists in Scripts/Interaction/WorldContextMenuBillboard.cs
- SowState exists in Scripts/Gameplay/Farming/SowState.cs
- CropData exists in Scripts/Gameplay/Farming/CropData.cs
- SowTileSaveData exists in Scripts/SaveSystem/SowTileSaveData.cs
- FieldState exists in Scripts/Gameplay/Farming/FieldState.cs
- CropSeedData exists in Scripts/Gameplay/Farming/CropSeedData.cs
- CropField exists in Scripts/Gameplay/Farming/CropField.cs
- CropFieldSaveData exists in Scripts/Gameplay/Farming/CropFieldSaveData.cs
- CropCrateInstance exists in Scripts/Gameplay/Farming/CropCrateInstance.cs

CURRENT FARMING IMPLEMENTATION:
- SowTile exists in Scripts/Gameplay/Farming/SowTile.cs
- PrototypeFieldController still exists in Scripts/Gameplay/Farming/PrototypeFieldController.cs and must not be deleted yet
- SowTile uses a state machine:
    - Empty
    - Sowing
    - Growing
    - ReadyToHarvest
    - Harvesting
- Growing remains time-based
- Harvesting remains time-based once started
- Sowing was refactored so it is NO LONGER driven by a ParticleSystem owned by SowTile
- SowTile now reacts to player particle collisions using OnParticleCollision(GameObject other)
- SowTile validates particle hits through PlayerSeedEmitter, not by hardcoded references
- Sowing only progresses when valid seed particles actually hit the tile
- If no particle hits occur, sowing does not progress
- SowTile has audio placeholders:
    - sowingSound
    - harvestingSound
    - harvestCompleteSound
- Audio fields are optional and null-safe

NEW CROPFIELD IMPLEMENTATION:
- CropField prefab path:
    - Prefabs/Gameplay/Prototype/Fields/CropField.prefab
- CropField is the new prefab-based farming baseline
- CropField uses FieldState only:
    - Empty
    - Sowed
    - Watered
    - Growing
    - ReadyToHarvest
    - Harvesting
    - BlockedByCrate
- CropField owns gameplay/state only
- The context menu UI is separate and reusable
- CropField uses the existing ActionTrigger child with InteractionZone for proximity activation
- CropField uses the existing Crops child as the growth visual root
- Crops stays hidden at local Y = -15 until growth starts
- Growth animates Crops local Y from -15 to +5
- CropField currently drives two different UI modes through WorldContextMenu:
    - action menu mode
    - progress mode
- When a timed action begins, the action menu hides and the progress UI appears
- Timed UI progress is currently used for:
    - Sowed timer
    - Watered timer
    - Growing timer
    - Harvesting timer
- CropField now coordinates player actions through:
    - PlayerActionType
    - PlayerActionController
- PlayerActionController owns:
    - Animator triggers for Sow / Water / Harvest / Pickup
    - movement lock/unlock through PlayerMovement.SetMovementEnabled(bool)
    - busy-state guarding while a timed action is running
- Gameplay timer is the source of truth for Sow / Water / Harvest action completion
- Growing remains automatic and does not lock the player
- CropField currently exposes:
    - defaultWaterTime
    - menuOffset
    - menuScreenOffset
    - progressOffset
    - progressScreenOffset
- If the player is still inside the trigger when a timed action completes, the action menu should reappear with the updated state
- If the player leaves the trigger while a timed action is still running, progress UI is intended to remain visible until the timer completes
- CropField currently uses:
    - CornSeedData asset for timing data
    - CornCropVisual prefab as the configured crop prefab fallback
    - CornCrate prefab as the harvest crate prefab
- Crate lifecycle is handled through CropCrateInstance notifying CropField when the spawned crate is removed
- Save/load entry points on CropField are:
    - GetSaveData()
    - LoadFromData()
- Bonus support currently uses:
    - isBonusApplied
    - bonusMultiplier
- Bonus currently affects grow time
- The current implementation is structured to allow:
    - future inventory checks before sowing
    - multiple crop types through crop options
    - additional actions through the reusable menu layer

CURRENT WORLD UI IMPLEMENTATION DETAILS:
- WorldContextMenu is currently a world-space canvas
- WorldContextMenuBillboard currently keeps the menu parented to the CropField transform
- The menu uses local offsets relative to the CropField, not overlay projection
- The menu is intentionally not billboarded toward the camera
- Current default CropField offsets are:
    - menuOffset = (-1.4, 0, 0)
    - menuScreenOffset = (0, 0)
    - progressOffset = (0, 0, -1)
    - progressScreenOffset = (0, 0)
- CropField currently exposes separate offsets for menu and progress:
    - menuOffset
    - menuScreenOffset
    - progressOffset
    - progressScreenOffset
- The latest intent from the user is:
    - action menu should appear to the side of the CropField and not cover it
    - progress UI should appear below the field
    - text/buttons must be readable on mobile
- The UI was recently enlarged for readability:
    - wider panel
    - taller buttons
    - larger button labels
    - larger progress label
- WorldContextMenu currently keeps:
    - fixed panel width
    - dynamic panel height based on action count
    - fixed progress panel height

CURRENT WORLD UI STABILITY NOTES:
- This UI has gone through several iterations and was previously unstable
- Known previous failure modes included:
    - panel filling the entire screen
    - panel clipping so only part of Sow was visible
    - text mirroring
    - menu appearing over the CropField instead of to the side
- If a new agent touches WorldContextMenu or WorldContextMenuBillboard, inspect the current world-space child-follow logic first before replacing it
- Prefer small targeted adjustments to:
    - world anchor offset
    - screen offset
    - panel width/height
    - font sizes
  rather than switching UI paradigms again unless the user explicitly approves

PLAYER PARTICLE EMITTER IMPLEMENTATION:
- PlayerSeedEmitter exists in Scripts/Player/PlayerSeedEmitter.cs
- This component is attached to the Player in SampleScene
- PlayerSeedEmitter is the approved identifier for valid seed particles
- Do not replace it with direct SowTile-to-Player references unless the user explicitly requests it
- PlayerSeedEmitter currently ensures a ParticleSystem exists at runtime if missing
- It configures particle collision and send-collision-messages technically, without redesigning visuals
- It exposes TryGetSowingProgress(GameObject hitObject, out float progressDelta)

SCENE WIRING THAT ALREADY EXISTS:
- SampleScene contains the Player with:
    - PlayerMovement
    - PlayerInteraction
    - PlayerActionController
    - Rigidbody
    - SphereCollider trigger for proximity detection
    - PlayerSeedEmitter
- SampleScene contains multiple scene tiles renamed as:
    - SowTile_01
    - SowTile_02
    - SowTile_03
    - SowTile_04
- Those scene tiles already have:
    - collider for the tile itself
    - trigger collider for interaction zone
    - InteractionZone
    - SowTile
- CornCropData exists and is used as the default crop data
- Prefabs/Gameplay currently contains helper prefabs used by the farming loop
- SampleScene now points its prototype field instance at Prefabs/Gameplay/Prototype/Fields/CropField.prefab
- The project now expects the Player tag to exist in TagManager.asset
- SampleScene is the primary manual validation scene for the new CropField flow

IMPORTANT KNOWN CONSTRAINTS:
- Do not delete or recreate existing files just to refactor
- Preserve Unity references and .meta files
- Prefer adapting the current implementation over replacing it wholesale
- Do not reintroduce keyboard assumptions; this is a mobile project
- Do not move scene objects or prefab hierarchies unnecessarily
- Do not redesign visuals unless the user explicitly asks

KNOWN VALIDATION GAP:
- The previous agent could not run Unity Editor validation from the terminal
- Assume the code and YAML wiring were prepared carefully, but runtime/editor validation still needs confirmation inside Unity
- If something seems off, inspect the existing implementation first before replacing it
- The current WorldContextMenu positioning/scale is still being tuned live with the user inside Unity
- Treat current UI placement values as working iteration values, not final design values

PROMPT FOR THE NEXT AGENT:
You are working on a Unity (C#) mobile farming game in this repository.

Before doing anything else:
1. Read Assets/Agents.md completely.
2. Use the implementation described there as the current project baseline.
3. Do NOT re-architect from scratch.
4. Do NOT delete or recreate existing files.
5. Preserve Unity references and .meta files.
6. Show a plan before applying changes.

Important current context:
- The game is mobile-first.
- Interaction is automatic by proximity.
- CropField is now the intended prefab-based farming path for further work.
- SowTile and PrototypeFieldController still exist and should not be deleted without explicit approval.
- SowTile already exists and has been refactored to use player particle collision for sowing.
- PlayerSeedEmitter is the current safe identifier for valid sowing particles.
- SampleScene is wired with PlayerInteraction, PlayerSeedEmitter, and a CropField prefab instance for the new farming flow.
- SampleScene is wired with PlayerInteraction, PlayerActionController, PlayerSeedEmitter, and a CropField prefab instance for the new farming flow.
- PlayerActionController is the intended integration point between farming actions and player animation/movement lock.
- Growing and harvesting are still time-based.
- Audio placeholders already exist on SowTile.
- WorldContextMenu is currently a world-space child-follow UI aligned with the CropField.
- The user is actively iterating on menu placement, size, and readability in mobile conditions.
- If continuing this UI work, preserve the current CropField gameplay flow and focus on placement/scale tweaks before any larger refactor.

Your job is to continue from the current implementation, not to rebuild it from zero.

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

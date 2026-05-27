# NeonLap prefabs

Runtime code still **builds** hover cars and tracks by default (fast iteration). When art is ready:

1. Play **SampleScene** once so cars exist in the hierarchy (or duplicate a runtime-built car from a recorded session).
2. Drag the configured player car to `HoverCar_Player.prefab` and each AI template to `HoverCar_AI.prefab`.
3. Add **NeonLap → Production → Mark Selection As Player/AI Car Prefab** so `NeonLapCarPrefabRoot` is set.
4. **NeonLap → Production → Create Content Catalog** (if missing), then assign prefabs on `NeonLapContentCatalog`.
5. On the **NeonLap** scene object, assign the catalog or leave empty to load from `Resources/NeonLap/NeonLapContentCatalog`.

Track **colliders and checkpoints** remain procedural (`OvalTrackBuilder`). Optional `TrackEnvironmentPrefab` on the catalog is for decorative props only.

See [`docs/PRODUCTION.md`](../../../docs/PRODUCTION.md) for Addressables migration.

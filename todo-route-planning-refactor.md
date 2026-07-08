# RoutePlanningService Refactor — To-Do

## 1. Fix `IRgvRoutePlanning.Solve` tuple + typo — DONE
- [x] `IRgvRoutePlanning.cs`: name the `Solve` return tuple elements (`RawPath`, `SmoothedPath`) instead of `(IEnumerable<PathPoint>, IEnumerable<PathPoint>)`.
- [x] `RgvRoutePlanning.cs`: update `Solve` implementation to return the named tuple.
- [x] Update call sites (`RoutePlanningService.SolveClusterRoute`, `SolveConnectorRoute`) to use the named elements instead of `var (result, _) = ...`.
- [x] `IRoutePlanningService.cs` / `RoutePlanningService.cs`: rename `imageSteam` → `imageStream` (interface fixed; implementation and controller already used the correct name).

## 2. Extract cluster/connector solving into `ClusterFlowRouteSolver` — DONE
- [x] Create `IClusterFlowRouteSolver` + `ClusterFlowRouteSolver` (Application layer, `Application/Services/RoutePlanningService/`).
- [x] Move `SolveClusterRoute`, `SolveConnectorRoute`, `GetStationPermutations`, `GetAllPermutations`, `GetRandomDistinctPermutations`, `IsBetterScore`, `FindNearestConnector` there.
- [x] Move `ClusterGenerationsNumber`, `ConnectorGenerationsNumber`, `MaxClusterPermutations` (renamed → `ClusterPermutationSampleSize`) constants with it.
- [x] Replace `Console.WriteLine` ANSI logging with `_logger.LogInformation` via injected `ILogger<ClusterFlowRouteSolver>`.
- [x] `RoutePlanningService.SolveRoute` calls `_clusterFlowRouteSolver` instead of the private methods directly.
- [x] Registered `IClusterFlowRouteSolver` in `Application/DependencyInjection.cs`.

## 3. Extract draw/write/persist tail of `SolveRoute` — DONE
- [x] Created `IRouteResultPersister` + `RouteResultPersister` (Application layer) owning: `DrawMultipleFlows` → `WriteImage` → `ToRoutePlanningDto` → `WriteToJson` → mission status transition.
- [x] Kept the try/catch inside `Persist`; still one generic failure log (per-stage failure detail deferred — not done in this pass, flagging as a possible future follow-up).
- [x] `RoutePlanningService.SolveRoute` now: solves clusters/flows → calls `_routeResultPersister.Persist(...)` → updates mission/repo → dispatches domain events.
- [x] Registered `IRouteResultPersister` in `Application/DependencyInjection.cs`.

## 4. Introduce `RoutePlanningRequest` parameter object — DONE
- [x] Defined `RoutePlanningRequest` record (`Application/DTOs/RoutePlanning/RoutePlanningRequest.cs`) bundling: `MissionId`, `ImageStream`, `Algorithm`, `RowDim`, `ColDim`, `WidthLength`, `HeightLength`, `Points`, `Clusters`, `ClusterFlows`.
- [x] Updated `IRoutePlanningService.FindRgvBestRoute` to take the single request object.
- [x] Updated `RoutePlanningService.FindRgvBestRoute` implementation (deconstructs the record positionally at the top, rest of the method body unchanged).
- [x] Updated `MissionController` call site to construct `new RoutePlanningRequest(...)`.

## 5. Clean up validation plumbing — DONE
- [x] Replaced `ToPathPoints`'s `(bool isError, Result? value)` + `out` pattern with `Result<List<PathPoint>>`.
- [x] Made `PointFactory.Create` return `Result<PathPoint>` instead of throwing `ArgumentException` (fixed a latent copy-paste bug in the process: the missing-`processingTime` branch used to say "Station requires a name." instead of "...a processing time."). Updated the one other call site (`Grid.CreateMapMatrix`, which can never fail for `PointCategory.Path`, so it unwraps via `.Value`).
- [x] Added a `RequireValid<T>(Result<T> result, string context)` helper that logs a warning and wraps the failure in `ApplicationError.Validation` — applied to `missionIdResult`, `pathPointsResult`, `algorithmResult`, `clusterResult`, `clusterFlowResult`, `gridResult`, `rgvMapResult`, collapsing each from ~5 lines to 2.
- [x] `ClusterGenerationsNumber`/`ConnectorGenerationsNumber`/`MaxClusterPermutations` already moved out in step 2 — nothing left to relocate.

## Verification
- [ ] `dotnet build` after each step.
- [ ] Manual smoke test of the route-planning endpoint after step 4 (signature change) to confirm the controller still compiles/serializes correctly.

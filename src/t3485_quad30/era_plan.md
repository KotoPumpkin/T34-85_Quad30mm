# T3485Q30MOD 1.3.0 ERA implementation

User requests real Fulda Expansion ERA on hull front/both sides, turret front/both sides and unoccupied forward roof. Hard constraint: no model clipping. Preserve v1.2 engine/ammo/fire-control/sights/missile/hatch changes.

- Reuse exact installed Fulda1989.Kontakt1.Setup API and original tile mesh/material, independently pair each visual/collider. Runtime dependency on installed Fulda accepted by the explicit reuse request; do not rewrite or redistribute its DLL/bundles.
- Main agent owns local surface proxies, placement, exclusion/swept-volume checks and integration. Worker owns only FuldaEraAdapter.cs.
- Build temporary static physics proxies from actual native T34 hull/turret skinned geometry. Exclude movable bones from mounting surfaces; reserve optics/hatches/guns/accessories and native missile assembly.
- Project a separated tile grid onto requested armor surfaces. Test the entire tile footprint against surface curvature, move each tile outward for positive clearance, reject obstruction/tile intersections. Leave unsafe cells empty.
- Attach hull and turret groups to their own rig bones, not the gun. No permanent proxies or per-frame mesh rebuilds.
- Add meaningful pure geometry regression tests and native audits: actual Fulda ERA resistances/consumption, per-instance ownership, tile/body/accessory/tile clearances across gun/turret/missile poses, seven region counts, render captures.
- Back up v1.2, build/update v1.3, run existing and new tests, install/package docs with dependency, verify all original hashes.

Completed native preflight:109 Kontakt-1 tiles (hull front21,left29,right19; turret front11,left11,right11,roof7);1.5 mm surface stand-off;240 native-limit sampled motion poses;25 footprint probes;closed-mesh containment fallback;per-tile native spent-state isolation;seven real-model render views. Existing ammo/engine/sights/hatch checks passed. Read-only review completed with no further material findings. No active mission projectile/drive test or proof of continuous-motion clearance.

Completed v0.1.0 native preflight: T-34 front/side structural and all T34_Turret_armour fields +50mm on 18 components; front glacis60, side plate95, turret casting100; original donor armor unchanged.

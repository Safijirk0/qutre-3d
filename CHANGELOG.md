# Changelog

All notable changes to **Qutre 3D** are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project uses [Semantic Versioning](https://semver.org/).

## [0.3.0] — 2026-11-05

### Added

- **Mug** primitive: a surface-of-revolution body with a hollow rim, inner wall
  and a C-shaped handle swept along a Bezier curve.
- **Teapot** primitive: a closed lathe body with lid and knob, a tapered spout
  and a curved handle.
- Code-drawn shelf icons, layer dot colors and inspector titles for both shapes.
- Mug and teapot in the Bryce demo scene, sitting on the checker floor.
- `CHANGELOG.md` and a GitHub Actions build workflow.

### Changed

- Soft contact shadows now support six casters instead of four.
- README refreshed with new screenshots, a 0.3.0 download table and a
  "What's new" section (English and Russian).

## [0.2.0] — 2026-11-04

### Added

- Bryce preset and demo scene: electric sky, checker floor, chrome, glass, gold.
- Checker floor primitive with two tile colors, tile size and roughness.
- Sky and floor reflections, Fresnel glass, soft contact shadows and
  anti-aliased reflected checker.
- Redesigned Bryce-inspired interface with a primitive shelf.
- Bilingual RU/EN UI, 1920×1080 PNG export and JSON scenes.
- Procedural terrain, ocean, fog and cinematic post-processing.

### Fixed

- Reflection aliasing on the checker floor and floor LOD at grazing angles.
- Preset switching now keeps the plane/terrain visibility consistent.

## [0.1.0] — 2026-11-03

- Initial editor prototype: terrain, water, camera, materials, presets and export.

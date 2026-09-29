# Changelog

One line per release. Full notes with bench data are in `docs/F22E_NO_FCS_<version>_notes.md`. Results are from an offline 6-DOF bench, not flight tests, unless noted.

- **1.9.7**: AoA → pitch-rate hand-over on the negative side
- **1.9.6**: AoA protection no longer overrides a nose-down stick at the limit
- **1.9.5**: low-thrust AoA cap comes back at once on throttle-up; floating hold no longer latches full stick
- **1.9.4**: 20 % less yaw stop time at 290-330 km/h
- **1.9.3**: more yaw rate at 150-210 km/h, roll-ceiling limit cycle removed, AoA-protection hitch removed
- **1.9.2**: yaw braking floor re-referenced to 270 km/h, smooth low-IAS margin schedule
- **1.9.1**: stick map for the AoA → pitch-rate hand-over, HUD rate label, low-thrust cap rise limit
- **1.9.0**: pitch command shaping, hybrid INDI, AoA-to-rate hand-over, low-thrust AoA cap
- **1.8.45**: asymmetric cap: smoothing, +~3°, IAS-decay anticipation; high-AoA lateral braking floor
- **1.8.44**: player config trimmed, dev/debug unlock DLL
- **1.8.43**: one DLL, performance profile in the config
- **1.8.42**: roll pitch-fight cost, nozzle seam shading
- **1.8.41**
- **1.8.40**

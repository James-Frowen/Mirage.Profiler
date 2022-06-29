# Mirage.Profiler (work in progress)

Network Profiler for [Mirage](https://github.com/MirageNet/Mirage) using the new [unity profiler package](https://docs.unity3d.com/Packages/com.unity.profiling.core@1.0/manual/index.html)


Thanks to https://github.com/JesusLuvsYooh/MirrorNetworkProfiler for some reference code.


## How to use:
1) Add Requires package: "com.unity.profiling.core": "1.0.2",
2) Add NetworkProfilerBehaviour to NetworkManager
3) Open Unity's Profiler and select the "Network Profiler" module
4) Press play, and start server

## Development TODO

- [x] Send and received counters
- [x] Bytes per second
- [x] Table for messages
- [ ] Flexable width of table
- [ ] ScrollBar for table (height get squashed with no scrollbar)
- [ ] Group by Message type
- [ ] Saving messages so that they can be viewed outside of playmode
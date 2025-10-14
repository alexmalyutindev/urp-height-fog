
![screen](./Pictures/Screenshot_20251014-143244.png)

Main points:
- I have chosen Unity 6.0.59f1 instead of 6.0.56f1 because of security vulnerability.
- The `HeightFogFeature` pass was injected before `RenderPassEvent.BeforeRenderingTransparents`, to be able to use DepthCopy and ColorCopy made after Opaque and AlphaTest geometry.
- Feature is using _

### MaliOC report:
```yaml
Mali Offline Compiler v8.8.0 (Build 888cd7)
Copyright (c) 2007-2025 Arm Limited. All rights reserved.

Configuration
=============

Hardware: Mali-G1 r0p1
Architecture: Arm 5th Generation
Driver: r55p0-00rel0
Shader type: Vulkan Fragment

Main shader
===========

Work registers: 13 (40% used at 100% occupancy)
Uniform registers: 14 (10% used)
Stack use: false
16-bit arithmetic: 25%
- Idle SIMD lanes: 30%

                               A     FMA     CVT     SFU      LS       V       T    Bound
Total instruction cycles:    0.25    0.17    0.03    0.25    0.00    0.06    0.25 A, SFU, T
Shortest path cycles:        0.25    0.17    0.03    0.25    0.00    0.06    0.25 A, SFU, T
Longest path cycles:         0.25    0.17    0.03    0.25    0.00    0.06    0.25 A, SFU, T

A = Arithmetic, FMA = Arith FMA, CVT = Arith CVT, SFU = Arith SFU,
LS = Load/Store, V = Varying, T = Texture

Shader properties
=================

Has uniform computation: true
Has side-effects: false
Modifies coverage: false
Uses late ZS test: false
Uses late ZS update: false
Reads color buffer: false
```

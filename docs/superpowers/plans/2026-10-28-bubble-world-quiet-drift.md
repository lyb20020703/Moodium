# Bubble World Quiet Drift Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 给泡泡世界的分层点云增加低成本、安静的微漂浮效果。

**Architecture:** `BubbleWorldController` 保持现有 baked layer mesh 不变，保存每层初始局部姿态，并在 `Inside` 阶段只更新 20 个层级 Transform。退出和重新进入时恢复姿态，确保进入动画与扫描成像不被破坏。

**Tech Stack:** Unity C#, Transform animation, existing URP/Unlit baked point-cloud meshes.

**Spec:** `docs/superpowers/specs/2026-10-28-bubble-world-quiet-drift-design.md`

## Global Constraints

- 不逐点改写 mesh 顶点。
- 位移保持 0.0015–0.003 m，旋转不超过约 0.2 度。
- 只在 `Inside` 阶段启用，每帧最多更新 20 个 Transform。

## Review Focus

- 点云尚未完成 Build 时不访问未初始化的漂移缓存。
- 进入/退出/重复进入后层级姿态不能累积偏移。
- Reveal 阶段的层级启停和缩放不能被漂移逻辑覆盖。
- Nature 与 Candy 两个资源文件夹都使用相同且稳定的表现。

### Task 1: Add quiet layer drift

**Files:**
- Modify: `Assets/Scripts/Moodium/BubbleWorld/BubbleWorldController.cs`
- Test: `git diff --check`; Unity compilation when the project is available

**Interfaces:**
- Consumes: existing `m_Layers`, `m_WorldRoot`, `Phase`, and `Time.time`.
- Produces: private `UpdateQuietLayerDrift()` behavior; no public API changes.

- [ ] Add private arrays for initial local positions/rotations and deterministic per-layer phases.
- [ ] Populate/reset the arrays when the point layers are built and cached.
- [ ] Add `UpdateQuietLayerDrift()` that runs only in `Phase.Inside`, applies bounded sinusoidal local motion, and restores identity offsets for other phases.
- [ ] Call it from the existing `Update()` without allocations.
- [ ] Verify `git diff --check` and inspect the diff for no mesh/material changes.

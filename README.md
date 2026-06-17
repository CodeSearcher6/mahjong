# Mahjong Solitaire

A Mahjong Solitaire game built in C# with WPF, developed as a university coursework
project. The player clears the board by matching pairs of free tiles, with hints,
undo/redo and shuffling to help when no moves are left.

<img width="1478" height="887" alt="Знімок екрана 2026-04-07 150307" src="https://github.com/user-attachments/assets/99483843-002a-4da2-b5b1-ce7e9ddec7ff" />

## Features
- Classic Mahjong Solitaire gameplay — match pairs of free tiles to clear the board
- Tile availability checks (only "free" tiles can be selected)
- Pair-matching validation
- Hint system that highlights an available move
- "No moves left" detection
- Tile shuffling when the board is stuck
- Undo and redo

## Tech stack
- **Language:** C#
- **UI:** WPF (Windows Presentation Foundation)

## Design
The project was planned with a class diagram and a structural scheme, with
functionality split across the main components before implementation.

## How to run
1. Open the solution in Visual Studio (or run with `dotnet run`).
2. Build and start the project (Windows + .NET required for WPF).

## What I learned
- Modeling game rules and board state in C#
- Implementing algorithms: tile availability, pair detection, hint search, move checks, shuffling
- Building a desktop GUI with WPF and handling user interaction
- Designing the app structure up front with class and structural diagrams

---
*University coursework. Built by [Vladyslav Suprun](https://github.com/CodeSearcher6).*

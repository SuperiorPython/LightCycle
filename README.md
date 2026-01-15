# Light Cycle (Tron-Inspired Unity Game)

A Tron-inspired **light cycle arena game** built in Unity, featuring grid-based movement, AI opponents, trail collisions, and a competitive scoring system.

This project is being developed collaboratively by a **3-person team**, with each member responsible for extending a specific aspect of the game beyond the core MVP.

---

## 🎮 Game Overview

Players control a light cycle inside a rectangular arena.  
Each cycle leaves behind a solid trail that acts as a deadly obstacle.

### Core Rules
- Cycles move continuously on a grid
- Trails persist and cause collisions
- Crashing into a wall or trail eliminates the cycle
- Last surviving cycle wins

---

## ✅ Current MVP Features

### Gameplay
- Player vs **up to 5 AI opponents**
- Grid-based movement with fixed ticks
- No instant reverse movement (classic Tron rule)
- AI logic that navigates, avoids walls, and crashes into trails

### Arena
- Fixed camera showing the **entire arena**
- Neon-style arena borders
- World-space trails that remain in place

### Scoring System
- **Passive points** earned for surviving
- **Aggressive points** awarded for eliminating another bike
- Scoreboard dynamically reorders by score
- Eliminated bikes:
  - Turn **red** in the scoreboard
  - Lock into the lowest available position
  - Stop rearranging

### UI
- Top-right scoreboard panel
- Real-time score updates
- Clear visual distinction between alive and eliminated players

---

## 🧠 Technical Highlights

- Custom **ArenaGrid** system with cell ownership tracking
- AI and player bikes share the same movement and collision rules
- Modular TrailManager per bike
- Event-driven crash handling
- Separation of gameplay logic, AI, and UI layers

---

## 👥 Team Responsibilities (Planned Work)

This section outlines how development will be split among the three team members moving forward.

### 🧑‍💻 Bryson — Game Mechanics & Systems
**Primary focus: core game flow and structure**
- Game over / win screen
- Title screen & main menu
- Arena variants and scaling
- Match flow and state management
- Polishing core mechanics

> *Owner of gameplay systems, arena logic, and overall structure.*

---

### 🧑‍💻 Member 2 — AI Enhancements *(Placeholder)*
- AI difficulty levels
- Smarter pathfinding / prediction
- Aggressive vs defensive AI behaviors
- Personality-based AI styles

> *(Details to be finalized)*

---

### 🧑‍💻 Member 3 — Visuals & Polish *(Placeholder)*
- Visual effects (glow, trail effects)
- UI polish and animations
- Sound effects and music
- Feedback on crashes and eliminations

> *(Details to be finalized)*

---

## 🛠 Tech Stack
- **Unity**
- **C#**
- Custom AI logic
- Grid-based arena system
- Unity UI (Text - Legacy)

---

## 📌 Project Status
- ✔ Core gameplay complete (MVP)
- ✔ AI, scoring, and UI functional
- 🔜 Menus, game over screens, polish, and extensions

---

## 🎯 Future Ideas
- Power-ups
- Arena hazards
- Multiplayer modes
- Match replays
- Difficulty scaling

---

*This project serves as both a game development exercise and a portfolio piece demonstrating AI behavior, systems design, and Unity architecture.*

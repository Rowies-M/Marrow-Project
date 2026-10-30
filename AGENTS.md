
# Marrow - Project Instructions

## Project overview
Marrow is a small top-down web RPG developed as a bootcamp final project.

The goal is to demonstrate the concepts learned throughout the bootcamp while keeping the scope manageable.

## Technology stack

### Backend
- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- REST architecture
- JWT authentication

### Frontend
- React
- Vite
- Tailwind CSS
- JavaScript

## Architecture

The backend must follow a layered architecture:

- Controllers: HTTP endpoints and request handling.
- Services: business logic and validation.
- Repositories: data access.
- DTOs: data transfer between frontend and backend.
- Models: domain and persistence entities.

Use dependency injection and asynchronous operations where appropriate.

## Game concept

- Top-down 2D perspective.
- Small dungeon.
- One playable class: Warrior.
- Four-direction movement.
- Turn-based combat.
- Basic attack and two abilities.
- Enemies: Skeleton, Demon and Boss.
- Experience and character progression.
- Save and load progress.
- No inventory in the initial MVP.

## Development rules

1. Keep the implementation simple and maintainable.
2. Do not introduce unnecessary dependencies.
3. Do not overengineer the architecture.
4. Follow REST conventions and use appropriate HTTP status codes.
5. Validate incoming requests using DTOs and DataAnnotations.
6. Keep game business rules in the backend.
7. The frontend handles rendering, movement input and visual feedback.
8. Never send every movement frame to the backend.
9. Document important architectural decisions.
10. Do not implement features outside the agreed MVP without approval.

## Collaboration rules

- Explain the purpose of significant changes.
- Work in small, reviewable stages.
- Before modifying multiple files, explain the planned changes.
- Do not delete or overwrite existing work without approval.
- Do not install additional dependencies without explaining why.
- Do not execute Git push or publish changes without explicit approval.
- Run relevant builds and tests after implementation.
- Prefer clear, readable code over unnecessary abstractions.

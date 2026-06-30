# core-moteur

`core-moteur` is the Astrea-EIP engine repository.
It owns core business computation and domain logic consumed by other services.

## What belongs here

This repository owns:

- engine and calculation logic
- domain rules and route or scoring behavior
- engine-specific tests and artifacts
- repository-local engine documentation

This repository does not own:

- frontend or mobile UI concerns
- deployment environment state
- central contribution standards

## Local development

The repository includes a `justfile` for common commands.
Use `just --list` to inspect the available shortcuts.

Typical validation commands:

```bash
dotnet build lib -c Release
dotnet test tests --configuration Release
```

## Documentation

Repository-specific documentation lives under `docs/`.
The shared handbook lives in `Astrea-EIP/docs`.

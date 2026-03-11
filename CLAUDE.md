# CLAUDE.md — NexTraceOne

This file provides guidance for AI assistants (Claude Code and similar tools) working in this repository.

---

## Project Overview

**Repository:** `dlimadev/NexTraceOne`
**Status:** Initial setup — no source code committed yet.

NexTraceOne is a project currently in its bootstrapping phase. This CLAUDE.md will be updated as the codebase evolves. When code is added, update the relevant sections below.

---

## Repository State

As of the initial commit, the repository contains only this CLAUDE.md file. There are no:
- Source files
- Package manifests (package.json, pyproject.toml, Cargo.toml, etc.)
- Build scripts
- Test suites
- CI/CD pipelines
- Configuration files

All sections below are scaffolded and should be filled in as the project grows.

---

## Directory Structure

```
NexTraceOne/
├── CLAUDE.md          # This file
└── (to be populated)
```

Update this tree whenever significant directories or files are added.

---

## Technology Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| TBD   | TBD       | TBD   |

---

## Development Setup

### Prerequisites

Document required tools, runtime versions, and system dependencies here.

```bash
# Example — replace with actual setup commands
# node --version  # vX.X.X
# python --version  # X.X.X
```

### Initial Setup

```bash
git clone <repo-url>
cd NexTraceOne
# install dependencies
# configure environment
```

### Environment Variables

Document required environment variables here. Never commit secrets.

```bash
# Copy example env file and fill in values
cp .env.example .env
```

| Variable | Required | Description |
|----------|----------|-------------|
| TBD      | TBD      | TBD         |

---

## Common Commands

Update this table as scripts are added to the project.

| Command | Purpose |
|---------|---------|
| TBD | Build the project |
| TBD | Run tests |
| TBD | Run linter |
| TBD | Start dev server |

---

## Testing

Document the testing framework, how to run tests, and testing conventions here.

```bash
# Run all tests
# Run a single test file
# Run tests with coverage
```

### Testing Conventions

- TBD: test file naming pattern
- TBD: test directory location
- TBD: mocking approach

---

## Code Conventions

### General

- Follow the conventions already present in the codebase.
- Keep changes minimal and focused — do not refactor code unrelated to the task.
- Do not add comments, docstrings, or type annotations to code you did not change.

### Linting & Formatting

Document linter and formatter configuration files here once added (e.g., `.eslintrc`, `pyproject.toml`, `.prettierrc`).

---

## Git Workflow

### Branch Naming

- Feature branches: `feature/<short-description>`
- Bug fixes: `fix/<short-description>`
- Claude-generated branches: `claude/<task-id>`

### Commit Messages

Use concise, imperative-style commit messages:
```
Add user authentication module
Fix null pointer in trace parser
Refactor config loading to use env vars
```

### Pull Requests

- Keep PRs small and focused on a single concern.
- Include a clear description of what changed and why.
- Reference related issues where applicable.

---

## AI Assistant Guidelines

### When Asked to Implement Features

1. Read all relevant existing files before making changes.
2. Understand the existing patterns before introducing new ones.
3. Make the minimum change necessary to satisfy the request.
4. Do not add error handling, logging, or validation beyond what is needed.
5. Do not create new files when editing an existing one is sufficient.

### When Asked to Fix Bugs

1. Reproduce or understand the bug before touching code.
2. Fix the root cause, not just the symptom.
3. Do not refactor surrounding code as part of a bug fix.

### When Asked to Explain Code

1. Read the code thoroughly before explaining.
2. Be precise and cite file paths and line numbers when relevant.

### Things to Avoid

- Do not push to branches other than the one designated in the current task.
- Do not commit secrets, credentials, or environment files.
- Do not add dependencies without explicit request.
- Do not use `--no-verify` to bypass git hooks.
- Do not force-push without explicit user approval.

---

## Security Notes

- Never commit `.env` files or any file containing secrets.
- Validate input at system boundaries (user input, external APIs).
- Avoid introducing known OWASP Top 10 vulnerabilities.

---

## Updating This File

Update CLAUDE.md whenever:
- A new major dependency is added
- A new directory is introduced with a non-obvious purpose
- A workflow or convention changes
- A common pitfall is discovered

Keep this file accurate and concise — it is read by AI assistants on every task.

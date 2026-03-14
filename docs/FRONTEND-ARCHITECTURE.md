# FRONTEND-ARCHITECTURE.md

## Principles

Feature based architecture aligned with bounded contexts.

## Structure

src/features/{context}/pages src/features/{context}/components
src/features/{context}/api src/shared/components src/shared/hooks
src/shared/utils

## Rules

All UI text must use i18n. Persona aware layouts. Reusable design
system. API calls via centralized client.

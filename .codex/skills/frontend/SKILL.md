---
name: frontend
description: Use when working on the NetClaw frontend in `frontend/` including React 19 pages, TanStack Router routes, React Query hooks, API services, forms, shared UI components, and Tailwind styling.
---

# NetClaw Frontend

Use this skill for frontend changes under `frontend/`.

## Stack

- React 19 + TypeScript + Vite
- TanStack Router — file-based routes in `frontend/src/routes/`
- TanStack Query — server state, caching, mutations in `frontend/src/hooks/api/`
- API services in `frontend/src/services/api/`
- Shared components in `frontend/src/components/`
- Tailwind CSS v4 + `clsx` + `tailwind-merge`

## Repo Map

```
frontend/src/
├── routes/                  # TanStack Router file-based route files
├── pages/                   # Page-level UI and screen composition
├── hooks/api/               # React Query wrappers (useQuery / useMutation)
├── services/api/            # HTTP clients per resource (*-service.ts)
├── @types/models/           # Frontend request and response types
├── components/              # Shared UI: forms, cards, dialogs, tables, shell
└── lib/                     # API client, helpers, shared utilities
```

## Backend Contract Alignment

The backend uses `NetClaw.Contracts` records as the source of truth for request/response shapes. When backend contracts change, update the matching types in `frontend/src/@types/models/` and service payloads in `frontend/src/services/api/` in the same task.

## Working Rules

1. **Find the full vertical slice** before editing: route → page → hook → service → model type.
2. **Data layer separation**: HTTP calls live in `services/api/`; cache/mutation/invalidation logic lives in `hooks/api/`; page composition lives in `pages/` or `routes/`.
3. **React Query conventions**: match existing query-key patterns and invalidation strategy when extending hooks.
4. **Reuse before creating**: check `components/` for existing form, card, dialog, and table primitives before adding new ones.
5. **Route files**: preserve `createFileRoute` pattern; do not move business logic into route files.
6. **Type safety**: never use `any` — model types must reflect actual backend contract shapes.

## Typical Change Paths

### New screen or route

1. Add route file under `frontend/src/routes/`.
2. Implement page content under `frontend/src/pages/`.
3. Add query/mutation hooks in `frontend/src/hooks/api/` if the page fetches or mutates server data.
4. Add service calls in `frontend/src/services/api/`.
5. Add or update model types in `frontend/src/@types/models/`.

### CRUD/resource change

1. Start from the existing resource service (`*-service.ts`).
2. Update the matching `use-*.ts` hooks — query, mutation, cache invalidation, toast feedback.
3. Wire the updated hook surface into the page or form component.

### Backend contract changed

1. Update `frontend/src/@types/models/<resource>.ts` to match new contract.
2. Update the service call payload/return type in `frontend/src/services/api/<resource>-service.ts`.
3. Fix any TypeScript errors in hooks and pages that consume the changed type.

## Validation

```bash
pnpm -C frontend typecheck
pnpm -C frontend lint
pnpm -C frontend build
```

Run the narrowest check first. If you skip a step, state it explicitly.

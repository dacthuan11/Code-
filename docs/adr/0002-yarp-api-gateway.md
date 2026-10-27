# ADR 0002 — YARP as API Gateway

**Status:** Accepted  
**Date:** 2026-10-01

## Context

All 6 services expose their own ports. Clients should not need to know service addresses, and auth should be enforced centrally.

## Decision

Use Microsoft YARP (Yet Another Reverse Proxy) as the single entry point on port 5000.

## Consequences

- Route configuration lives in `appsettings.json` under `ReverseProxy:Routes`.
- JWT validation middleware runs in the Gateway; services trust the forwarded identity.
- Adding a new service = one config block in Gateway, no code change.

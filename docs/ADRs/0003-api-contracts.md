# ADR 0003: Separate API Contracts from Domain Models

## Status
Accepted

## Context
When building an API, exposing Domain Models directly to clients leads to tight coupling. Any change in the Domain Model will break the API contracts. 

## Decision
We will separate all API requests and responses into an ApiContracts namespace within the Shared project. 
The structure will be organized by version and feature (e.g., ApiContracts/V1/Auth/Requests, ApiContracts/V1/Auth/Responses).
Domain Models will be mapped to API Contracts using AutoMapper within the Application layer.

## Consequences
- **Positive**: Strict boundary between Domain and Presentation. API remains stable even if Domain changes.
- **Negative**: Requires mapping logic and duplicate models (DTOs).

# ADR 0002: Use Result Pattern for Application Layer Responses

## Status
Accepted

## Context
Exceptions are expensive and should only be used for exceptional situations, not expected business rule violations. Relying on exceptions for control flow makes it difficult to understand the possible failure modes of an operation.

## Decision
We will use the Result Pattern (Result and Result<T>) for all Application layer handlers and services. 
A Result object will encapsulate:
- Success flag
- Domain Error object (Code, Message)
- Validation errors
- Additional metadata

Controllers will map the Result to a standardized ApiResponse object.

## Consequences
- **Positive**: Explicit error handling, avoiding hidden exceptions. Improved performance by eliminating stack trace unwinding for expected errors.
- **Negative**: Adds boilerplate code to handlers. Controllers need to map Result explicitly.

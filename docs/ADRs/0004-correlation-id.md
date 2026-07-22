# ADR 0004: Standardize Correlation ID for Request Tracing

## Status
Accepted

## Context
In a distributed system or even a monolithic system with background jobs, tracing a request across multiple components and logs is crucial for debugging.

## Decision
We will enforce a standard CorrelationId and TraceId across the application. 
- The API will accept an X-Correlation-Id header. If missing, it generates a new GUID.
- It will be stored in ICurrentRequest and injected into the Serilog LogContext.
- All standard ApiResponse objects will include the CorrelationId and TraceId.
- The TraceId uses the built-in Activity.Current.TraceId.

## Consequences
- **Positive**: Improved observability and debugging. Unified logging context.
- **Negative**: Adds slight overhead in middleware processing.

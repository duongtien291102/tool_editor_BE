# Architecture Review — Sprint 6 AI Provider Framework

## Executive Summary

The provider framework satisfies dependency inversion: policy contracts live in Application, provider mechanics live in Infrastructure, and RenderWorker depends on a factory contract rather than vendor implementations. The design supports additional providers through DI registration without a factory switch-case or render-pipeline rewrite.

## Boundary Review

| Layer | Responsibility | Sprint 6 additions |
| :--- | :--- | :--- |
| Domain | Render provider identity and render job state | No behavioral change |
| Application | Vendor-neutral contracts and configuration model | Factory, registry, selector, health, API keys, capability enum, options |
| Infrastructure | Policy implementations and mock adapters | Base provider, registry, selector, health mock, key store, seven mock routes |
| API | Composition root and configuration binding | Named provider options and DI activation |

No Auth, Project, Media, Script, or Timeline production file was changed.

## Dependency and Extensibility Review

- RenderWorker's only provider-selection dependency is `IRenderProviderFactory`.
- The factory delegates selection and contains no vendor-specific logic.
- The registry discovers `IEnumerable<IRenderProvider>` from DI.
- Concrete providers override `RenderInternalAsync`; lifecycle behavior cannot drift between vendors.
- `IProviderSelector` permits a future round-robin, weighted, cost-aware, or capability-aware policy without worker changes.
- `IProviderHealthChecker` permits a future active/cached health service without provider changes.
- `IApiKeyProvider` permits vault integrations without exposing configuration to provider implementations.

## Resilience Review

`AbstractRenderProvider` applies bounded timeout and retry options, caller cancellation, exponential retry delay, duration measurement, structured logs, and deterministic exception-to-error mapping. Disabled or capability-incompatible providers return explicit failure codes.

`FirstAvailableProviderSelector` skips disabled and unhealthy providers. It throws a typed `ProviderUnavailableException` only when no usable registration exists.

## Security Review

- No API key is embedded in source or checked-in configuration.
- Providers do not depend on `IConfiguration`.
- API keys are accessed only through `IApiKeyProvider`.
- `MemoryApiKeyProvider` is an intentional development implementation; production may replace it with a vault adapter at DI composition.

## Render Pipeline Review

The existing queue, repository, MediatR progress update, aggregate transitions, result parsing, retry-count backoff, and cancellation surface are retained. The provider resolution line is replaced by factory resolution. A confirmed progress-task wait defect was corrected with a linked cancellation token; integration coverage prevents recurrence.

## Production Readiness Assessment

The framework is production-ready as an extensibility foundation: contracts are stable, provider lifecycle concerns are centralized, configuration is validated, secrets are abstracted, health fallback is implemented, mock adapters are deterministic, and the entire regression suite passes. Real vendor clients remain intentionally out of scope.

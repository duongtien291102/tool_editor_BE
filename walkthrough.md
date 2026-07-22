# Sprint 6 — AI Provider Framework Walkthrough

## Outcome

Sprint 6 adds a vendor-neutral AI provider framework on top of the existing render abstraction. RenderWorker now resolves providers exclusively through `IRenderProviderFactory`; it has no knowledge of concrete AI vendors.

## Delivered Components

- Application contracts: factory, registry, selector, health checker, API-key provider, capabilities, and provider options.
- Shared provider lifecycle: `AbstractRenderProvider` implements logging, timing, cancellation, timeout, retry, capability validation, and exception mapping.
- Provider discovery: `RenderProviderRegistry` is populated through `IEnumerable<IRenderProvider>` from DI and rejects duplicate routes.
- Provider selection: `FirstAvailableProviderSelector` honors enabled state and health, preferring the requested route and falling back to the first available provider.
- Secret boundary: `MemoryApiKeyProvider` implements `IApiKeyProvider`; no key is hardcoded and providers never access configuration directly.
- Mock implementations: Internal, OpenAI, Runway, Kling, Veo, ElevenLabs, and Stable Video. No external AI endpoint is called.
- Capability declarations for image, video, voice, subtitle, upscale, inpainting, outpainting, and image editing.

## RenderWorker Integration

The worker receives `IRenderProviderFactory` by constructor injection and calls `GetProvider(job.Provider)`. Queue behavior, job state transitions, persistence, cancellation, and output mapping remain unchanged.

During integration testing, a synchronization defect was confirmed: the worker waited for the complete five-second simulated progress loop after a provider had already returned. A linked progress token now stops the reporter immediately on provider completion. This is the only behavioral correction in the render pipeline.

## Verification

`ProviderFrameworkTests` contains 13 new unit tests for the factory, registry, selector, health checker, abstract base, mock providers, and API-key provider.

`ProviderFrameworkIntegrationTests` contains 3 new integration tests for DI/provider resolution, unhealthy-provider fallback, and RenderWorker processing with OpenAI and Runway jobs.

Final result:

```text
Build succeeded: 0 errors, 2 reported instances of the existing NU1903 warning.
Unit tests:       185 passed, 0 failed.
Integration:      43 passed, 0 failed.
Total:            228 passed, 0 failed.
```

Auth, Project, Media, Script, Timeline, and Render regression suites all pass. No Git command was used.

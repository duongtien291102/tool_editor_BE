# AI Provider Framework

## Architecture

Sprint 6 introduces an extensible provider boundary without coupling the render pipeline to vendor implementations.

```text
RenderWorker
    -> IRenderProviderFactory
        -> IProviderSelector
            -> IRenderProviderRegistry
            -> IProviderHealthChecker
            -> IOptionsMonitor<ProviderOptions>
                -> IRenderProvider
                    -> AbstractRenderProvider
                        -> Mock vendor provider
```

The worker injects `IRenderProviderFactory` and never references OpenAI, Runway, Kling, Veo, ElevenLabs, Stable Video, or any provider class. Provider registration and selection are DI concerns.

## Factory Pattern

`IRenderProviderFactory.GetProvider(RenderProvider provider)` is the only provider-resolution operation exposed to the worker. `RenderProviderFactory` delegates policy to `IProviderSelector`; it contains no vendor branching.

## Registry Pattern

`IRenderProviderRegistry` supports runtime registration, lookup, and enumeration. `RenderProviderRegistry` is constructed from `IEnumerable<IRenderProvider>`, so a provider becomes discoverable through normal DI registration. Duplicate enum registrations fail fast to prevent ambiguous routing.

## Provider Lifecycle

Providers are stateless singletons. `AbstractRenderProvider` owns the common lifecycle:

1. Read named `ProviderOptions` through `IOptionsMonitor`.
2. Reject disabled providers or unsupported capabilities.
3. Start structured logging and timing.
4. Apply a linked cancellation token and configured timeout.
5. Execute retry with bounded exponential delay.
6. Map timeout, connectivity, cancellation, and general exceptions to `RenderResult` error codes.
7. Return a strongly typed `RenderResult`.

Concrete providers override only `RenderInternalAsync` and declare `Provider` plus `Capabilities`.

## Health Checking and Selection

`MockProviderHealthChecker` is healthy by default and supports deterministic state changes for testing. `FirstAvailableProviderSelector` first attempts the requested provider, then selects the first registered provider satisfying both conditions:

- `ProviderOptions.Enabled` is `true`.
- `IProviderHealthChecker.IsHealthy` is `true`.

When none is available, `ProviderUnavailableException` is raised. Round-robin is intentionally not implemented in Sprint 6.

## Capability Matrix

| Provider | Image | Video | Voice | Subtitle | Upscale | Inpainting | Outpainting | Image editing |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| Internal mock | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| Mock OpenAI | Yes | No | No | Yes | No | Yes | Yes | Yes |
| Mock Runway | No | Yes | No | No | Yes | Yes | No | Yes |
| Mock Kling | No | Yes | No | No | No | No | No | Yes |
| Mock Veo | No | Yes | No | No | No | No | No | No |
| Mock ElevenLabs | No | No | Yes | No | No | No | No | No |
| Mock Stable Video | No | Yes | No | No | Yes | No | No | No |

All implementations are simulations and make no external AI API calls.

## Configuration

Each provider uses a named section under `Providers:{RenderProvider}`:

```json
{
  "Providers": {
    "OpenAI": {
      "Enabled": true,
      "Timeout": 30,
      "Retry": 1,
      "BaseUrl": null,
      "ApiKey": null,
      "Model": "mock-openai"
    }
  }
}
```

Providers do not inject or read `IConfiguration`.

## Secret Management

`IApiKeyProvider` is the secret boundary. `MemoryApiKeyProvider` supports runtime set/remove operations and can source initial values from named options. No API key is hardcoded. A future vault adapter can replace this registration without modifying a provider or the worker.

For production, inject secrets through environment/user-secret configuration or replace `IApiKeyProvider` with Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault integration. Do not commit secret values to JSON.

## Extension Guide

To add a provider:

1. Add its `RenderProvider` enum value if it is a new logical route.
2. Create a sealed class deriving `AbstractRenderProvider`.
3. Declare the provider enum and immutable capability set.
4. Override only `RenderInternalAsync` for vendor-specific execution.
5. Register the class as another `IRenderProvider` in DI.
6. Add its named `Providers:{Name}` configuration.
7. Add registry, capability, base-lifecycle, and DI integration tests.

No factory, selector, registry, or RenderWorker change is required.

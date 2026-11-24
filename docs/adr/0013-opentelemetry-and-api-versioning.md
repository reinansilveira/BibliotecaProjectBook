# 0013. OpenTelemetry for observability and URL versioning for the API

## Context

Logs alone do not show where a slow request spends its time across the API, SQL Server, RabbitMQ and external HTTP calls. The published front end also needs a stable contract while the API keeps evolving.

## Decision

- The API uses OpenTelemetry for traces and metrics and sends Serilog logs through OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Docker Compose runs the free .NET Aspire dashboard to explore them; any OTLP backend (Grafana, Jaeger, Azure Monitor) can be used instead without code changes.
- Routes are versioned in the URL with `Asp.Versioning` (`/api/v1`). Responses report the supported versions.

## Consequences

- One request can be followed end to end, including SQL statements and messages published to RabbitMQ.
- Telemetry costs nothing when the endpoint is not configured, as on the free host.
- Breaking changes can be released as `v2` without breaking the front end that uses `v1`.

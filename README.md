# CspReport

A lightweight .NET 10 Minimal API service that collects Content Security Policy (CSP) violation reports using the legacy report-uri directive.

This service is designed to be:
- Simple
- Fast
- Safe
- Easy to deploy

It accepts CSP reports from modern browsers (Chrome, Edge) and logs them as structured JSON for later analysis.

---

## Features

- .NET 10 Minimal API
- CSP report-uri ingestion endpoint
- Handles real browser CSP payloads
- One JSON report per line (JSONL)
- File-based logging (easy to tail and parse)
- Payload size limits
- No MVC, no controllers, no framework bloat

---

## Endpoint

### POST /csp/report-uri

Accepts legacy CSP violation reports with:

```
Content-Type: application/csp-report
```

Payload format:

```json
{
  "csp-report": {
    "document-uri": "https://example.com",
    "blocked-uri": "https://evil.com/script.js",
    "violated-directive": "script-src 'self'",
    "effective-directive": "script-src",
    "original-policy": "default-src 'self'"
  }
}
```

Response:
- 204 No Content

---

## Configuration

Configured via appsettings.json:

```json
{
  "CspReport": {
    "LogDirectory": "Logs",
    "FileName": "csp-report-uri.jsonl",
    "MaxBodyBytes": 262144
  }
}
```

| Setting | Description |
|--------|-------------|
| LogDirectory | Directory where CSP reports are written |
| FileName | JSONL log file name |
| MaxBodyBytes | Maximum allowed request body size |

---

## Log Format

Each CSP violation is written as one JSON object per line:

```json
{
  "schema": "report-uri",
  "receivedAt": "2025-12-11T22:45:12.314Z",
  "remoteIp": "203.0.113.10",
  "userAgent": "Mozilla/5.0 ...",
  "documentUri": "https://example.com",
  "blockedUri": "https://evil.com/script.js",
  "violatedDirective": "script-src 'self'",
  "effectiveDirective": "script-src",
  "originalPolicy": "default-src 'self'",
  "raw": { ... }
}
```

---

## Running Locally

### Prerequisites
- .NET 10 SDK

### Run

```bash
dotnet run
```

The service will listen on a local port, for example:

```
http://localhost:5048
```

Optional health check:

```
GET /
Response: CspReport is running
```

---

## Testing

### PowerShell test

```powershell
Invoke-RestMethod `
  -Uri http://localhost:5048/csp/report-uri `
  -Method Post `
  -ContentType "application/csp-report" `
  -Body '{ "csp-report": { "document-uri": "test" } }' `
  -SkipHttpErrorCheck
```

Expected result:
- HTTP 204
- New entry in Logs/csp-report-uri.jsonl

---

## Using with CSP

Add this header to the site you want to monitor:

```http
Content-Security-Policy:
  default-src 'self';
  script-src 'self';
  report-uri https://YOURDOMAIN/csp/report-uri;
```

Note:
Browsers will not send CSP reports from HTTPS pages to HTTP endpoints.
Run this service over HTTPS in production.

---

## Project Structure

```text
CspReport/
|-- Program.cs
|-- appsettings.json
|-- Endpoints/
|   |-- CspReportEndpoints.cs
|-- Models/
|   |-- CspReportEnvelope.cs
|-- Options/
|   |-- CspReportOptions.cs
|-- Sinks/
|   |-- ICspReportSink.cs
|   |-- FileCspReportSink.cs
```

---

## Security Notes

- CSP reports may contain URLs, user agents, and IP addresses.
- Log files are not committed to source control.
- Consider rate limiting and HTTPS when deploying publicly.

---

## Roadmap / Ideas

- SQLite or PostgreSQL sink
- Report-To / Reporting API support
- Rate limiting
- Serilog with rolling logs
- Docker image
- Health checks and metrics

---

## License

MIT (or your preferred license)

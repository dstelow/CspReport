# GitHub Copilot Instructions for CspReport

## C# Code Style

### Formatting
- Use file-scoped namespaces
- Use `var` only when the type is obvious from the right side
- Prefer explicit types for primitive values and interfaces
- Use expression-bodied members for simple one-liners
- Always use braces for control statements, even single lines

### Code Quality
- Always check for null before dereferencing
- Use nullable reference types correctly (`?` annotations)
- Prefer `readonly` for fields that don't change after construction
- Use `sealed` for classes not intended for inheritance
- Implement proper async/await patterns (no `.Result` or `.Wait()`)

### .NET 10 Best Practices
- Use minimal APIs pattern (no controllers)
- Prefer dependency injection over static dependencies
- Use `IOptions<T>` for configuration
- Use `CancellationToken` parameters in async methods
- Leverage built-in JSON serialization with `System.Text.Json`

### Error Handling
- Return appropriate HTTP status codes (200, 204, 400, 413, 500)
- Validate input parameters at endpoint level
- Use try-catch only where necessary, prefer validation
- Log exceptions with proper context

### Thread Safety
- Use `SemaphoreSlim` for async synchronization
- Never use `lock` in async code
- Consider concurrent access when reading/writing files
- Document thread-safety guarantees in interfaces

### Testing Considerations
- Keep business logic separate from HTTP concerns
- Use interfaces for testability (e.g., `ICspReportSink`)
- Make methods easy to unit test
- Avoid static state

## Project-Specific Rules

### Endpoint Design
- All CSP endpoints should be prefixed with `/csp/`
- POST endpoints for ingestion should return 204 No Content
- GET endpoints should return 200 OK with JSON payload
- Include appropriate validation and size limits

### Logging
- Use JSONL format (one JSON object per line)
- Include timestamp, IP, User-Agent in all report envelopes
- Never log sensitive user data beyond what's in CSP reports

### Configuration
- All configurable values should go in `CspReportOptions`
- Use reasonable defaults
- Validate configuration at startup if critical

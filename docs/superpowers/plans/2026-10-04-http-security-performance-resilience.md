# HTTP Security, Performance, and Resilience Plan

> Historical record: completion markers and validation counts below describe the candidate tested on 2026-10-04. They are not fresh validation of the current working tree; current status must be established from the present source and a newly run applicable gate.

Goal: implement the approved HTTP module plan in this conversation.

## Tasks

- [x] Fix shared handler mutations, client lifetime, per-request timeout, and response disposal.
- [x] Add streaming upload/download paths and configurable response limits.
- [x] Add opt-in safe-method retries with replay checks and Retry-After.
- [x] Fix Web local-IP, host, file-write, streaming-download, and bounded-body behaviors.
- [x] Add focused unit/integration tests, migration notes, and comparable performance evidence.

## Follow-up audit closure

- [x] Recheck retry eligibility after `OnSendBefore` changes the actual HTTP method; reject method changes between attempts.
- [x] Link caller cancellation and `RequestAborted` in server-side streaming and bounded body reads.
- [x] Bound custom response callbacks before invocation and reject direct response-stream returns. Keep the response alive until callbacks finish; pass cancellation to the new callback overload.
- [x] Dispose superseded upload streams and owned streams rejected before sending.
- [x] Route ordinary result and byte retrieval through the existing protected virtual response methods.
- [x] Replay seekable caller upload streams; reject non-seekable streams when retries are enabled.
- [x] Serialize `application/*+json` request bodies with the configured media type.
- [x] Add overridable retry clock, delay, and jitter hooks for deterministic tests.
- [x] Cover retry method selection, replay, Retry-After date/delta, retry count, callback size limits, stream ownership, request abort, named DI factory reuse, and failed file replacement.
- [x] Place real file replacement tests in the HTTP integration project; update API and migration documentation.
- [x] Refresh same-host candidate upload and JSON performance measurements with the unchanged benchmark harness.

## Final TODO closure

- [x] Test callback cancellation and response lifetime, independent stream returns, infinite timeout retries, and parameterized vendor JSON media types.
- [x] Test deterministic jitter and capped backoff, all six retryable status codes, Retry-After beyond the total timeout budget, and a shared timeout across attempts.
- [x] Test empty and non-seekable short-read response streams, concurrent requests with different timeouts, and unchanged TLS/cookie handler state after rejected options.
- [x] Test cancellation during client file download and preservation of the existing target; move two real file-system Web tests to the HTTP integration project without duplicate tests.
- [x] Update Chinese API contracts, migration notes, and the benchmark report for the measured Release assembly.

Validation: HTTP unit tests pass 1538/1538 on each of netcoreapp3.1, net5.0, net6.0, net7.0, and net8.0. HTTP integration tests pass 49/49 on each of netcoreapp3.1, net5.0, net6.0, and net7.0. The benchmark report identifies its measured Release assembly separately.

Remaining validation scope: local Handler benchmarks do not establish real network download throughput. The legacy `OnSendAfter(Func<HttpResponseMessage, Task<TResult>>)` delegate has no cancellation-token argument; the library waits for that callback to finish to keep the response valid, so a callback that blocks indefinitely must arrange its own cancellation. External CI and production network behavior are outside this local verification.

Constraints: keep current target frameworks and dependencies, preserve UTF-8, do not commit.

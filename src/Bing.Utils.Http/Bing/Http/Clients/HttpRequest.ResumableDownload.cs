using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bing.Http.Clients;

public partial class HttpRequest<TResult> where TResult : class
{
    /// <inheritdoc />
    public void SetResumableDownloadValidator(Func<Stream, Task> validator)
    {
        if (validator == null) throw new ArgumentNullException(nameof(validator));
        _resumableDownloadValidator = (stream, _) => validator(stream);
    }

    /// <summary>
    /// 设置断点下载完成后的正文校验器，并向校验器传递当前操作的取消令牌。
    /// </summary>
    /// <param name="validator">接收临时文件流和取消令牌的校验器。</param>
    public void SetResumableDownloadValidator(Func<Stream, CancellationToken, Task> validator)
    {
        _resumableDownloadValidator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <inheritdoc />
    public Task<HttpDownloadResult> DownloadFileResumableWithResultAsync(
        string filePath, CancellationToken cancellationToken = default)
    {
        if (SendAfterAction != null || SendAfterWithCancellationAction != null)
            return FailPreflightAsync<HttpDownloadResult>(
                new NotSupportedException(
                    "Resumable downloads cannot be combined with OnSendAfter; remove the custom response handler."));
        if (!HttpMethod.Get.Equals(_httpMethod))
            throw new NotSupportedException("Resumable downloads require the GET method.");

        return _completionObserver == null
            ? DownloadFileResumableCoreAsync(filePath, cancellationToken, null)
            : ObserveOperationAsync(state =>
                DownloadFileResumableCoreAsync(filePath, cancellationToken, state));
    }

    /// <summary>

    /// 执行带状态文件和验证器的断点续传下载。

    /// </summary>
    private async Task<HttpDownloadResult> DownloadFileResumableCoreAsync(
        string filePath, CancellationToken cancellationToken, ObservationState observation)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            var error = new ArgumentException("File path is required.", nameof(filePath));
            DisposeOwnedUploadStreams(error);
            throw error;
        }

        string fullPath;
        try { fullPath = Path.GetFullPath(filePath); }
        catch (Exception ex)
        {
            DisposeOwnedUploadStreams(ex);
            throw;
        }

        using var downloadLock = AcquireResumeLock(fullPath);
        var partialPath = fullPath + ".binghttp.part";
        var statePath = fullPath + ".binghttp.resume.json";
        var candidate = LoadResumeState(partialPath, statePath);
        ResumeDownloadState activeState = null;
        var completedByRange = false;
        var committed = false;
        Exception fileError = null;
        var requestExecuted = false;

        try
        {
            HttpClient requestClient = null;
            Action<HttpRequestMessage, int, HttpClient> prepareMessage = (message, attempt, client) =>
            {
                requestClient = client;
                message.Headers.Remove("Range");
                message.Headers.Remove("If-Range");
                message.Headers.Remove("Accept-Encoding");
                message.Headers.TryAddWithoutValidation("Accept-Encoding", "identity");

                if (attempt == 1)
                {
                    var identity = BuildRequestIdentity(message, client);
                    var requestFingerprint = BuildRequestFingerprint(message, client);
                    if (!IsResumeCandidate(candidate, partialPath, identity, requestFingerprint))
                    {
                        candidate = null;
                        DeleteResumeFiles(partialPath, statePath);
                    }
                    else
                    {
                        candidate.Identity = identity;
                        candidate.RequestFingerprint = requestFingerprint;
                    }
                }

                if (candidate != null)
                {
                    message.Headers.TryAddWithoutValidation(
                        "Range", "bytes=" + candidate.BytesWritten.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-");
                    message.Headers.TryAddWithoutValidation("If-Range", GetIfRange(candidate));
                }
            };

            requestExecuted = true;
            var result = await ExecuteAsync(async (response, token) =>
            {
                object content = null;
                Exception processingError = null;
                try
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        if (candidate != null && response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                        {
                            ValidateResumeResponseIdentity(response, requestClient, candidate);

                            if (IsCompletedRangeResponse(response, candidate))
                            {
                                activeState = candidate;
                                completedByRange = true;
                                return new HttpDownloadResult(response, candidate.BytesWritten, false);
                            }
                        }

                        content = await ReadResponseBytesAsync(response, token).ConfigureAwait(false);
                        FailHandler(response, content);
                        return new HttpDownloadResult(response, candidate?.BytesWritten ?? 0, false);
                    }

                    var offset = 0L;
                    long? expectedPartialLength = null;
                    if (candidate != null)
                    {
                        ValidateResumeResponseIdentity(response, requestClient, candidate);
                        if (response.StatusCode == HttpStatusCode.PartialContent)
                        {
                            expectedPartialLength = ValidatePartialResponse(response, candidate);
                            offset = candidate.BytesWritten;
                        }
                        else if (response.StatusCode == HttpStatusCode.OK)
                        {
                            candidate = null;
                        }
                        else
                        {
                            throw new HttpRequestException(
                                "A resumable download requires a 200 or 206 response.");
                        }
                    }
                    else if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpRequestException(
                            "A fresh download requires a 200 response.");
                    }

                    activeState = CreateResumeState(response, identity: null, offset, candidate);

                    activeState.RequestFingerprint = BuildRequestFingerprint(response.RequestMessage, requestClient);
                    activeState.Identity = BuildRequestIdentity(response.RequestMessage, requestClient);
                    if (!HasValidator(activeState))
                        activeState = null;
                    else
                        SaveResumeState(statePath, activeState);

                    var mode = offset == 0 ? FileMode.Create : FileMode.OpenOrCreate;
                    using var stream = new FileStream(
                        partialPath, mode, FileAccess.Write, FileShare.None, 81920, true);
                    if (offset != 0)
                    {
                        if (stream.Length != offset)
                            throw new IOException("The partial download length no longer matches its resume state.");
                        stream.Position = offset;
                    }

                    var progress = _downloadProgressObserver == null ? null :
                        new DownloadProgressState(_downloadProgressObserver, _progressMinimumBytes,
                            _progressMinimumInterval);
                    var copied = await CopyResponseAsync(response, stream, token, progress, offset,
                        value =>
                        {
                            if (activeState != null)
                            {
                                stream.Flush(true);
                                activeState.BytesWritten = offset + value;
                                SaveResumeState(statePath, activeState);
                            }
                        }).ConfigureAwait(false);
                    if (expectedPartialLength.HasValue && copied != expectedPartialLength.Value)
                        throw new HttpRequestException("The partial response body length does not match Content-Range.");

                    var total = offset + copied;
                    if (activeState?.TotalLength is long expected && expected != total)
                        throw new HttpRequestException("The resumed response length does not match Content-Range.");
                    if (activeState != null)
                        activeState.BytesWritten = total;
                    return new HttpDownloadResult(response, total, false);
                }
                catch (Exception ex)
                {
                    processingError = ex;
                    throw;
                }
                finally
                {
                    CompleteHandlerPreservingException(response, content, processingError);
                }
            }, cancellationToken, observation, nested: true, allowRetries: true, prepareMessage: prepareMessage)
                .ConfigureAwait(false);

            if (result == null || (!result.IsSuccessStatusCode && !completedByRange))
                return result;

            cancellationToken.ThrowIfCancellationRequested();
            if (_resumableDownloadValidator != null)
            {
                using var validationStream = new FileStream(
                    partialPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
                await _resumableDownloadValidator(validationStream, cancellationToken).ConfigureAwait(false);
            }
            if (File.Exists(fullPath))
                File.Replace(partialPath, fullPath, null);
            else
                File.Move(partialPath, fullPath);
            committed = true;
            DeleteFileIfExists(statePath);
            return result.WithFileSaved();
        }
        catch (Exception ex)
        {
            fileError = ex;
            throw;
        }
        finally
        {
            Exception cleanupError = null;
            if (!committed)
            {
                if (activeState != null && activeState.BytesWritten > 0 && HasValidator(activeState))
                {
                    try { SaveResumeState(statePath, activeState); }
                    catch (Exception ex) { cleanupError = ex; }
                }
                else if (activeState == null && candidate == null)
                {
                    cleanupError = CaptureDelete(partialPath, cleanupError);
                    cleanupError = CaptureDelete(statePath, cleanupError);
                }
            }
            else
            {
                cleanupError = CaptureDelete(statePath, cleanupError);
            }

            if (!requestExecuted)
                cleanupError = DisposeUploadStreams(cleanupError);
            if (fileError == null && cleanupError != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupError).Throw();
        }
    }

    /// <summary>

    /// 获取目标文件对应的断点续传锁。

    /// </summary>
    private static FileStream AcquireResumeLock(string fullPath)
    {
        return new FileStream(
            fullPath + ".binghttp.lock",
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None,
            4096,
            FileOptions.SequentialScan);
    }
    /// <summary>
    /// 生成用于匹配恢复状态的请求身份信息。
    /// </summary>
    private static string BuildRequestIdentity(HttpRequestMessage message, HttpClient client)
    {
        if (message?.RequestUri == null) return null;
        var uri = message.RequestUri;
        if (!uri.IsAbsoluteUri)
        {
            var baseUri = client?.BaseAddress;
            if (baseUri == null || !baseUri.IsAbsoluteUri) return null;
            uri = new Uri(baseUri, uri);
        }
        return message.Method.Method + "|" + uri.AbsoluteUri;
    }

    /// <summary>

    /// 生成包含请求和客户端配置的恢复指纹。

    /// </summary>
    private static string BuildRequestFingerprint(HttpRequestMessage message, HttpClient client)
    {
        var values = new System.Collections.Generic.List<string>();
        AddFingerprintHeaders(values, client?.DefaultRequestHeaders);
        AddFingerprintHeaders(values, message?.Headers);
        values.Sort(StringComparer.Ordinal);
        using (var sha = SHA256.Create())
        {
            var payload = System.Text.Encoding.UTF8.GetBytes(string.Join("\n", values));
            return BitConverter.ToString(sha.ComputeHash(payload))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
    }

    /// <summary>

    /// 将影响恢复身份的请求头加入指纹。

    /// </summary>
    private static void AddFingerprintHeaders(
        System.Collections.Generic.List<string> values,
        System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,
            System.Collections.Generic.IEnumerable<string>>> headers)
    {
        if (headers == null) return;
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, "Range", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(header.Key, "If-Range", StringComparison.OrdinalIgnoreCase))
                continue;
            var headerValues = new System.Collections.Generic.List<string>(header.Value);
            headerValues.Sort(StringComparer.Ordinal);
            values.Add(header.Key.ToLowerInvariant() + "=" + string.Join("\u001f", headerValues));
        }
    }

    /// <summary>

    /// 确认恢复响应与已保存的请求身份一致。

    /// </summary>
    private static void ValidateResumeResponseIdentity(
        HttpResponseMessage response, HttpClient client, ResumeDownloadState state)
    {
        if (!string.Equals(state.Identity,
                BuildRequestIdentity(response.RequestMessage, client), StringComparison.Ordinal))
            throw new HttpRequestException("The response URI changed during the partial download.");
        if (!string.IsNullOrWhiteSpace(state.RequestFingerprint) &&
            !string.Equals(state.RequestFingerprint,
                BuildRequestFingerprint(response.RequestMessage, client), StringComparison.Ordinal))
            throw new HttpRequestException("The request headers changed during the partial download.");
    }
    /// <summary>
    /// 根据已保存的验证器生成 If-Range 值。
    /// </summary>
    private static string GetIfRange(ResumeDownloadState state) =>
        !string.IsNullOrWhiteSpace(state.ETag) &&
        !state.ETag.StartsWith("W/", StringComparison.OrdinalIgnoreCase)
            ? state.ETag : state.LastModified;

    /// <summary>

    /// 判断恢复状态是否包含有效的响应验证器。

    /// </summary>
    private static bool HasValidator(ResumeDownloadState state) =>
        state != null &&
        (!string.IsNullOrWhiteSpace(state.ETag) && !state.ETag.StartsWith("W/", StringComparison.OrdinalIgnoreCase) ||
         !string.IsNullOrWhiteSpace(state.LastModified));

    /// <summary>

    /// 判断恢复状态是否包含强 ETag 验证器。

    /// </summary>
    private static bool HasStrongETag(ResumeDownloadState state) =>
        !string.IsNullOrWhiteSpace(state?.ETag) &&
        !state.ETag.StartsWith("W/", StringComparison.OrdinalIgnoreCase);

    /// <summary>

    /// 判断响应是否支持继续追加到已有部分文件。

    /// </summary>
    private static bool IsResumeCandidate(
        ResumeDownloadState state, string partialPath, string identity, string requestFingerprint)
    {
        if (state == null || state.BytesWritten <= 0 || string.IsNullOrWhiteSpace(identity) ||
            !string.Equals(state.Identity, identity, StringComparison.Ordinal) ||
            !string.IsNullOrWhiteSpace(state.RequestFingerprint) &&
            !string.Equals(state.RequestFingerprint, requestFingerprint, StringComparison.Ordinal) ||
            !HasValidator(state))
            return false;
        try { return File.Exists(partialPath) && new FileInfo(partialPath).Length == state.BytesWritten; }
        catch { return false; }
    }

    /// <summary>

    /// 加载并校验已有的断点续传状态。

    /// </summary>
    private static ResumeDownloadState LoadResumeState(string partialPath, string statePath)
    {
        try
        {
            if (!File.Exists(partialPath) || !File.Exists(statePath) ||
                new FileInfo(statePath).Length > 64 * 1024)
            {
                DeleteResumeFiles(partialPath, statePath);
                return null;
            }

            var state = JsonSerializer.Deserialize<ResumeDownloadState>(
                File.ReadAllText(statePath, System.Text.Encoding.UTF8));
            if (state == null || state.SchemaVersion < 0 || state.SchemaVersion > ResumeStateSchemaVersion ||
                state.BytesWritten <= 0 || !HasValidator(state))
            {
                DeleteResumeFiles(partialPath, statePath);
                return null;
            }
            if (state.SchemaVersion == 0)
                state.SchemaVersion = ResumeStateSchemaVersion;
            return state;
        }
        catch
        {
            DeleteResumeFiles(partialPath, statePath);
            return null;
        }
    }

    /// <summary>

    /// 根据响应信息创建新的断点续传状态。

    /// </summary>
    private static ResumeDownloadState CreateResumeState(
        HttpResponseMessage response, string identity, long offset, ResumeDownloadState previous)
    {
        var state = new ResumeDownloadState
        {
            SchemaVersion = ResumeStateSchemaVersion,
            Identity = identity,
            RequestFingerprint = previous?.RequestFingerprint,
            ContentEncoding = GetContentEncoding(response),
            BytesWritten = offset,
            ETag = GetHeader(response, "ETag") ?? previous?.ETag,
            LastModified = GetHeader(response, "Last-Modified") ?? previous?.LastModified
        };
        var rangeLength = response.Content?.Headers.ContentRange?.Length;
        state.TotalLength = rangeLength ?? (response.Content?.Headers.ContentLength.HasValue == true
            ? offset + response.Content.Headers.ContentLength.Value
            : previous?.TotalLength);
        return state;
    }

    /// <summary>

    /// 读取响应的内容编码头。

    /// </summary>
    private static string GetContentEncoding(HttpResponseMessage response) =>
        GetHeader(response, "Content-Encoding") ?? "identity";

    /// <summary>

    /// 从响应中读取指定名称的首个头值。

    /// </summary>
    private static string GetHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
            return values.FirstOrDefault();
        return response.Content?.Headers.TryGetValues(name, out values) == true
            ? values.FirstOrDefault() : null;
    }

    /// <summary>

    /// 校验部分响应与已保存的续传状态匹配。

    /// </summary>
    private static long ValidatePartialResponse(
        HttpResponseMessage response, ResumeDownloadState state)
    {
        var range = response.Content?.Headers.ContentRange;
        if (range == null ||
            !string.Equals(range.Unit, "bytes", StringComparison.OrdinalIgnoreCase) ||
            !range.From.HasValue ||
            !range.To.HasValue ||
            !range.Length.HasValue ||
            range.From.Value != state.BytesWritten ||
            range.To.Value < range.From.Value ||
            range.To.Value >= range.Length.Value)
            throw new HttpRequestException("The server returned an invalid Content-Range for the partial download.");

        var expectedLength = range.To.Value - range.From.Value + 1;
        if (response.Content.Headers.ContentLength.HasValue &&
            response.Content.Headers.ContentLength.Value != expectedLength)
            throw new HttpRequestException("The partial response length does not match Content-Range.");

        var responseEncoding = GetContentEncoding(response);
        if (!string.IsNullOrWhiteSpace(state.ContentEncoding) &&
            !string.Equals(state.ContentEncoding, responseEncoding, StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException("The response content encoding changed during the partial download.");

        var responseEtag = GetHeader(response, "ETag");
        var responseLastModified = GetHeader(response, "Last-Modified");
        if (HasStrongETag(state) &&
            !string.Equals(state.ETag, responseEtag, StringComparison.Ordinal))
            throw new HttpRequestException("The resource ETag changed during the partial download.");
        if (!HasStrongETag(state) &&
            !string.IsNullOrWhiteSpace(state.LastModified) &&
            !string.Equals(state.LastModified, responseLastModified, StringComparison.Ordinal))
            throw new HttpRequestException("The resource Last-Modified value changed during the partial download.");

        return expectedLength;
    }

    /// <summary>

    /// 判断响应是否已覆盖完整的目标字节范围。

    /// </summary>
    private static bool IsCompletedRangeResponse(
        HttpResponseMessage response, ResumeDownloadState state)
    {
        var range = response.Content?.Headers.ContentRange;
        if (range == null ||
            !string.Equals(range.Unit, "bytes", StringComparison.OrdinalIgnoreCase) ||
            range.From.HasValue ||
            range.To.HasValue ||
            !range.Length.HasValue ||
            range.Length.Value != state.BytesWritten)
            return false;
        var responseEncoding = GetContentEncoding(response);
        if (!string.IsNullOrWhiteSpace(state.ContentEncoding) &&
            !string.Equals(state.ContentEncoding, responseEncoding, StringComparison.OrdinalIgnoreCase))
            return false;

        var responseEtag = GetHeader(response, "ETag");
        var responseLastModified = GetHeader(response, "Last-Modified");
        return HasStrongETag(state)
            ? string.Equals(state.ETag, responseEtag, StringComparison.Ordinal)
            : !string.IsNullOrWhiteSpace(state.LastModified) &&
              string.Equals(state.LastModified, responseLastModified, StringComparison.Ordinal);
    }

    /// <summary>

    /// 将断点续传状态原子写入状态文件。

    /// </summary>
    private static void SaveResumeState(string statePath, ResumeDownloadState state)
    {
        var temporaryPath = statePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            {
                writer.Write(JsonSerializer.Serialize(state));
                writer.Flush();
                stream.Flush(true);
            }

            if (File.Exists(statePath))
                File.Replace(temporaryPath, statePath, null);
            else
                File.Move(temporaryPath, statePath);
        }
        finally
        {
            DeleteFileIfExists(temporaryPath);
        }
    }

    /// <summary>

    /// 删除断点续传使用的部分文件和状态文件。

    /// </summary>
    private static void DeleteResumeFiles(string partialPath, string statePath)
    {
        DeleteFileIfExists(partialPath);
        DeleteFileIfExists(statePath);
    }

    /// <summary>

    /// 尝试删除文件并保留已有的首个异常。

    /// </summary>
    private static Exception CaptureDelete(string path, Exception firstError)
    {
        try { DeleteFileIfExists(path); }
        catch (Exception ex) { firstError ??= ex; }
        return firstError;
    }

    /// <summary>

    /// 存在时删除指定文件。

    /// </summary>
    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}

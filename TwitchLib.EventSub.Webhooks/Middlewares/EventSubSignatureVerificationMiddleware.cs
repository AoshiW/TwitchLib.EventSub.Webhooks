using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.Models;
using TwitchLib.EventSub.Webhooks.Extensions;

#pragma warning disable 1591
namespace TwitchLib.EventSub.Webhooks.Middlewares
{
    // TODO rename to   EventSubMiddleware (or something else?)

    public class EventSubSignatureVerificationMiddleware
    {
        private readonly ILogger<EventSubSignatureVerificationMiddleware> _logger;
        private readonly TwitchLibEventSubOptions _options;
        private readonly IEventSubWebhooks _eventSubWebhooks;

        public EventSubSignatureVerificationMiddleware(RequestDelegate next, ILogger<EventSubSignatureVerificationMiddleware> logger, IOptions<TwitchLibEventSubOptions> options, IEventSubWebhooks eventSubWebhooks)
        {
            _ = next; // we don't need it but it's required for middleware
                     // (if we got this far it has to be some ES event otherwise we have to return an error)
            _logger = logger;
            _eventSubWebhooks = eventSubWebhooks;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var metadata = WebhookEventSubMetadata.CreateMetadata(context.Request.Headers);
            var body = await ReadRequestBodyContentAsync(context.Request);

            //  TODO Q: Move validation logic to service?
            if (!await IsValidEventSubRequest(metadata, body))
            {
                await WriteResponseAsync(context, 403, "text/plain", "Invalid Signature");
                return;
            }

            switch (metadata.MessageType)
            {
                case "webhook_callback_verification":
                    var json = await JsonDocument.ParseAsync(context.Request.Body);
                    await WriteResponseAsync(context, 200, "text/plain", json.RootElement.GetProperty("challenge"u8).GetString()!);
                    return;
                case "notification":
                    await _eventSubWebhooks.ProcessNotificationAsync(metadata, body);
                    await WriteResponseAsync(context, 200, "text/plain", "Thanks for the heads up Jordan");
                    return;
                case "revocation":
                    await _eventSubWebhooks.ProcessRevocationAsync(metadata, body);
                    await WriteResponseAsync(context, 200, "text/plain", "Thanks for the heads up Jordan");
                    return;
                default:
                    await WriteResponseAsync(context, 400, "text/plain", $"Unknown EventSub message type: {metadata.MessageType}");
                    return;
            }
        }

        private async Task<bool> IsValidEventSubRequest(WebhookEventSubMetadata metadata, ReadOnlyMemory<byte> body)
        {
            try
            {
                return IsSignatureValid(metadata.MessageSignature, metadata.MessageId, metadata.MessageTimestamp, body.Span, _options.SecretBytes!);
            }
            catch (Exception ex)
            {
                _logger.LogSignatureVerificationException(ex.Message);
                return false;
            }
        }

        internal static bool IsSignatureValid(ReadOnlySpan<char> messageSignature, ReadOnlySpan<char> messageId, ReadOnlySpan<char> messageTimestamp, ReadOnlySpan<byte> messageBody, ReadOnlySpan<byte> secret)
        {
            var messageCharSpan = GetSpanFromArrayPool<char>(messageId.Length + messageTimestamp.Length, out var messageCharArray);
            messageCharSpan.TryWrite($"{messageId}{messageTimestamp}", out _);

            var messageByteSpan = GetSpanFromArrayPool<byte>(Encoding.UTF8.GetByteCount(messageCharSpan) + messageBody.Length, out var messageByteArray);
            var written = Encoding.UTF8.GetBytes(messageCharSpan, messageByteSpan);
            messageBody.CopyTo(messageByteSpan.Slice(written));
            ArrayPool<char>.Shared.Return(messageCharArray);

            Span<byte> computedSignature = stackalloc byte[HMACSHA256.HashSizeInBytes];
            HMACSHA256.HashData(secret, messageByteSpan, computedSignature);
            ArrayPool<byte>.Shared.Return(messageByteArray);

            ReadOnlySpan<char> sha256Prefix = "sha256=";
            if (messageSignature.StartsWith(sha256Prefix))
                messageSignature = messageSignature.Slice(sha256Prefix.Length);
#if NET9_0_OR_GREATER
            Span<byte> providedSignature = stackalloc byte[HMACSHA256.HashSizeInBytes];
            Convert.FromHexString(messageSignature, providedSignature, out _, out _);
#else
            var providedSignature = Convert.FromHexString(messageSignature).AsSpan();
#endif

            return providedSignature.SequenceEqual(computedSignature);
        }

        static Span<T> GetSpanFromArrayPool<T>(int length, out T[] array)
        {
            array = ArrayPool<T>.Shared.Rent(length);
            return array.AsSpan(0, length);
        }

        private static async Task<ReadOnlyMemory<byte>> ReadRequestBodyContentAsync(HttpRequest request)
        {
            using var memoryStream = new MemoryStream();
            await request.Body.CopyToAsync(memoryStream);
            request.Body.Seek(0L, SeekOrigin.Begin);

            return memoryStream.GetBuffer().AsMemory(0, (int)memoryStream.Position);
        }

        private static async Task WriteResponseAsync(HttpContext context, int statusCode, string contentType, string responseBody)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = contentType;
            await context.Response.WriteAsync(responseBody);
        }
    }
}
#pragma warning restore 1591
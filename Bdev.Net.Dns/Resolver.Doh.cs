using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Bdev.Net.Dns.Exceptions;

namespace Bdev.Net.Dns
{
    public partial class Resolver
    {
        /// <summary>
        ///     Perform a DNS-over-HTTPS (DoH) lookup against the provided DoH endpoint.
        ///     This sends the DNS wire-format message as application/dns-message in a POST request
        ///     (RFC 8484) and returns the parsed Response.
        /// </summary>
        /// <param name="dohEndpoint">The HTTPS URI of the DoH server (e.g. https://dns.google/dns-query)</param>
        /// <param name="request">The logical DNS request to send</param>
        /// <param name="timeoutMs">Request timeout in milliseconds</param>
        /// <returns>A Response constructed from the returned DNS wire-format bytes</returns>
        public static Response LookupOverHttps(Uri dohEndpoint, Request request, int timeoutMs = 5000)
        {
            if (dohEndpoint == null) throw new ArgumentNullException(nameof(dohEndpoint));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestMessage = request.GetMessage();

            // mark this request with a unique id similar to UDP behaviour
            var uniqueId = DateTime.Now.Ticks;
            unchecked
            {
                requestMessage[0] = (byte) (uniqueId >> 8);
                requestMessage[1] = (byte) uniqueId;
            }

            var responseBytes = DohTransfer(dohEndpoint, requestMessage, timeoutMs);

            var response = new Response(responseBytes);
            return response;
        }

        /// <summary>
        /// Convenience overload accepting a string URL
        /// </summary>
        public static Response LookupOverHttps(string dohEndpoint, Request request, int timeoutMs = 5000)
        {
            if (string.IsNullOrWhiteSpace(dohEndpoint)) throw new ArgumentNullException(nameof(dohEndpoint));
            return LookupOverHttps(new Uri(dohEndpoint), request, timeoutMs);
        }

        private static byte[] DohTransfer(Uri dohEndpoint, byte[] requestMessage, int timeoutMs = 5000)
        {
            try
            {
                return DohTransferAsync(dohEndpoint, requestMessage, timeoutMs).GetAwaiter().GetResult();
            }
            catch (AggregateException ae) when (ae.InnerException is NoResponseException)
            {
                throw ae.InnerException;
            }
            catch (AggregateException ae)
            {
                throw new NoResponseException(ae.InnerException ?? ae);
            }
        }

        private static async Task<byte[]> DohTransferAsync(Uri dohEndpoint, byte[] requestMessage, int timeoutMs = 5000)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };

            var req = new HttpRequestMessage(HttpMethod.Post, dohEndpoint)
            {
                Version = new Version(2, 0)
            };

            req.Content = new ByteArrayContent(requestMessage);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");

            HttpResponseMessage resp;
            try
            {
                resp = await http.SendAsync(req).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                // network-level failures map to no response
                throw new NoResponseException(ex);
            }
            catch (TaskCanceledException ex)
            {
                // timeout
                throw new NoResponseException(ex);
            }

            if (!resp.IsSuccessStatusCode)
            {
                // map 4xx/5xx to no response for consistency with existing methods
                throw new NoResponseException($"DoH server returned {(int)resp.StatusCode} {resp.ReasonPhrase}");
            }

            // optional: enforce content-type if present
            if (resp.Content.Headers.ContentType != null &&
                !string.Equals(resp.Content.Headers.ContentType.MediaType, "application/dns-message", StringComparison.OrdinalIgnoreCase))
            {
                throw new NoResponseException("DoH server returned unexpected content-type");
            }

            try
            {
                var bytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                return bytes;
            }
            catch (Exception ex)
            {
                throw new NoResponseException(ex);
            }
        }
    }
}

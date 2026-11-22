using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Rezervise.Api.Logging
{
    /// <summary>
    /// Removes sensitive values from log payloads before they leave the process.
    /// Masking happens on the serialized object graph so nested objects, arrays and
    /// dictionaries are all covered regardless of the original shape.
    /// The class is fail-safe: serialization problems degrade to a placeholder instead of throwing.
    /// </summary>
    public static class SensitiveDataMasker
    {
        /// <summary>Replacement value written in place of a sensitive field.</summary>
        public const string MaskedValue = "***MASKED***";

        /// <summary>Default byte budget for a serialized log body (10 KB).</summary>
        public const int DefaultMaxBytes = 10 * 1024;

        /// <summary>Marker appended when a payload is shortened.</summary>
        public const string TruncationMarker = "...[truncated]";

        /// <summary>Returned when a value genuinely cannot be serialized.</summary>
        public const string SerializationFailedPlaceholder = "<serialization_failed>";

        /// <summary>
        /// Returned for malformed or non-JSON text: raw bytes may contain credentials
        /// and must never be echoed back, not even truncated.
        /// </summary>
        public const string InvalidJsonPlaceholder = "<invalid_json_body>";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            MaxDepth = 16
        };

        /// <summary>
        /// Masks sensitive fields inside an object graph.
        /// </summary>
        /// <param name="value">Value to inspect; any serializable object is accepted.</param>
        /// <param name="sensitiveFields">
        /// Field names to mask. When null or empty, <see cref="GelfOptions.DefaultSensitiveFields"/> is used.
        /// </param>
        /// <returns>A masked <see cref="JsonNode"/> representation, or null when the input is null.</returns>
        public static JsonNode? Mask(object? value, IReadOnlyCollection<string>? sensitiveFields = null)
        {
            if (value is null)
                return null;

            try
            {
                var node = JsonSerializer.SerializeToNode(value, value.GetType(), SerializerOptions);

                MaskNode(node, BuildFieldSet(sensitiveFields));

                return node;
            }
            catch
            {
                // Masking must never surface an exception to the caller; the raw value is dropped.
                return null;
            }
        }

        /// <summary>
        /// Masks sensitive fields and serializes the result, enforcing a byte budget.
        /// </summary>
        /// <param name="value">Value to inspect.</param>
        /// <param name="sensitiveFields">Field names to mask; defaults are used when null or empty.</param>
        /// <param name="maxBytes">Maximum accepted size of the serialized result.</param>
        /// <returns>The masked JSON string, a truncation marker, or a failure placeholder; null when the input is null.</returns>
        public static string? MaskAndSerialize(
            object? value,
            IReadOnlyCollection<string>? sensitiveFields = null,
            int maxBytes = DefaultMaxBytes)
        {
            if (value is null)
                return null;

            try
            {
                var node = JsonSerializer.SerializeToNode(value, value.GetType(), SerializerOptions);

                MaskNode(node, BuildFieldSet(sensitiveFields));

                var json = node?.ToJsonString(SerializerOptions);

                return json is null ? null : TruncateToBytes(json, maxBytes);
            }
            catch
            {
                return SerializationFailedPlaceholder;
            }
        }

        /// <summary>
        /// Masks sensitive fields inside an already serialized JSON document.
        /// Used for raw request bodies, which are read as strings rather than objects.
        /// </summary>
        /// <param name="json">Raw JSON text; malformed or non-JSON input is replaced by a bounded placeholder.</param>
        /// <param name="sensitiveFields">Field names to mask; defaults are used when null or empty.</param>
        /// <param name="maxBytes">Maximum accepted size of the returned string.</param>
        /// <returns>The masked JSON string, a placeholder for invalid JSON, or null when the input is null or empty.</returns>
        public static string? MaskJson(
            string? json,
            IReadOnlyCollection<string>? sensitiveFields = null,
            int maxBytes = DefaultMaxBytes)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            try
            {
                var node = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { MaxDepth = 16 });

                if (node is null)
                    return TruncateToBytes(json, maxBytes);

                MaskNode(node, BuildFieldSet(sensitiveFields));

                return TruncateToBytes(node.ToJsonString(SerializerOptions), maxBytes);
            }
            catch
            {
                // Form encoded or otherwise malformed bodies: no sensitive structure can be
                // identified reliably, so the raw text is never returned (not even truncated).
                return InvalidJsonPlaceholder;
            }
        }

        /// <summary>
        /// Builds the case-insensitive lookup of sensitive field names.
        /// </summary>
        /// <param name="sensitiveFields">Configured field names, or null for the built-in defaults.</param>
        /// <returns>A set used during traversal.</returns>
        private static HashSet<string> BuildFieldSet(IReadOnlyCollection<string>? sensitiveFields)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var source = sensitiveFields is null || sensitiveFields.Count == 0
                ? GelfOptions.DefaultSensitiveFields
                : sensitiveFields;

            foreach (var field in source)
            {
                if (!string.IsNullOrWhiteSpace(field))
                    set.Add(field.Trim());
            }

            return set;
        }

        /// <summary>
        /// Recursively masks matching keys inside a JSON node graph.
        /// </summary>
        /// <param name="node">Node to traverse; mutated in place.</param>
        /// <param name="sensitiveFields">Field names that must be masked.</param>
        private static void MaskNode(JsonNode? node, HashSet<string> sensitiveFields)
        {
            switch (node)
            {
                case JsonObject jsonObject:
                    // Snapshot the keys: the object is mutated while iterating.
                    foreach (var key in jsonObject.Select(pair => pair.Key).ToList())
                    {
                        if (sensitiveFields.Contains(key))
                        {
                            jsonObject[key] = MaskedValue;
                            continue;
                        }

                        MaskNode(jsonObject[key], sensitiveFields);
                    }

                    break;

                case JsonArray jsonArray:
                    foreach (var item in jsonArray)
                        MaskNode(item, sensitiveFields);

                    break;
            }
        }

        /// <summary>
        /// Shortens a string so that its UTF-8 encoding does not exceed the byte budget.
        /// </summary>
        /// <param name="value">Source string.</param>
        /// <param name="maxBytes">Maximum accepted UTF-8 byte count; non-positive values disable truncation.</param>
        /// <returns>The original string when it fits, otherwise a truncated copy with a marker.</returns>
        private static string TruncateToBytes(string value, int maxBytes)
        {
            if (maxBytes <= 0 || Encoding.UTF8.GetByteCount(value) <= maxBytes)
                return value;

            // Binary search the longest prefix whose UTF-8 representation fits the budget.
            var low = 0;
            var high = value.Length;

            while (low < high)
            {
                var middle = (low + high + 1) / 2;

                if (Encoding.UTF8.GetByteCount(value.AsSpan(0, middle)) <= maxBytes)
                    low = middle;
                else
                    high = middle - 1;
            }

            // Never split a surrogate pair: that would produce an invalid UTF-16 fragment.
            if (low > 0 && char.IsHighSurrogate(value[low - 1]))
                low--;

            return value.Substring(0, low) + TruncationMarker;
        }
    }
}

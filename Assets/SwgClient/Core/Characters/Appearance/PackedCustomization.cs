using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Swg.Client.Core.Characters.Appearance
{
    /// <summary>The native client's escaped customization string, preserved without UTF-8 conversion.</summary>
    public sealed class PackedCustomization
    {
        public byte[] Encoded
        {
            get; private set;
        }
        public IReadOnlyDictionary<byte, short> Values
        {
            get; private set;
        }

        public static PackedCustomization Decode(byte[] encoded)
        {
            if (encoded == null)
                throw new ArgumentNullException(nameof(encoded));
            if (encoded.Length > 65536)
                throw new InvalidDataException("Oversized customization string");
            var values = new Dictionary<byte, short>();
            if (encoded.Length != 0)
            {
                int offset = 0;
                byte Next()
                {
                    if (offset >= encoded.Length)
                        throw new InvalidDataException("Truncated customization string");
                    byte value = encoded[offset++];
                    if (value != 0xff)
                        return value;
                    if (offset >= encoded.Length)
                        throw new InvalidDataException("Truncated customization escape");
                    switch (encoded[offset++])
                    {
                        case 1:
                            return 0;
                        case 2:
                            return 255;
                        default:
                            throw new InvalidDataException("Invalid customization escape or premature terminator");
                    }
                }

                if (Next() != 1)
                    throw new InvalidDataException("Unsupported customization version");
                int count = Next();
                // Core3 recognizes 0xff as an empty/sentinel variable count.
                if (count != 255)
                    for (int i = 0; i < count; i++)
                    {
                        byte type = Next();
                        int value = Next();
                        if ((type & 0x80) != 0)
                        {
                            type &= 0x7f;
                            value |= Next() << 8;
                        }

                        values[type] = unchecked((short)value);
                    }

                if (offset != encoded.Length - 2 || encoded[offset] != 255 || encoded[offset + 1] != 3)
                    throw new InvalidDataException("Missing customization terminator or unexpected trailing data");
            }

            return new PackedCustomization
            {
                Encoded = (byte[])encoded.Clone(),
                Values = new ReadOnlyDictionary<byte, short>(values)
            };
        }

        public IReadOnlyDictionary<string, short> ResolveNames(IReadOnlyDictionary<ushort, string> ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));
            var result = new Dictionary<string, short>(StringComparer.Ordinal);
            foreach (var pair in Values)
                if (ids.TryGetValue(pair.Key, out var name))
                    result[name] = pair.Value;
            return new ReadOnlyDictionary<string, short>(result);
        }
    }
}

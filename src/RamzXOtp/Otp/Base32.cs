// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Text;

namespace RamzXOtp.Otp
{
    /// <summary>
    /// Base32 (RFC 4648, section 6) with the lenient input handling OTP secrets need:
    /// spaces, hyphens, line breaks, lower case and missing or present padding are accepted.
    /// </summary>
    public static class Base32
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>Encodes without padding (as recommended for otpauth:// URIs).</summary>
        public static string Encode(byte[] data, bool padding = false)
        {
            if (data == null) throw new ArgumentNullException("data");

            StringBuilder sb = new StringBuilder((data.Length * 8 + 4) / 5 + 6);
            int buffer = 0, bits = 0;
            foreach (byte b in data)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    sb.Append(Alphabet[(buffer >> (bits - 5)) & 0x1F]);
                    bits -= 5;
                }
            }
            if (bits > 0) sb.Append(Alphabet[(buffer << (5 - bits)) & 0x1F]);
            if (padding) while (sb.Length % 8 != 0) sb.Append('=');
            return sb.ToString();
        }

        /// <summary>Removes separators and padding and converts to upper case.</summary>
        public static string Normalize(string input)
        {
            if (input == null) throw new ArgumentNullException("input");

            StringBuilder sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (char.IsWhiteSpace(c) || c == '-' || c == '=') continue;
                sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>Decodes after <see cref="Normalize"/>. Throws FormatException on invalid input.</summary>
        public static byte[] Decode(string input)
        {
            string s = Normalize(input);

            // Lengths 1, 3 and 6 (mod 8) cannot come from whole bytes.
            int rest = s.Length % 8;
            if (rest == 1 || rest == 3 || rest == 6)
                throw new FormatException("Invalid Base32 length.");

            byte[] result = new byte[s.Length * 5 / 8];
            int buffer = 0, bits = 0, pos = 0;
            foreach (char c in s)
            {
                int v = Alphabet.IndexOf(c);
                if (v < 0) throw new FormatException("Invalid Base32 character '" + c + "'.");
                buffer = (buffer << 5) | v;
                bits += 5;
                if (bits >= 8)
                {
                    result[pos++] = (byte)((buffer >> (bits - 8)) & 0xFF);
                    bits -= 8;
                }
            }
            return result;
        }

        /// <summary>Like <see cref="Decode"/>, but returns false instead of throwing.</summary>
        public static bool TryDecode(string input, out byte[] data)
        {
            try { data = Decode(input); return true; }
            catch (FormatException) { data = null; return false; }
        }
    }
}

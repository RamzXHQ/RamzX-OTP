// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;

namespace RamzXOtp.Qr
{
    /// <summary>Arithmetic in GF(2^8) with the QR code polynomial x^8 + x^4 + x^3 + x^2 + 1 (0x11D).</summary>
    internal static class Gf256
    {
        internal static readonly byte[] Exp = new byte[512];
        internal static readonly byte[] Log = new byte[256];

        static Gf256()
        {
            int x = 1;
            for (int i = 0; i < 255; ++i)
            {
                Exp[i] = (byte)x;
                Log[x] = (byte)i;
                x <<= 1;
                if ((x & 0x100) != 0) x ^= 0x11D;
            }
            for (int i = 255; i < 512; ++i) Exp[i] = Exp[i - 255];
        }

        internal static byte Mul(int a, int b)
        {
            if (a == 0 || b == 0) return 0;
            return Exp[Log[a] + Log[b]];
        }

        internal static byte Div(int a, int b)
        {
            if (b == 0) throw new DivideByZeroException();
            if (a == 0) return 0;
            return Exp[Log[a] + 255 - Log[b]];
        }

        /// <summary>alpha^e for any integer e.</summary>
        internal static byte Pow(int e)
        {
            e %= 255;
            if (e < 0) e += 255;
            return Exp[e];
        }
    }

    /// <summary>
    /// Reed-Solomon code as used by QR codes: generator roots alpha^0 .. alpha^(n-1),
    /// codeword bytes ordered from the highest polynomial degree to the lowest.
    /// Decoder: syndromes, Berlekamp-Massey, Chien search, Forney.
    /// </summary>
    internal static class ReedSolomon
    {
        /// <summary>Coefficients of the monic generator polynomial without its leading 1, highest degree first.</summary>
        internal static byte[] Generator(int degree)
        {
            if (degree < 1 || degree > 254) throw new ArgumentOutOfRangeException("degree");

            byte[] r = new byte[degree];
            r[degree - 1] = 1;
            int root = 1;
            for (int i = 0; i < degree; ++i)
            {
                // Multiply the current product by (x - root).
                for (int j = 0; j < r.Length; ++j)
                {
                    r[j] = Gf256.Mul(r[j], root);
                    if (j + 1 < r.Length) r[j] ^= r[j + 1];
                }
                root = Gf256.Mul(root, 2);
            }
            return r;
        }

        /// <summary>Error correction bytes for the data (remainder of data * x^n divided by the generator).</summary>
        internal static byte[] Encode(byte[] data, int eccLength)
        {
            byte[] gen = Generator(eccLength);
            byte[] rem = new byte[eccLength];
            foreach (byte b in data)
            {
                int factor = b ^ rem[0];
                Array.Copy(rem, 1, rem, 0, rem.Length - 1);
                rem[rem.Length - 1] = 0;
                for (int i = 0; i < rem.Length; ++i) rem[i] ^= Gf256.Mul(gen[i], factor);
            }
            return rem;
        }

        /// <summary>
        /// Corrects up to eccLength/2 byte errors in place (block = data followed by ecc bytes).
        /// Returns false if the block cannot be corrected.
        /// </summary>
        internal static bool Correct(byte[] block, int eccLength)
        {
            int n = block.Length;
            if (eccLength <= 0 || eccLength >= n) throw new ArgumentOutOfRangeException("eccLength");

            byte[] s = Syndromes(block, eccLength);
            if (AllZero(s)) return true;

            // Berlekamp-Massey: error locator C(x), lowest degree first.
            byte[] c = new byte[eccLength + 1];
            byte[] b = new byte[eccLength + 1];
            c[0] = 1;
            b[0] = 1;
            int l = 0, m = 1;
            int bScale = 1;
            for (int k = 0; k < eccLength; ++k)
            {
                int d = s[k];
                for (int i = 1; i <= l; ++i) d ^= Gf256.Mul(c[i], s[k - i]);

                if (d == 0) { ++m; continue; }

                byte coef = Gf256.Div(d, bScale);
                if (2 * l <= k)
                {
                    byte[] t = (byte[])c.Clone();
                    for (int i = 0; i + m <= eccLength; ++i) c[i + m] ^= Gf256.Mul(coef, b[i]);
                    l = k + 1 - l;
                    b = t;
                    bScale = d;
                    m = 1;
                }
                else
                {
                    for (int i = 0; i + m <= eccLength; ++i) c[i + m] ^= Gf256.Mul(coef, b[i]);
                    ++m;
                }
            }
            if (2 * l > eccLength) return false;

            // Chien search: position pos has power p = n-1-pos; error if C(alpha^-p) == 0.
            int[] positions = new int[l];
            int found = 0;
            for (int pos = 0; pos < n; ++pos)
            {
                int xInv = Gf256.Pow(-(n - 1 - pos));
                if (Eval(c, l, xInv) == 0)
                {
                    if (found == l) return false;
                    positions[found++] = pos;
                }
            }
            if (found != l) return false;

            // Omega(x) = S(x) * C(x) mod x^eccLength.
            byte[] omega = new byte[eccLength];
            for (int i = 0; i < eccLength; ++i)
                for (int j = 0; j <= i && j <= l; ++j)
                    omega[i] ^= Gf256.Mul(c[j], s[i - j]);

            // Forney (first consecutive root alpha^0): e = X * Omega(X^-1) / C'(X^-1).
            foreach (int pos in positions)
            {
                int p = n - 1 - pos;
                int x = Gf256.Pow(p);
                int xInv = Gf256.Pow(-p);

                int num = Eval(omega, eccLength - 1, xInv);
                int den = 0;
                for (int i = 1; i <= l; i += 2) den ^= Gf256.Mul(c[i], Gf256.Pow(Gf256.Log[xInv] * (i - 1)));
                if (den == 0) return false;

                block[pos] ^= Gf256.Mul(x, Gf256.Div(num, den));
            }

            return AllZero(Syndromes(block, eccLength));
        }

        private static byte[] Syndromes(byte[] block, int eccLength)
        {
            byte[] s = new byte[eccLength];
            for (int j = 0; j < eccLength; ++j)
            {
                int x = Gf256.Pow(j);
                int v = 0;
                foreach (byte r in block) v = Gf256.Mul(v, x) ^ r;
                s[j] = (byte)v;
            }
            return s;
        }

        // Evaluates sum_{i=0..degree} poly[i] * x^i.
        private static int Eval(byte[] poly, int degree, int x)
        {
            int v = 0;
            for (int i = degree; i >= 0; --i) v = Gf256.Mul(v, x) ^ poly[i];
            return v;
        }

        private static bool AllZero(byte[] a)
        {
            foreach (byte v in a) if (v != 0) return false;
            return true;
        }
    }
}

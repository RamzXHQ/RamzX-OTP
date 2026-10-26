// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;

namespace RamzXOtp.Qr
{
    internal sealed class FinderPattern
    {
        internal float X, Y, Module;
        internal int Count;
    }

    /// <summary>
    /// Finds the three finder patterns (1:1:3:1:1 black/white runs in both directions) and
    /// samples the module grid with an affine mapping. Screen QR codes are sharp and not
    /// perspective-distorted, so no perspective correction is needed; rotation is handled.
    /// </summary>
    internal static class QrDetector
    {
        internal static List<FinderPattern> FindFinders(BitImage img)
        {
            List<FinderPattern> found = new List<FinderPattern>();
            int w = img.Width;
            int[] start = new int[w + 1], len = new int[w + 1];
            bool[] black = new bool[w + 1];

            for (int y = 0; y < img.Height; ++y)
            {
                int n = 0;
                for (int x = 0; x < w;)
                {
                    bool c = img[x, y];
                    int s = x;
                    while (x < w && img[x, y] == c) ++x;
                    start[n] = s; len[n] = x - s; black[n] = c; ++n;
                }

                for (int i = 0; i + 4 < n; ++i)
                {
                    if (!black[i]) continue;
                    int a = len[i], b = len[i + 1], c = len[i + 2], d = len[i + 3], e = len[i + 4];
                    if (!IsFinderRatio(a, b, c, d, e)) continue;

                    float hTotal = a + b + c + d + e;
                    float cx = start[i + 2] + c / 2f;
                    float cy, vTotal, cx2, hTotal2;
                    if (!CrossCheck(img, (int)cx, y, true, hTotal, out cy, out vTotal)) continue;
                    if (!CrossCheck(img, (int)cx, (int)cy, false, hTotal, out cx2, out hTotal2)) continue;
                    Add(found, cx2, cy, (hTotal2 + vTotal) / 14f);
                    // A crafted image tiled with finder-like squares would make the search
                    // quadratic and freeze KeePass for minutes: give up on this image instead.
                    if (found.Count > MaxFinders) return new List<FinderPattern>();
                }
            }
            return found;
        }

        /// <summary>Real screenshots have 3 per QR code; even a page of 60 codes stays below this.</summary>
        internal const int MaxFinders = 200;

        private static bool IsFinderRatio(int a, int b, int c, int d, int e)
        {
            int total = a + b + c + d + e;
            if (total < 7) return false;
            float m = total / 7f, tol = m * 0.6f;
            return Math.Abs(a - m) < tol && Math.Abs(b - m) < tol && Math.Abs(c - 3 * m) < 3 * tol
                && Math.Abs(d - m) < tol && Math.Abs(e - m) < tol;
        }

        // Re-measures the pattern along one axis through (x0, y0).
        private static bool CrossCheck(BitImage img, int x0, int y0, bool vertical, float expectedTotal,
            out float center, out float total)
        {
            center = total = 0;
            int limit = vertical ? img.Height : img.Width;
            int p0 = vertical ? y0 : x0;
            if (p0 < 0 || p0 >= limit) return false;
            Func<int, bool> at = q => vertical ? img[x0, q] : img[q, y0];
            if (!at(p0)) return false;

            int[] c = new int[5];
            int p = p0;
            while (p >= 0 && at(p)) { ++c[2]; --p; }
            while (p >= 0 && !at(p)) { ++c[1]; --p; }
            while (p >= 0 && at(p) && c[0] <= c[2]) { ++c[0]; --p; }
            p = p0 + 1;
            while (p < limit && at(p)) { ++c[2]; ++p; }
            int endCenter = p;
            while (p < limit && !at(p)) { ++c[3]; ++p; }
            while (p < limit && at(p) && c[4] <= c[2]) { ++c[4]; ++p; }

            if (c[0] == 0 || c[1] == 0 || c[3] == 0 || c[4] == 0) return false;
            if (!IsFinderRatio(c[0], c[1], c[2], c[3], c[4])) return false;

            total = c[0] + c[1] + c[2] + c[3] + c[4];
            if (Math.Abs(total - expectedTotal) > expectedTotal * 0.6f) return false;
            center = endCenter - c[2] / 2f;
            return true;
        }

        private static void Add(List<FinderPattern> list, float x, float y, float module)
        {
            foreach (FinderPattern f in list)
            {
                float tol = Math.Max(f.Module, module) * 1.5f;
                if (Math.Abs(f.X - x) <= tol && Math.Abs(f.Y - y) <= tol &&
                    Math.Abs(f.Module - module) <= Math.Max(1f, f.Module * 0.5f))
                {
                    f.X = (f.X * f.Count + x) / (f.Count + 1);
                    f.Y = (f.Y * f.Count + y) / (f.Count + 1);
                    f.Module = (f.Module * f.Count + module) / (f.Count + 1);
                    ++f.Count;
                    return;
                }
            }
            list.Add(new FinderPattern { X = x, Y = y, Module = module, Count = 1 });
        }

        /// <summary>
        /// Combinations of three finders that form a right isosceles triangle, best first, as
        /// (top-left, top-right, bottom-left).
        /// </summary>
        internal static List<FinderPattern[]> Triples(List<FinderPattern> finders)
        {
            List<FinderPattern> cands = finders.FindAll(f => f.Count >= 2);
            if (cands.Count < 3) cands = new List<FinderPattern>(finders);
            cands.Sort((a, b) => b.Count.CompareTo(a.Count));
            if (cands.Count > 15) cands.RemoveRange(15, cands.Count - 15);

            List<KeyValuePair<double, FinderPattern[]>> scored = new List<KeyValuePair<double, FinderPattern[]>>();
            for (int i = 0; i < cands.Count; ++i)
                for (int j = i + 1; j < cands.Count; ++j)
                    for (int k = j + 1; k < cands.Count; ++k)
                    {
                        FinderPattern[] t = Order(cands[i], cands[j], cands[k]);
                        double score;
                        if (t != null && Score(t, out score)) scored.Add(new KeyValuePair<double, FinderPattern[]>(score, t));
                    }
            scored.Sort((a, b) => a.Key.CompareTo(b.Key));
            return scored.ConvertAll(kv => kv.Value);
        }

        // Puts the right-angle corner first and orients the other two clockwise.
        private static FinderPattern[] Order(FinderPattern a, FinderPattern b, FinderPattern c)
        {
            double ab = Dist(a, b), ac = Dist(a, c), bc = Dist(b, c);
            FinderPattern tl, p, q;
            if (bc >= ab && bc >= ac) { tl = a; p = b; q = c; }
            else if (ac >= ab && ac >= bc) { tl = b; p = a; q = c; }
            else { tl = c; p = a; q = b; }

            double cross = (p.X - tl.X) * (q.Y - tl.Y) - (p.Y - tl.Y) * (q.X - tl.X);
            return cross > 0 ? new[] { tl, p, q } : new[] { tl, q, p };
        }

        private static bool Score(FinderPattern[] t, out double score)
        {
            score = 0;
            float mMin = Math.Min(t[0].Module, Math.Min(t[1].Module, t[2].Module));
            float mMax = Math.Max(t[0].Module, Math.Max(t[1].Module, t[2].Module));
            if (mMax > mMin * 1.4f) return false;

            double l1 = Dist(t[0], t[1]), l2 = Dist(t[0], t[2]), hyp = Dist(t[1], t[2]);
            double legDiff = Math.Abs(l1 - l2) / Math.Max(l1, l2);
            double angleDiff = Math.Abs(hyp - Math.Sqrt(l1 * l1 + l2 * l2)) / hyp;
            if (legDiff > 0.2 || angleDiff > 0.1) return false;

            double modules = (l1 + l2) / 2 / ((mMin + mMax) / 2);
            if (modules < 12 || modules > 175) return false;

            score = legDiff + angleDiff + (mMax - mMin) / mMax;
            return true;
        }

        private static double Dist(FinderPattern a, FinderPattern b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Candidate symbol sizes for a triple, most likely first.</summary>
        internal static List<int> Dimensions(FinderPattern[] t)
        {
            float module = (t[0].Module + t[1].Module + t[2].Module) / 3f;
            double est = (Dist(t[0], t[1]) + Dist(t[0], t[2])) / 2 / module + 7;
            int best = 17 + 4 * (int)Math.Round((est - 17) / 4);
            List<int> dims = new List<int>();
            foreach (int d in new[] { best, best - 4, best + 4, best - 8, best + 8 })
                if (d >= 21 && d <= 177) dims.Add(d);
            return dims;
        }

        /// <summary>Samples the module centres; null if the grid leaves the image.</summary>
        internal static bool[,] Sample(BitImage img, FinderPattern[] t, int dim)
        {
            float span = dim - 7;
            float ux = (t[1].X - t[0].X) / span, uy = (t[1].Y - t[0].Y) / span;
            float vx = (t[2].X - t[0].X) / span, vy = (t[2].Y - t[0].Y) / span;

            bool[,] grid = new bool[dim, dim];
            for (int my = 0; my < dim; ++my)
                for (int mx = 0; mx < dim; ++mx)
                {
                    // Finder centres are the centres of module 3; module m is centred at m + 0.5.
                    float fx = t[0].X + (mx - 3) * ux + (my - 3) * vx;
                    float fy = t[0].Y + (mx - 3) * uy + (my - 3) * vy;
                    int px = (int)Math.Floor(fx), py = (int)Math.Floor(fy);
                    if (px < 0 || py < 0 || px >= img.Width || py >= img.Height) return null;
                    grid[mx, my] = img[px, py];
                }
            return grid;
        }
    }
}

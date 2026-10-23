// RamzX OTP - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using KeePass.App.Configuration;

using RamzXOtp.Integration;

using Xunit;

namespace RamzXOtp.Tests
{
    // Stand-ins with the same public shape as MiftawerkUnlock.MiftawerkUnlockApi.
    public static class FakeUnlockApiV1
    {
        public const int ApiVersion = 1;
        public static bool Answer = true;
        public static string LastMessage;

        public static Task<bool> VerifyUserAsync(IWin32Window owner, string message)
        {
            LastMessage = message;
            return Task.FromResult(Answer);
        }
    }

    public static class FakeUnlockApiV2
    {
        public const int ApiVersion = 2;
        public static Task<bool> VerifyUserAsync(IWin32Window owner, string message) { return Task.FromResult(true); }
    }

    public static class FakeUnlockApiWrongSignature
    {
        public const int ApiVersion = 1;
        public static bool VerifyUserAsync(IWin32Window owner, string message) { return true; }
    }

    public class UnlockBridgeTests
    {
        [Fact]
        public async Task Bind_CompatibleApi_Verifies()
        {
            UnlockBridge b = UnlockBridge.Bind(typeof(FakeUnlockApiV1));
            Assert.NotNull(b);

            FakeUnlockApiV1.Answer = true;
            Assert.True(await b.VerifyAsync(null, "hello"));
            Assert.Equal("hello", FakeUnlockApiV1.LastMessage);

            FakeUnlockApiV1.Answer = false;
            Assert.False(await b.VerifyAsync(null, "x"));
        }

        [Fact]
        public void Bind_IncompatibleApis_AreIgnored()
        {
            Assert.Null(UnlockBridge.Bind(null));
            Assert.Null(UnlockBridge.Bind(typeof(FakeUnlockApiV2)));
            Assert.Null(UnlockBridge.Bind(typeof(FakeUnlockApiWrongSignature)));
            Assert.Null(UnlockBridge.Bind(typeof(string)));
        }

        [Fact]
        public void Find_WithoutMiftawerkUnlockLoaded_ReturnsNull()
        {
            Assert.Null(UnlockBridge.Find()); // the test process does not load Miftawerk Unlock
        }
    }

    public class CopyGuardTests
    {
        private DateTime m_now = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
        private int m_prompts;
        private bool m_answer = true;
        private bool m_unlockLoaded = true;

        private CopyGuard NewGuard(AceCustomConfig cfg)
        {
            return new CopyGuard(cfg, () =>
            {
                if (!m_unlockLoaded) return null;
                return msg => { ++m_prompts; return Task.FromResult(m_answer); };
            }, () => m_now);
        }

        [Fact]
        public async Task Off_ByDefault_NoPrompt()
        {
            CopyGuard g = NewGuard(new AceCustomConfig());
            Assert.False(g.RequireHello);
            Assert.True(g.MayShowWithoutAsking);
            Assert.Equal(GuardResult.Allowed, await g.CheckAsync("x"));
            Assert.Equal(0, m_prompts);
        }

        [Fact]
        public async Task Enable_NeedsSuccessfulHello()
        {
            CopyGuard g = NewGuard(new AceCustomConfig());
            m_answer = false;
            Assert.False(await g.EnableAsync("x"));
            Assert.False(g.RequireHello);

            m_answer = true;
            Assert.True(await g.EnableAsync("x"));
            Assert.True(g.RequireHello);

            m_unlockLoaded = false;
            CopyGuard g2 = NewGuard(new AceCustomConfig());
            Assert.False(await g2.EnableAsync("x"));
        }

        [Fact]
        public async Task On_AsksOnce_ThenGraceTime_ThenAgain()
        {
            AceCustomConfig cfg = new AceCustomConfig();
            CopyGuard g = NewGuard(cfg);
            g.RequireHello = true;
            Assert.False(g.MayShowWithoutAsking);

            Assert.Equal(GuardResult.Allowed, await g.CheckAsync("x"));
            Assert.Equal(1, m_prompts);

            m_now = m_now.AddSeconds(20); // within 30 s grace time
            Assert.True(g.MayShowWithoutAsking);
            Assert.Equal(GuardResult.Allowed, await g.CheckAsync("x"));
            Assert.Equal(1, m_prompts);

            m_now = m_now.AddSeconds(15); // grace time over
            Assert.False(g.MayShowWithoutAsking);
            m_answer = false;
            Assert.Equal(GuardResult.Denied, await g.CheckAsync("x"));
            Assert.Equal(2, m_prompts);
        }

        [Fact]
        public async Task On_ButUnlockMissing_IsReported()
        {
            CopyGuard g = NewGuard(new AceCustomConfig());
            g.RequireHello = true;
            m_unlockLoaded = false;
            Assert.False(g.UnlockAvailable);
            Assert.Equal(GuardResult.UnlockMissing, await g.CheckAsync("x"));
        }

        [Fact]
        public async Task SwitchingOff_ClearsGraceTime_AndIsStored()
        {
            AceCustomConfig cfg = new AceCustomConfig();
            CopyGuard g = NewGuard(cfg);
            Assert.True(await g.EnableAsync("x"));
            Assert.True(cfg.GetBool(CopyGuard.ConfigKey, false));

            g.RequireHello = false;
            Assert.False(cfg.GetBool(CopyGuard.ConfigKey, true));
            g.RequireHello = true;
            Assert.False(g.MayShowWithoutAsking); // grace time was cleared
        }

        [Fact]
        public void NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CopyGuard(null, () => null, () => DateTime.UtcNow));
        }
    }
}

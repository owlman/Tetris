using System;
using System.Reflection;
using owlgame;

namespace TetrisTests
{
    /// <summary>
    /// The bilingual text table. Deliberately UI-free: it never constructs a
    /// window, so it also runs in the headless CI job.
    ///
    /// Two things are being guarded here:
    ///   * the language resolution order (command line beats env var beats OS culture);
    ///   * that neither column of the table is left half-translated - every
    ///     Chinese string must contain a CJK glyph, every English one must not.
    /// </summary>
    public class StringsTests
    {
        private const string EnvName = "OWLGAME_LANG";

        private static void WithEnv(string value, Action body)
        {
            string saved = Environment.GetEnvironmentVariable(EnvName);
            try
            {
                Environment.SetEnvironmentVariable(EnvName, value);
                body();
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvName, saved);
                Strings.SetLanguage(Strings.Language.Chinese);
            }
        }

        private static string[] AllChinese()
        {
            return new string[]
            {
                Strings.UiFont, Strings.MainTitle, Strings.Welcome,
                Strings.PreviewTitle, Strings.ControlTitle,
                Strings.ScoreCaption, Strings.LevelCaption, Strings.LinesCaption,
                Strings.Credit, Strings.StartButton, Strings.StopButton,
                Strings.RestartButton, Strings.HelpButton, Strings.ExitButton,
                Strings.HelpTitle, Strings.HelpSubtitle, Strings.HelpRotate,
                Strings.HelpLeft, Strings.HelpRight, Strings.HelpDown,
                Strings.OkButton
            };
        }

        private static string[] AllEnglish()
        {
            Strings.SetLanguage(Strings.Language.English);
            try
            {
                return AllChinese();
            }
            finally
            {
                Strings.SetLanguage(Strings.Language.Chinese);
            }
        }

        private static bool HasCjk(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '\u4E00' && c <= '\u9FFF') return true;   // ideographs
                if (c >= '\u3000' && c <= '\u303F') return true;   // CJK punctuation
                if (c == '\uFF08' || c == '\uFF09') return true;   // full-width parens
                if (c == '\u2026') return true;                    // ellipsis
            }
            return false;
        }

        [Test]
        public void CommandLineBeatsTheEnvironmentVariable()
        {
            WithEnv("zh", delegate
            {
                Strings.Configure(new string[] { "--lang=en" });
                Assert.AreEqual("English", Strings.Current.ToString(),
                    "the explicit switch must win over the env var");
            });
        }

        [Test]
        public void EnvironmentVariableIsHonouredWhenNoSwitchIsGiven()
        {
            WithEnv("en", delegate
            {
                Strings.Configure(new string[0]);
                Assert.AreEqual("English", Strings.Current.ToString(),
                    "OWLGAME_LANG should be read when the command line is empty");
            });
        }

        [Test]
        public void UnknownValuesFallBackToTheSystemCulture()
        {
            WithEnv("klingon", delegate
            {
                Strings.Configure(new string[] { "--lang=klingon" });
                Assert.AreEqual(Strings.SystemDefault.ToString(),
                    Strings.Current.ToString(),
                    "a nonsense value must not be treated as either language");
            });
        }

        [Test]
        public void SwitchesAreCaseInsensitive()
        {
            WithEnv("", delegate
            {
                Strings.Configure(new string[] { "--LANG=EN" });
                Assert.AreEqual("English", Strings.Current.ToString(),
                    "the switch name and value are both case-insensitive");
            });
        }

        [Test]
        public void SystemDefaultFollowsTheUiCulture()
        {
            System.Globalization.CultureInfo saved =
                System.Globalization.CultureInfo.CurrentUICulture;
            try
            {
                System.Globalization.CultureInfo.CurrentUICulture =
                    new System.Globalization.CultureInfo("zh-CN");
                Assert.AreEqual("Chinese", Strings.SystemDefault.ToString(),
                    "zh-CN should map to Chinese");

                System.Globalization.CultureInfo.CurrentUICulture =
                    new System.Globalization.CultureInfo("en-US");
                Assert.AreEqual("English", Strings.SystemDefault.ToString(),
                    "en-US should map to English");
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentUICulture = saved;
            }
        }

        [Test]
        public void EveryChineseStringIsTranslated()
        {
            Strings.SetLanguage(Strings.Language.Chinese);
            string[] values = AllChinese();
            Assert.AreEqual(21, values.Length, "the table gained or lost an entry");

            for (int i = 0; i < values.Length; i++)
            {
                Assert.IsTrue(values[i] != null && values[i].Length > 0,
                    "Chinese entry " + i + " is empty");
                Assert.IsTrue(HasCjk(values[i]),
                    "Chinese entry " + i + " has no CJK glyph: " + values[i]);
            }
        }

        [Test]
        public void NoEnglishStringLeaksChinese()
        {
            string[] values = AllEnglish();
            for (int i = 0; i < values.Length; i++)
            {
                Assert.IsTrue(values[i] != null && values[i].Length > 0,
                    "English entry " + i + " is empty");
                Assert.IsTrue(!HasCjk(values[i]),
                    "English entry " + i + " still contains Chinese: " + values[i]);
            }
        }

        [Test]
        public void BothColumnsDiffer()
        {
            string[] chinese = AllChinese();
            string[] english = AllEnglish();
            for (int i = 0; i < chinese.Length; i++)
            {
                Assert.IsTrue(chinese[i] != english[i],
                    "entry " + i + " is identical in both languages: " + chinese[i]);
            }
        }

        [Test]
        public void TheTypefaceFollowsTheLanguage()
        {
            Strings.SetLanguage(Strings.Language.Chinese);
            Assert.AreEqual("\u5FAE\u8F6F\u96C5\u9ED1", Strings.UiFont,
                "Chinese UI uses Microsoft YaHei");
            Strings.SetLanguage(Strings.Language.English);
            Assert.AreEqual("Segoe UI", Strings.UiFont,
                "English UI uses Segoe UI");
            Strings.SetLanguage(Strings.Language.Chinese);
        }

        [Test]
        public void GameOverCarriesTheScore()
        {
            Strings.SetLanguage(Strings.Language.English);
            string rooms = "Game over" + Environment.NewLine + "Score ";
            Assert.AreEqual(rooms + "1234", Strings.GameOver(1234),
                "the score must be rendered into the overlay text");
            Assert.AreEqual(rooms + "0", Strings.GameOver(0),
                "a zero score is still a number, not a special case");
            Strings.SetLanguage(Strings.Language.Chinese);
            Assert.AreEqual("你失败了…" + Environment.NewLine + "分数 999999",
                Strings.GameOver(999999),
                "a six digit score must be rendered without being clipped");
            Strings.SetLanguage(Strings.Language.Chinese);
        }
    }
}

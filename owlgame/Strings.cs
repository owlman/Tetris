using System;
using System.Globalization;

namespace owlgame
{
    /// <summary>
    /// Bilingual UI text.
    ///
    /// The language is resolved once, before the first window is constructed,
    /// and never changes afterwards: every label is assigned from these
    /// properties while the form is being built. Switching at runtime would
    /// mean walking the control tree and re-measuring the layout, which this
    /// project has no reason to do.
    ///
    /// Resolution order (first match wins):
    ///   1. --lang=zh / --lang=en on the command line
    ///   2. the OWLGAME_LANG environment variable
    ///   3. the compile-time switch (LANG_ZH / LANG_EN), set by build-release.sh
    ///   4. the OS UI culture (Chinese culture -> Chinese, anything else -> English)
    /// </summary>
    public static class Strings
    {
        public enum Language
        {
            Chinese,
            English
        }

        private static Language current = Language.Chinese;

        private static readonly Language? buildLanguage = ResolveBuildLanguage();

        public static Language Current
        {
            get { return current; }
        }

        /// <summary>
        /// The language pinned at compile time, or null when this binary follows
        /// the OS culture. Pinned with <c>/define:LANG_ZH</c> or
        /// <c>/define:LANG_EN</c>; see <c>tests/build-release.sh</c>.
        /// </summary>
        public static Language? BuildLanguage
        {
            get { return buildLanguage; }
        }

        private static Language? ResolveBuildLanguage()
        {
#if LANG_ZH && LANG_EN
#error LANG_ZH and LANG_EN are mutually exclusive; pick one.
#endif
#if LANG_ZH
            return Language.Chinese;
#elif LANG_EN
            return Language.English;
#else
            return null;
#endif
        }

        /// <summary>The language the OS would suggest.</summary>
        public static Language SystemDefault
        {
            get
            {
                return CultureInfo.CurrentUICulture
                    .TwoLetterISOLanguageName == "zh"
                    ? Language.Chinese
                    : Language.English;
            }
        }

        public static void Configure(string[] args)
        {
            Language requested;

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (arg != null
                        && arg.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryParse(arg.Substring("--lang=".Length), out requested))
                        {
                            current = requested;
                            return;
                        }
                    }
                }
            }

            if (TryParse(Environment.GetEnvironmentVariable("OWLGAME_LANG"), out requested))
            {
                current = requested;
                return;
            }

            current = buildLanguage ?? SystemDefault;
        }

        public static void SetLanguage(Language language)
        {
            current = language;
        }

        private static bool TryParse(string value, out Language language)
        {
            language = Language.Chinese;
            if (string.IsNullOrEmpty(value))
                return false;

            string v = value.Trim().ToLowerInvariant();
            if (v == "zh" || v == "zh-cn" || v == "chinese")
            {
                language = Language.Chinese;
                return true;
            }
            if (v == "en" || v == "en-us" || v == "english")
            {
                language = Language.English;
                return true;
            }
            return false;
        }

        private static string T(string chinese, string english)
        {
            return current == Language.English ? english : chinese;
        }

        // ---- typeface -------------------------------------------------
        // YaHei carries the CJK glyphs the Chinese UI needs; Segoe UI is the
        // native Windows UI face and renders Latin text noticeably better.
        public static string UiFont
        {
            get { return T("微软雅黑", "Segoe UI"); }
        }

        // ---- main window ----------------------------------------------
        public static string MainTitle
        {
            get { return T("俄罗斯方块", "Tetris"); }
        }

        public static string Welcome
        {
            get { return T("欢迎尝试游戏", "Welcome to Tetris"); }
        }

        public static string PreviewTitle
        {
            get { return T("下一个", "Next"); }
        }

        public static string ControlTitle
        {
            get { return T("游戏控制", "Controls"); }
        }

        public static string ScoreCaption
        {
            get { return T("分数", "Score"); }
        }

        public static string LevelCaption
        {
            get { return T("等级", "Level"); }
        }

        public static string LinesCaption
        {
            get { return T("消行", "Lines"); }
        }

        public static string Credit
        {
            get { return T("owlman 制作", "by owlman"); }
        }

        public static string StartButton
        {
            get { return T("开始(&E)", "&Start"); }
        }

        public static string StopButton
        {
            get { return T("停止(&P)", "&Stop"); }
        }

        public static string RestartButton
        {
            get { return T("重玩(&O)", "&Restart"); }
        }

        public static string HelpButton
        {
            get { return T("说明(&S)", "&Help"); }
        }

        public static string ExitButton
        {
            get { return T("退出(&X)", "E&xit"); }
        }

        /// <summary>Text shown on the overlay once the stack reaches the top.</summary>
        /// <remarks>
        /// Split over two lines on purpose: the overlay is only as wide as the
        /// playfield (220 px) and a single line with a six digit score needs
        /// more than that, so it used to be clipped at the edges.
        /// </remarks>
        public static string GameOver(int score)
        {
            return T("你失败了…", "Game over")
                + Environment.NewLine
                + T("分数 ", "Score ")
                + score.ToString(CultureInfo.InvariantCulture);
        }

        // ---- help window ----------------------------------------------
        public static string HelpTitle
        {
            get { return T("操作说明", "Controls"); }
        }

        public static string HelpSubtitle
        {
            get { return T("键盘操作", "Keyboard"); }
        }

        public static string HelpRotate
        {
            get { return T("旋转", "Rotate"); }
        }

        public static string HelpLeft
        {
            get { return T("左移", "Left"); }
        }

        public static string HelpRight
        {
            get { return T("右移", "Right"); }
        }

        public static string HelpDown
        {
            get { return T("加速", "Drop"); }
        }

        public static string OkButton
        {
            get { return T("确定", "OK"); }
        }
    }
}

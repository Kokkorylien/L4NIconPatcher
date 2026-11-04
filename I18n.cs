using System.Globalization;
using System.Windows.Forms;

namespace L4NIconPatcher
{
    internal static class I18n
    {
        internal static bool English { get; set; }

        internal static void LoadLanguage()
        {
            English = Properties.Settings.Default.EnglishLanguage;
        }

        internal static void SaveLanguage()
        {
            Properties.Settings.Default.EnglishLanguage = English;
            Properties.Settings.Default.Save();
        }

        internal static string T(string chinese, string english)
        {
            return English ? english : chinese;
        }

        internal static string F(string chinese, string english, params object[] arguments)
        {
            return string.Format(CultureInfo.CurrentCulture, T(chinese, english), arguments);
        }

        internal static void Bind(Control control, string chinese, string english)
        {
            control.Tag = new[] { chinese, english };
            control.Text = T(chinese, english);
        }

        internal static void Apply(Control control)
        {
            var translations = control.Tag as string[];
            if (translations != null && translations.Length == 2)
            {
                control.Text = T(translations[0], translations[1]);
            }
            foreach (Control child in control.Controls) Apply(child);
        }
    }
}

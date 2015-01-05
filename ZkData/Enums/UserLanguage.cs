using System.ComponentModel;

namespace ZkData
{
    public enum UserLanguage
    {
        [Description("Auto, let the system decide")]
        auto,
        [Description("English")]
        en,
        [UserLanguageNote(
            "В текущий момент ведется локализация материалов сайта на русский язык, с целью помочь начинающим игрокам и сделать игру доступнее!<br /><a href='/Wiki/Localization'>Присоединяйся</a>"
            )]
        [Description("Йа креведко!")]
        ru
    }
}
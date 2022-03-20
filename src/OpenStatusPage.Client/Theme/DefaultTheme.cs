using MudBlazor;

namespace OpenStatusPage.Client.Theme
{
    public class DefaultTheme : MudTheme
    {
        public DefaultTheme()
        {
            Palette = new Palette()
            {
                AppbarBackground = Colors.Shades.White,
                AppbarText = "#424242ff",
            };
        }
    }
}

namespace InsightFlow
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage =
                new NavigationPage(
                    new MainPage())
                {
                    BarBackgroundColor =
                        Color.FromArgb("#0F2747"),

                    BarTextColor =
                        Colors.White
                };
        }
    }
}
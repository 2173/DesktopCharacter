using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DesktopCharacter.Model.Locator;
using DesktopCharacter.Model.Service.Slack;
using DesktopCharacter.ViewModel.Dialog;
using DesktopCharacter.Model.Service;

namespace DesktopCharacter.View.Dialog
{
    /// <summary>
    /// SlackSignInDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class SlackSignInDialog : Window
    {
        public SlackSignInDialog()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            var vm = DataContext as SlackSignInViewModel;

            var slackService = ServiceLocator.Instance.GetInstance<IService<SlackAuthInfo>>();
            Browser.Navigate(new Uri(slackService.AuthUrl()));
        }

        private void Browser_OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            if (e.Uri.Host == "localhost")
            {
                var vm = DataContext as SlackSignInViewModel;
                vm.Complete(e.Uri);
                Close();
            }
        }
    }
}

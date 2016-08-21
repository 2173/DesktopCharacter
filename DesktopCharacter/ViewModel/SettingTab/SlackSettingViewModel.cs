using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DesktopCharacter.Model.Database.Domain;
using DesktopCharacter.Model.Locator;
using DesktopCharacter.Model.Repository;
using DesktopCharacter.Model.Service.Slack;
using DesktopCharacter.View.Dialog;
using DesktopCharacter.ViewModel.Dialog;
using Livet.Commands;
using Livet.Messaging;
using DesktopCharacter.Model.Service;

namespace DesktopCharacter.ViewModel.SettingTab
{
    class SlackSettingViewModel : Livet.ViewModel, SettingViewModelBase
    {
        private ViewModelCommand _createAccount;
        public ViewModelCommand CreateAccount
        {
            get
            {
                if (_createAccount == null)
                {
                    _createAccount = new ViewModelCommand(() =>
                    {
                        var vm = new SlackSignInViewModel(OnCodeReceive);
                        Messenger.Raise(new TransitionMessage(typeof(SlackSignInDialog), vm, TransitionMode.Modal, "SignIn"));
                    });
                }
                return _createAccount;
            }
        }

        private ObservableCollection<SlackUser> _slackUsers;
        public ObservableCollection<SlackUser> SlackUsers
        {
            get
            {
                return _slackUsers;
            }
            set
            {
                _slackUsers = value;
                this.RaisePropertyChanged("SlackUsers");
            }
        }

        private SlackUser _selectedSlackUser;

        public SlackUser SelectedSlackUser
        {
            get { return _selectedSlackUser; }
            set
            {
                _selectedSlackUser = value;
                this.RaisePropertyChanged("SelectedSlackUser");
            }
        }

        private ViewModelCommand _deleteAccount;
        public ViewModelCommand DeleteAccount => _deleteAccount ??
            (_deleteAccount = new ViewModelCommand(() =>
            {
                _slackUsers.Remove(_selectedSlackUser);
            }));

        private readonly List<SlackUser> _oldSlackUsersList;


        public SlackSettingViewModel()
        {
            var slackUserRepository = ServiceLocator.Instance.GetInstance<SlackUserRepository>();

            SlackUsers = new ObservableCollection<SlackUser>();
            var users = slackUserRepository.FindAll();
            _oldSlackUsersList = users;

            UpdateSlackUsers();
        }

        public void OnClose()
        {
            //現在の状態を保存
            var slackUserRepository = ServiceLocator.Instance.GetInstance<SlackUserRepository>();
            slackUserRepository.Save(SlackUsers.ToList());

            //差分からアカウントを削除
            var diff = new List<SlackUser>(_oldSlackUsersList);
            foreach (var twitterUser in SlackUsers)
            {
                diff.Remove(twitterUser);
            }
            slackUserRepository.Delete(diff);
        }

        public void OnCodeReceive(Uri uri)
        {
            var query = uri.Query;
            if (query.Length != 0)
            {
                query = query.Substring(1);//先頭の?が邪魔なので削除
            }
            var uriParams = query.Split('&')
                .Select(q => q.Split('='))
                .ToDictionary(k => k[0], v => v[1]);

            if (!uriParams.ContainsKey("code"))
            {
                //codeの値がなければおかしい
                //キャンセルした時とかに入る。例外出さずにそのままクローズする
                return;
            }
            var code = uriParams["code"];


            var slackService = ServiceLocator.Instance.GetInstance<IService<SlackAuthInfo>>();
            //コードを使って認証
            var auth = slackService.ProcessAuth(code);
            //処理待ち
            auth.Wait();
            //永続化
            slackService.Save(auth.Result);

            Application.Current.Dispatcher.Invoke(UpdateSlackUsers);

        }

        private void UpdateSlackUsers()
        {
            var slackUserRepository = ServiceLocator.Instance.GetInstance<SlackUserRepository>();
            SlackUsers.Clear();
            var slackUsers = slackUserRepository.FindAll();
            slackUsers.ForEach(user => SlackUsers.Add(user));

            if (SelectedSlackUser != null)
            {
                var token = SelectedSlackUser.AccessToken;
                SelectedSlackUser = SlackUsers.ToList().Find(user => user.AccessToken == token);
            }
        }
    }
}

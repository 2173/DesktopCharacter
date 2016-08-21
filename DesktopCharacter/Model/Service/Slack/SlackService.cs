using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Codeplex.Data;
using DesktopCharacter.Model.Database.Domain;
using DesktopCharacter.Model.Locator;
using DesktopCharacter.Model.Repository;

namespace DesktopCharacter.Model.Service.Slack
{
    public class SlackService : IService<SlackAuthInfo>, IInitializable, IDisposable
    {
        private const string CLIENT_ID = "4009303066.63272279520";
        private const string CLIENT_SECRET = "d7e876a1670273602f6fdb55e1bf7dca";

        private List<SlackClient> _clientList = new List<SlackClient>();

        public string AuthUrl()
        {
            return $"https://slack.com/oauth/authorize?client_id={CLIENT_ID}&scope=client";
        }

        public Task<SlackAuthInfo> ProcessAuth(string code)
        {
            return Task.Run(() =>
            {
                using (var client = new HttpClient())
                {
                    var url = $"https://slack.com/api/oauth.access?client_id={CLIENT_ID}&client_secret={CLIENT_SECRET}&code={code}";
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var task = client.GetStringAsync(url);
                    task.Wait();
                    var json = DynamicJson.Parse(task.Result);
                    if (!json.ok)
                    {
                        throw new SlackAuthException(json.error);
                    }

                    var slackAuthInfo = new SlackAuthInfo(json.access_token, json.scope, json.user_id, json.team_name, json.team_id);
                    return slackAuthInfo;
                }
            });
        }

        public void Save(SlackAuthInfo info)
        {
            var slackUser = new SlackUser
            {
                AccessToken = info.AccessToken,
                TeamName = info.TeamName,
                Filter = new SlackNotificationFilter()
            };
            var repo = ServiceLocator.Instance.GetInstance<SlackUserRepository>();
            repo.Save(slackUser);

        }

        private List<SlackClient> FindAll()
        {
            var repo = ServiceLocator.Instance.GetInstance<SlackUserRepository>();
            return repo.FindAll()
                .Select(user => new SlackClient(user))
                .ToList();
        }

        public void Initialize()
        {
            _clientList = FindAll();
            foreach (var slackClient in _clientList)
            {
                slackClient.ConnectStreaming();
                slackClient.Message.Subscribe(json => OnMessageReceive(slackClient, json));
            }
        }

        public void Dispose()
        {
            foreach (var slackClient in _clientList)
            {
                slackClient.CloseStream();
            }
        }

        private void OnMessageReceive(SlackClient client, dynamic message)
        {
            var filter = client.User.Filter;
            var type = message.type;

            if (filter.Message && type == "message")
            {
                CharacterNotify.Instance.Talk("【Slack】" + message.text);
                return;
            }
        }
    }

    public class SlackAuthException : Exception
    {
        public SlackAuthException(string message) : base(message)
        {

        }
    }

    public class SlackAuthInfo
    {
        public string AccessToken { get; private set; }
        public string Scope { get; private set; }
        public string UserId { get; private set; }
        public string TeamName { get; private set; }
        public string TeamId { get; private set; }

        internal SlackAuthInfo(string token, string scope, string userId, string teamName, string teamId)
        {
            AccessToken = token;
            Scope = scope;
            UserId = userId;
            TeamName = teamName;
            TeamId = teamId;
        }
    }
}

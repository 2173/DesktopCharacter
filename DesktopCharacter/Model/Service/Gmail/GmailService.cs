using Codeplex.Data;
using DesktopCharacter.Model.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DesktopCharacter.Model.Service.Gmail
{
    public class GmailService : IService<GmailAuthInfo>, IInitializable, IDisposable
    {
        private const string CLIENT_ID = "786406455515-qs1iaktmq8f52g32jeeo9uml0ibp9s0g.apps.googleusercontent.com";
        private const string CLIENT_SECRET = "qrw9ov5KfgIpz85PmqHqv0KE";

        private List<GmailClinet> _clientList = new List<GmailClinet>();

        public string AuthUrl()
        {
            return $"https://accounts.google.com/o/oauth2/auth?client_id={CLIENT_ID}&redirect_uri=urn:ietf:wg:oauth:2.0:oob&scope=https://www.googleapis.com/auth/gmail.readonly&response_type=code&approval_prompt=force&access_type=offline";
        }

        public Task<GmailAuthInfo> ProcessAuth(string code)
        {
            return Task.Run(() =>
            {
                using (var client = new HttpClient())
                {
                    var url = $"curl -d client_id={CLIENT_ID} -d client_secret={CLIENT_SECRET} -d redirect_uri=urn:ietf:wg:oauth:2.0:oob -d grant_type=authorization_code -d code={code} https://accounts.google.com/o/oauth2/token";
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var task = client.GetStringAsync(url);
                    task.Wait();
                    var json = DynamicJson.Parse(task.Result);
                    if (!json.ok)
                    {
                        throw new GmailAuthException(json.error);
                    }
                    var slackAuthInfo = new GmailAuthInfo(json.access_token, json.token_type, json.expires_in, json.refresh_token);
                    return slackAuthInfo;
                }
            });
        }

        public void Save(GmailAuthInfo info)
        {
            //var slackUser = new SlackUser
            //{
            //    AccessToken = info.AccessToken,
            //    TeamName = info.TeamName,
            //    Filter = new SlackNotificationFilter()
            //};
            //var repo = ServiceLocator.Instance.GetInstance<SlackUserRepository>();
            //repo.Save(slackUser);
        }

        public void Initialize()
        {

        }

        public void Dispose()
        {

        }

        private void Reflec()
        {
            //string url = $"curl -d "client_id={CLIENT_ID}&client_secret={CLIENT_SECRET}&refresh_token={}&grant_type=refresh_token" https://accounts.google.com/o/oauth2/token";
        }

        private void OnMessageReceive(GmailClinet client, dynamic message)
        {
            //var filter = client.User.Filter;
            //var type = message.type;

            //if (filter.Message && type == "message")
            //{
            //    CharacterNotify.Instance.Talk("【Slack】" + message.text);
            //    return;
            //}
        }
    }

    public class GmailAuthException : Exception
    {
        public GmailAuthException(string message) : base(message)
        {

        }
    }

    public class GmailAuthInfo
    {
        public string AccessToken { get; private set; }
        public string TokenType { get; private set; }
        public string ExpiresIn { get; private set; }
        public string RefreshToken { get; private set; }

        internal GmailAuthInfo(string access_token, string token_type, string expires_in, string refresh_token)
        {
            AccessToken = access_token;
            TokenType = token_type;
            ExpiresIn = expires_in;
            RefreshToken = refresh_token;
        }
    }
}

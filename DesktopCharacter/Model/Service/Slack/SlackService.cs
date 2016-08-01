﻿using System;
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
    public interface ISlackService
    {
        /// <summary>
        /// 認証のためのURLを取得します。ユーザーはこのURLをブラウザで開き、認証を行います。
        /// 10分間のみ認証が有効です。
        /// 認証後はProcessAuthメソッドにコードを渡してSlackAuthInfoを取得してください。
        /// </summary>
        /// <returns>認証のためのurl</returns>
        string AuthUrl();

        /// <summary>
        /// FetchAuthUrlで開いたページで認証した後に得られるコードから認証情報を取得します。
        /// </summary>
        /// <param name="code">認証コード</param>
        /// <returns>認証情報</returns>
        Task<SlackAuthInfo> ProcessAuth(string code);

        /// <summary>
        /// 認証情報を永続化します。
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        void Save(SlackAuthInfo info);
    }

    public class SlackService : ISlackService, IInitializable, IDisposable
    {
        private const string CLIENT_ID = "4009303066.63272279520";
        private const string CLIENT_SECRET = "d7e876a1670273602f6fdb55e1bf7dca";

        private List<SlackClient> _clientList = new List<SlackClient>();

        public string AuthUrl()
        {
            return $"https://slack.com/oauth/authorize?client_id={CLIENT_ID}&scope=client";
        }

        public async Task<SlackAuthInfo> ProcessAuth(string code)
        {
            using (var client = new HttpClient())
            {
                var url = $"https://slack.com/api/oauth.access?client_id={CLIENT_ID}&client_secret={CLIENT_SECRET}&code={code}";
                client.Timeout = TimeSpan.FromSeconds(10);
                var result = await client.GetStringAsync(url);
                var json = DynamicJson.Parse(result);
                if (!json.ok)
                {
                    throw new SlackAuthException(json.error);
                }


                var slackAuthInfo = new SlackAuthInfo(json.access_token, json.scope, json.user_id, json.team_name, json.team_id);
                return slackAuthInfo;
            }
        }

        public void Save(SlackAuthInfo info)
        {
            var slackUser = new SlackUser
            {
                AccessToken = info.AccessToken,
                TeamName = info.TeamName
            };
            var repo = ServiceLocator.Instance.GetInstance<SlackUserRepository>();
            repo.Save(slackUser);

        }

        private List<SlackClient> FindAll()
        {
            var repo = ServiceLocator.Instance.GetInstance<SlackUserRepository>();
            return repo.FindAll()
                .Select(user => new SlackClient(user.AccessToken))
                .ToList();
        }

        public void Initialize()
        {
            _clientList = FindAll();
            foreach (var slackClient in _clientList)
            {
                slackClient.ConnectStreaming();
                slackClient.Message.Subscribe(str => CharacterNotify.Instance.Talk(str));
            }
        }

        public void Dispose()
        {
            foreach (var slackClient in _clientList)
            {
                slackClient.CloseStream();
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopCharacter.Model.Service
{
    public interface IService<Type>
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
        Task<Type> ProcessAuth(string code);

        /// <summary>
        /// 認証情報を永続化します。
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        void Save(Type info);
    }
}

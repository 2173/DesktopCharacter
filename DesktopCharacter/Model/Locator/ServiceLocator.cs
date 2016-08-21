using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesktopCharacter.Model.Repository;
using DesktopCharacter.Model.Service.Slack;
using DesktopCharacter.Model.Service.TimeSignal;
using DesktopCharacter.Model.Service.Twitter;
using DesktopCharacter.Model.AI;
using NLog;
using DesktopCharacter.Model.Service;

namespace DesktopCharacter.Model.Locator
{
    class ServiceLocator
    {

        public static ServiceLocator Instance { get; } = new ServiceLocator();
        private static Logger logger = LogManager.GetCurrentClassLogger();

        //毎回インスタンスを作成するスコープ
        private readonly InstanceContext prototypeContext = new PrototypeInstanceContext();
        //インスタンスを破棄しないシングルトンスコープ
        private readonly InstanceContext applicationContext = new InstanceContext();
        //コンフィグを更新した時にインスタンスを作りなおすスコープ
        private readonly InstanceContext configBaseContext = new InstanceContext();

        private ServiceLocator()
        {
        }

        /// <summary>
        /// ServiceLocatorの初期化
        /// </summary>
        public void InitializeServiceLocator()
        {
            logger.Info("InitializeServiceLocator");
            RegistFactories();
            InitializeContexts();
        }

        private void RegistFactories()
        {
            logger.Info("=== Begin RegiserFactories ===");
            RegisterByPrototypeScope<TwitterRepository>(() => new TwitterRepository());
            RegisterByConfigBaseScope<TwitterService>(() => new TwitterService());
            RegisterByConfigBaseScope<CodicRepository>(() => new CodicRepository());
            RegisterByPrototypeScope<WindowPositionRepository>(() => new WindowPositionRepository());
            RegisterByApplicationScope<BabumiConfigRepository>(() => new BabumiConfigRepository());
            RegisterByApplicationScope<ITimeSignalService>(() => new CompositeTimeSignalService(
                    new Service.TimeSignal.Impl.AnimationTimeSignal(),
                    new Service.TimeSignal.Impl.SimpleTimeSignalService())
                );
            RegisterByApplicationScope<BlackBoard>(() => new BlackBoard());
            RegisterByPrototypeScope<SlackUserRepository>(() => new SlackUserRepository());
            RegisterByConfigBaseScope<IService<SlackAuthInfo>>(() => new SlackService());
            logger.Info("=== End RegistFactories ===");
        }

        private void InitializeContexts()
        {
            logger.Info("InitializeContexts");
            InstanceContext[] contexts = { applicationContext, configBaseContext, prototypeContext };
            //インスタンスを最初に用意
            foreach (var instanceContext in contexts)
            {
                instanceContext.Initialize();
            }
            //全てのコンテキストの用意が終わってから呼び出し
            foreach (var instanceContext in contexts)
            {
                instanceContext.CallInitializeAll();
            }
        }

        /// <summary>
        /// インスタンスを取得
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetInstance<T>()
        {
            InstanceContext[] contexts = {applicationContext, configBaseContext, prototypeContext};
            foreach (var instanceContext in contexts)
            {
                var instance = instanceContext.GetInstance<T>();
                if (instance != null)
                {
                    return instance;
                }
            }
            throw new NullReferenceException("Instance not found.");
        }

        /// <summary>
        /// 設定が更新された時にインスタンスを作りなおす
        /// </summary>
        public void ClearConfigBaseContext()
        {
            logger.Info("ClearConfigBaseContext");
            configBaseContext.ClearCacheAll();
            configBaseContext.Initialize();
            configBaseContext.CallInitializeAll();
        }

        private void RegisterByPrototypeScope<T>(Func<T> instanceFactory) where T: class 
        {
            prototypeContext.RegisterFactory<T>(instanceFactory);
            logger.Info("Scope=Prototype Type={0}", typeof(T).FullName);
        }

        private void RegisterByApplicationScope<T>(Func<T> instanceFactory) where T : class
        {
            applicationContext.RegisterFactory<T>(instanceFactory);
            logger.Info("Scope=Application Type={0}", typeof(T).FullName);
        }

        private void RegisterByConfigBaseScope<T>(Func<T> instanceFactory) where T : class 
        {
            configBaseContext.RegisterFactory<T>(instanceFactory);
            logger.Info("Scope=ConfigBase Type={0}", typeof(T).FullName);
        }
    }

    /// <summary>
    /// インスタンスの生存管理を行うコンテキスト
    /// </summary>
    public class InstanceContext
    {
        //インスタンスを作成するファクトリ
        protected readonly Dictionary<Type, Func<object>> Factory = new Dictionary<Type, Func<object>>();
        //作成したインスタンスのキャッシュ
        private readonly Dictionary<Type, object> _instance = new Dictionary<Type, object>();
        
        /// <summary>
        /// 型からインスタンスを返します。<br/>
        /// キャッシュインスタンスか新しいインスタンスを返すかはInstanceContextのスコープに依存します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T GetInstance<T>()
        {
            var type = typeof(T);

            //インスタンスキャッシュから探す
            object o;
            if (_instance.TryGetValue(type, out o))
            {
                return (T)o;
            }

            //見つからないのでファクトリを探す
            Func<object> func;
            if (!Factory.TryGetValue(type, out func))
            {
                return default(T);
            }
            //見つかればファクトリからインスタンスを初期化
            o = func?.Invoke();
            if (o is IInitializable)
            {
                var initializable = (IInitializable) o;
                initializable.Initialize();
            }

            _instance.Add(type, o);
            return (T) o;
        }

        /// <summary>
        /// 全てのインスタンスを作成し、インスタンスキャッシュに詰める
        /// </summary>
        public virtual void Initialize()
        {
            ClearCacheAll();
            foreach (var keyValuePair in Factory)
            {
                _instance.Add(keyValuePair.Key, keyValuePair.Value.Invoke());
            }
        }

        /// <summary>
        /// 初期化メソッドを呼び出す
        /// </summary>
        public virtual void CallInitializeAll()
        {
            foreach (var value in _instance.Values)
            {
                (value as IInitializable)?.Initialize();
            }
        }

        /// <summary>
        /// 全てのインスタンスキャッシュを削除します。
        /// </summary>
        public void ClearCacheAll()
        {
            //インスタンスの後処理
            foreach (var disposable in _instance.Values.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
            _instance.Clear();
        }

        /// <summary>
        /// 指定の型のインスタンスキャッシュを削除します。
        /// </summary>
        /// <param name="type"></param>
        public void ClearChacheByType(Type type)
        {
            _instance.Remove(type);
        }

        public void RegisterFactory<T>(Func<T> instanceFactory) where T : class
        {
            Factory.Add(typeof(T), instanceFactory);
        }
    }

    class PrototypeInstanceContext : InstanceContext
    {
        public override T GetInstance<T>()
        {
            Func<object> func;
            if (!Factory.TryGetValue(typeof(T), out func))
            {
                return default(T);
            }

            var obj =  func.Invoke();

            if (!(obj is IInitializable)) return (T) obj;

            var initializable = (IInitializable)obj;
            initializable.Initialize();
            return (T) obj;
        }

        public override void Initialize()
        {
            //何もしない
        }

        public override void CallInitializeAll()
        {
            //何もしない
        }
    }

    interface IInitializable
    {
        void Initialize();
    }
}

﻿using DesktopCharacter.ViewModel;
using SharpGL;
using SharpGL.SceneGraph;
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
using BabumiGraphics.Graphics;
using BabumiGraphics.Live2D;
using BabumiGraphics;
using DesktopCharacter.Model.Locator;
using DesktopCharacter.Model.Repository;
using DesktopCharacter.Model.Database.Domain;
using System.IO;

namespace DesktopCharacter.View
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class Character : Window
    {
        /// <summary>
        /// スクリーンサイズ
        /// </summary>
        System.Drawing.Point mScreenSize;
        /// <summary>
        /// xamlに渡すための書き込み先ビットマップ
        /// </summary>
        WriteableBitmap mBitmapSource;
        /// <summary>
        /// 書き込み範囲（newの節約のためここでインスタンス化させる）
        /// </summary>
        Int32Rect mSourceRect;
        /// <summary>
        /// レンダリングターゲット（テクスチャーに描画させるため）
        /// </summary>
        RenderTarget mRenderTarget;
        /// <summary>
        /// Texture -> Bitmapのカラー情報
        /// </summary>
        Shader mComputeShader;
        /// <summary>
        /// GLSLで使うBitmapのカラー情報格納先
        /// </summary>
        SSBObject mSSBObject;
        /// <summary>
        /// Live2Dの管理クラス
        /// </summary>
        Manager mLive2DManager = new Manager();
        /// <summary>
        /// デスクトップマスコットを利用するために必要なバージョン
        /// </summary>
        static double RequiredVersion = 4.3;
        /// <summary>
        /// Live2Dのリソースディレクトリパス
        /// </summary>
        static string Live2DResoruceDir = "Res/Live2D";
        /// <summary>
        /// ロードするデータ名
        /// </summary>
        string LoadDataName { get; set; }

        public Character()
        {
            InitializeComponent();
            //!< 保存したPositionを取り出し設定する
            {
                var repo = ServiceLocator.Instance.GetInstance<WindowPositionRepository>();
                var pos = repo.FetchPosition();
                Top = pos.PosY;
                Left = pos.PosX;
            }
            //!< Live2Dディレクトリにランダムから選出する
            //!< フォルダーがもしない場合はそのこと通知して終了する
            {
                var repo = ServiceLocator.Instance.GetInstance<CharacterDataRepository>();
                LoadDataName = repo.GetDataName();
                if (LoadDataName == null)
                {
                    string[] dirs = Directory.GetDirectories(Live2DResoruceDir);
                    if (dirs.Length == 0)
                    {                 //!< GLのバージョンを表示してアプリケーションを終了する
                        MessageBox.Show(string.Format("[ ERROR ]\n{0}のパスにLive2Dのモデルデータが入っていない可能性があります。確認してみてください。", Live2DResoruceDir));
                        //!< アプリケーションを終了する
                        this.Close();
                    }
                    LoadDataName = System.IO.Path.GetFileName(dirs[new System.Random().Next(dirs.Length - 1)]);
                    //!< 次回からこちらをロードする
                    repo.Save(new CharacterData(LoadDataName));
                }
            }

            var userVM = this.menuItem.DataContext as ViewModel.Menu.MenuItemViewModel;
            userVM.CharacterVM = this.DataContext as CharacterViewModel;
            var characterVM = this.DataContext as ViewModel.CharacterViewModel;
            //characterVM.mTalkViewModel = this.TalkView.DataContext as ViewModel.TalkViewModel;

            mScreenSize = new System.Drawing.Point { X = (int)this.Width, Y = (int)this.Height };
            mBitmapSource = new WriteableBitmap(mScreenSize.X, mScreenSize.Y, 96, 96, PixelFormats.Bgra32, null);
            mSourceRect = new Int32Rect(0, 0, mScreenSize.X, mScreenSize.Y);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            var repo = ServiceLocator.Instance.GetInstance<WindowPositionRepository>();
            repo.Save((int)Left, (int)Top);
        }

        /// <summary>
        /// Handles the OpenGLDraw event of the OpenGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
        private void OpenGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
            //!< Debugの時だけバージョンチェックをする
            if(RequiredVersion > GraphicsManager.Instance.GetVersion())
            {
                return; //!< GL4.3以下なので処理をしない
            }

            OpenGL gl = args.OpenGL;
            GraphicsManager Manager = GraphicsManager.Instance;
            Manager.Device = args.OpenGL;

            Manager.Begin();
            {
                mLive2DManager.Update(mScreenSize.X, mScreenSize.Y);
            }
            Manager.End();

            Manager.SetEffect(mComputeShader);
            Manager.Uniform("Width", (float)mScreenSize.X);
            Manager.Uniform("Height", (float)mScreenSize.Y);
            Manager.UniformTexture(mRenderTarget.TextureID, 0);
            Manager.UniformBuffer(mSSBObject.Buffer, 1, OpenGL.GL_SHADER_STORAGE_BUFFER);
            Manager.Dispatch((uint)mScreenSize.X / 16, (uint)mScreenSize.Y / 16, 1);
            {
                var stride = this.mScreenSize.Y * 8 * 0.5;
                IntPtr ptr = Manager.Map(OpenGL.GL_SHADER_STORAGE_BUFFER, mSSBObject.Buffer);
                mBitmapSource.WritePixels(
                    mSourceRect,
                    ptr,
                    (int)stride * mScreenSize.Y,
                    (int)stride);
                Manager.UnMap(OpenGL.GL_SHADER_STORAGE_BUFFER);
            }
            this.Image.Source = mBitmapSource;
        }



        /// <summary>
        /// Handles the OpenGLInitialized event of the OpenGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
        private void OpenGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            GraphicsManager.Instance.Device = args.OpenGL;
            GraphicsManager.Instance.Initialize();
            //!< Debugの時だけバージョンチェックをする
            if (RequiredVersion > GraphicsManager.Instance.GetVersion())
            {
                return; //!< GL4.3以下なので処理をしない
            }
            mLive2DManager.Load(Live2DResoruceDir, LoadDataName, string.Format("{0}.model.json", LoadDataName));

            mRenderTarget = new RenderTarget { Width = (uint)mScreenSize.X, Height = (uint)mScreenSize.Y };
            mRenderTarget.Create();

            mSSBObject = new SSBObject();
            mSSBObject.Create(1000000 * sizeof(uint));

            mComputeShader = new Shader();
            mComputeShader.CreateShader("Res/computeShader.fx", Shader.Type.ComputeShader);
            mComputeShader.Attach();

            GraphicsManager.Instance.SetRenderTarget(mRenderTarget);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //!< Debugの時だけバージョンチェックをする
            if (RequiredVersion > GraphicsManager.Instance.GetVersion())
            {
                //!< GLのバージョンを表示してアプリケーションを終了する
                MessageBox.Show(string.Format(
                    "[ ERROR ]\nGL_VENDOR: {0} \nGL_RENDERER : {1} \nGL_VERSION : {2} \nOpenGLのバージョンが4.3以下です！コンピュートシェーダに対応していないため終了します",
                    GraphicsManager.Instance.mVender,
                    GraphicsManager.Instance.mRender,
                    GraphicsManager.Instance.mVersion));
                //!< アプリケーションを終了する
                this.Close();
            }
        }
    }
}

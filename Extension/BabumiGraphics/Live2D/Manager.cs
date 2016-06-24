﻿using Codeplex.Data;
using Live2DWrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BabumiGraphics.Utility;

namespace BabumiGraphics.Live2D
{
    public class Manager
    {
        private Live2DMotionQueueManager mAnimationQueue = null;

        private AnimationContainer mAnimationContainer = new AnimationContainer();
        private Model mModel = new Model();

        private List<Tuple<string, bool>> mRegisterAnimationQueue = new List<Tuple<string, bool>>();

        public Manager()
        {
            Live2DWrapping.init();
        }

        public void Load( string prefix, string modeljson )
        {
            mAnimationQueue = new Live2DMotionQueueManager();

            JsonObject modelObject = new JsonObject();
            JsonObject physicsObject = null;
            //!< ModelJsonの読み込み
            modelObject.LoadJson(prefix + "\\" + modeljson);
            
            {
                var data = modelObject.LoadObject;
                //!< モデル読み込み
                {
                    mModel.LoadModel(prefix, data.model, data.textures);
                }
                //!< アニメーション読み込み
                {
                    foreach ( var motion in (dynamic[])data.motions )
                    {
                        //System.Console.WriteLine(t.Keys);
                        foreach (var file in (dynamic[])motion.Value)
                        {
                            string path = prefix + "\\" + file.file;
                            var animation = new Live2DAnimation();
                            animation.loadMotion(path);
                            mAnimationContainer.Add(Path.GetFileNameWithoutExtension(path), animation);
                        }
                    }
                }
                //<! 物理ファイルを読み込む
                if (data.physics != "")
                {
                    physicsObject = new JsonObject();
                    physicsObject.LoadJson(prefix + "\\" + data.physics);
                }
            }

            if(physicsObject != null)
            {
                //!< PhysicsJson読み込み
                var data = physicsObject.LoadObject;
            }
        }

        public void Update( int Width, int Height )
        {
            bool animationFinish = mAnimationQueue.isFinished();

            //!< 登録されたアニメーションがなければアイドルアニメーションへ
            if (IsEmpty())
            {
                if (animationFinish)
                {
                    mAnimationQueue.startMotion(mAnimationContainer.GetAnimation("idle"), false);
                }
            }
            else
            {
                var animation = Front();
                //!< 即座に切り替える
                if (animation.Item2)
                {
                    mAnimationQueue.startMotion(mAnimationContainer.GetAnimation(animation.Item1), false);
                    //!< 切り替えたので削除する
                    Remove();
                }
                else
                {
                    //!< アニメーションが終了してから切り替える
                    if (animationFinish)
                    {
                        mAnimationQueue.startMotion(mAnimationContainer.GetAnimation(animation.Item1), false);
                        //!< 切り替えたので削除する
                        Remove();
                    }
                }
            }
            mAnimationQueue.updateParam(mModel.ModelObject);
            mModel.Update( Width, Height );
        }

        /// <summary>
        /// アニメーションセット
        /// </summary>
        /// <param name="name">アニメーション名</param>
        /// <param name="finish">即座に反映orアニメーションが終わったあとに反映のどちらか</param>
        public void SetAnimation(string name, bool finish)
        {
            mRegisterAnimationQueue.Add(new Tuple<string, bool>(name, finish));
        }

        private bool IsEmpty()
        {
            return mRegisterAnimationQueue.Count == 0;
        }

        private Tuple<string, bool> Front()
        {
            return mRegisterAnimationQueue[0];
        }

        private void Remove()
        {
            mRegisterAnimationQueue.RemoveAt(0);
        }
    }
}

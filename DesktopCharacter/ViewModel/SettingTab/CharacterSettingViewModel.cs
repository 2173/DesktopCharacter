﻿using DesktopCharacter.Model;
using Livet.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopCharacter.ViewModel.SettingTab
{
    class CharacterName : Livet.NotificationObject
    {
        public CharacterName(string name)
        {
            _fileName = name;
        }

        /// <summary>
        /// 選択項目 選択された場合trueになる
        /// </summary>
        private bool mIsSelected;
        public bool IsSelected
        {
            get { return mIsSelected; }
            set
            {
                mIsSelected = value;
                if (mIsSelected)
                {
                    CharacterPropertyNotify.Instance.CharacterLoad(_fileName);
                }
                this.RaisePropertyChanged("IsSelected");
            }
        }

        /// <summary>
        /// ファイル名
        /// </summary>
        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; this.RaisePropertyChanged("FileName"); }
        }
    }

    class CharacterSettingViewModel : Livet.ViewModel
    {
        /// <summary>
        /// ListViewの一覧に出るコレクション要素
        /// </summary>
        private ObservableCollection<CharacterName> _listCollection;
        public ObservableCollection<CharacterName> ListCollection
        {
            get { return this._listCollection; }
        }

        public CharacterSettingViewModel()
        {
            _listCollection = new ObservableCollection<CharacterName>();
            Reload();
        }

        private void Reload()
        {
            _listCollection.Clear();
            string[] dirs = Directory.GetDirectories(CharacterManaged.Live2DResoruceDir);
            if (dirs.Length == 0)
            {
                return;
            }
            foreach (var name in dirs)
            {
                _listCollection.Add(new CharacterName(System.IO.Path.GetFileName(name)));
            }
        }

        private ViewModelCommand _reloadCommand;
        public ViewModelCommand ReloadCommand
        {
            get
            {
                if (_reloadCommand == null)
                {
                    _reloadCommand = new ViewModelCommand(() =>
                    {
                        Reload();
                    });
                }
                return _reloadCommand;
            }
        }
    }
}

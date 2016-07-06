using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AperyGenerateTeacherGUI.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace AperyGenerateTeacherGUI.ViewModels
{
    internal class ShellViewModel
    {
        public ReactiveProperty<string> FileCheckStatus { get; private set; }
        public short ProcessorThread { get; set; }
        public int ThreadMaxValue { get; set; }
        public long TeacherNodes { get; set; }
        public ReactiveProperty<string> ButtonContent { get; set; }
        public ReactiveProperty<double> Progress { get; private set; }

        private Apery _apery { get; }
        private FileCheck _fileCheck { get; }

        public ReactiveProperty<string> LogText { get; private set; }

        public ReactiveCommand ButtonClickCommand { get; private set; }

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        private ReactiveProperty<bool> _aperyState;
        private ReactiveProperty<bool> _isFileCheckCompleted;
        private ReactiveProperty<bool> _areFilesOk;

        public ReactiveProperty<string> AlertMessage { get; private set; }

        public ShellViewModel()
        {
            this._fileCheck = new FileCheck();

            this._isFileCheckCompleted = this._fileCheck
                .ObserveProperty(o => o.CheckCompletedAndOk)
                .Select(x => x.Item1)
                .ToReactiveProperty()
                .AddTo(this._disposable);

            this.FileCheckStatus = this._isFileCheckCompleted
                .Select(x => x ? "" : "ファイル確認中")
                .ToReactiveProperty();

            this.AlertMessage = this._fileCheck
                .ObserveProperty(o => o.CheckCompletedAndOk)
                .Select(x => (x.Item1 && !x.Item2) ? "ファイルが破損しています。ダウンロードしなおしてください。" : "")
                .ToReactiveProperty()
                .AddTo(this._disposable);

            this._areFilesOk = this._fileCheck
                .ObserveProperty(o => o.CheckCompletedAndOk)
                .Select(x => x.Item2)
                .ToReactiveProperty()
                .AddTo(this._disposable);

            this._apery = Apery.Instance;

            this._aperyState = this._apery
                .ObserveProperty(o => o.IsAperyIdle)
                .ToReactiveProperty()
                .AddTo(this._disposable);

            this._disposable.Add(this._apery);

            this.TeacherNodes = 1000000;
            this.ProcessorThread = 1;
            this.ThreadMaxValue = GetProcessorCount.Count();

            this.ButtonContent = this._aperyState
                .Select(x => x ? "作成開始" : "作成中")
                .ToReactiveProperty()
                ;

            this.LogText = this._apery
                .ObserveProperty(o => o.Log)
                .ToReactiveProperty()
                .AddTo(this._disposable);

            this.Progress = this._apery
                .ObserveProperty(o => o.Progress)
                .ToReactiveProperty()
                .AddTo(this._disposable);

            this.ButtonClickCommand = new[] { this._aperyState, this._isFileCheckCompleted, this._areFilesOk }
            .CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            ;

            this.ButtonClickCommand.Subscribe(_ => this._apery.RunProcess(ProcessorThread, TeacherNodes));
        }

        ~ShellViewModel()
        {
            this._disposable.Dispose();
        }
    }
}

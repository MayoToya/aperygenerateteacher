using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AperyGenerateTeacherGUI.Models;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

namespace AperyGenerateTeacherGUI.ViewModels
{
    internal class ShellViewModel
    {
        public ReactiveProperty<string> FileCheckStatus { get; private set; }
        public short ProcessorThread { get; set; }
        public int ThreadMaxValue { get; }
        public long TeacherNodes { get; set; }
        public ReactiveProperty<string> ButtonContent { get; private set; }
        public ReadOnlyReactiveProperty<double> Progress { get; private set; }
        private Apery _apery { get; }
        private FileCheck _fileCheck { get; }
        public ReadOnlyReactiveProperty<string> LogText { get; private set; }
        public ReactiveCommand ButtonClickCommand { get; }
        private ReadOnlyReactiveProperty<Tuple<bool, bool>> _aperyState;
        private ReadOnlyReactiveProperty<bool> _isAperyReady;
        private ReadOnlyReactiveProperty<bool> _isFileCheckCompleted;
        private ReadOnlyReactiveProperty<bool> _areFilesOk;
        private IObservable<Tuple<bool, bool>> _fileCheckObserveProperty;
        public ReactiveProperty<string> AlertMessage { get; private set; }
        private CompositeDisposable _disposable { get; } = new CompositeDisposable();
        private ReadOnlyReactiveProperty<Tuple<bool, bool>> _fileCheckReadOnlyReactiveProperty { get; }

        public ShellViewModel()
        {
            this._fileCheck = new FileCheck();

            this._fileCheckObserveProperty = this._fileCheck
                .ObserveProperty(o => o.CheckCompletedAndOk);

            this._fileCheckReadOnlyReactiveProperty = this._fileCheck
                .ObserveProperty(o => o.CheckCompletedAndOk)
                .ToReadOnlyReactiveProperty()
                .AddTo(this._disposable);

            this._isFileCheckCompleted = this._fileCheckReadOnlyReactiveProperty
                .Select(x => x.Item1)
                .ToReadOnlyReactiveProperty()
                .AddTo(this._disposable);

            this._areFilesOk = this._fileCheckReadOnlyReactiveProperty
                .Select(x => x.Item2)
                .ToReadOnlyReactiveProperty()
                .AddTo(this._disposable);

            this.FileCheckStatus = this._fileCheckReadOnlyReactiveProperty
                .Select(x => x.Item1 ? "" : "ファイル確認中")
                .ToReactiveProperty()
                .AddTo(this._disposable);

            this.AlertMessage = this._fileCheckReadOnlyReactiveProperty
                .Select(x => (x.Item1 && !x.Item2) ? "ファイルが破損しています。ダウンロードしなおしてください。" : "")
                .ToReactiveProperty()
                .AddTo(this._disposable);

            this._apery = Apery.Instance;
            this._disposable.Add(this._apery);

            this._apery.Run(this._fileCheck.IsAperyGood);

            this._aperyState = this._apery
                .ObserveProperty(o => o.IsAperyIdleAndStarted)
                .ToReadOnlyReactiveProperty()
                .AddTo(this._disposable);

            this.LogText = this._apery
                .ObserveProperty(o => o.Log)
                .ToReadOnlyReactiveProperty()
                .AddTo(this._disposable);

            this.Progress = this._apery
                .ObserveProperty(o => o.Progress)
                .ToReadOnlyReactiveProperty()
                .AddTo(this._disposable);


            this.ButtonContent = this._aperyState
                .Select(x =>
                {
                    if (x.Item1 & x.Item2)
                    {
                        return "作成開始";
                    }

                    if (!x.Item1 & x.Item2)
                    {
                        return "作成中";
                    }

                    if (!x.Item1 & !x.Item2)
                    {
                        return "Aperyに問題有";
                    }

                    return "";
                })
                .ToReactiveProperty()
                ;

            this.ButtonClickCommand = new[] { this._aperyState.Select(x => x.Item1), this._aperyState.Select(x => x.Item2), this._isFileCheckCompleted, this._areFilesOk }
            .CombineLatestValuesAreAllTrue()
            .ToReactiveCommand(false)
            ;

            this.TeacherNodes = 1000000;
            this.ProcessorThread = 1;
            this.ThreadMaxValue = GetProcessorCount.Count();

            this.ButtonClickCommand.Subscribe(_ => this._apery.RunProcess(ProcessorThread, TeacherNodes));
        }

        ~ShellViewModel()
        {
            this._disposable.Dispose();
        }
    }
}

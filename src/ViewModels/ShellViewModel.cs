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

        public short ProcessorThread { get; set; }
        public int ThreadMaxValue { get; set; }
        public long TeacherNodes { get; set; }
        public ReactiveProperty<string> ButtonContent { get; set; }

        private Apery _apery { get; }

        public ReactiveProperty<string> LogText { get; private set; }

        public ReactiveCommand ButtonClickCommand { get; private set; }

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();


        public ShellViewModel()
        {
            this._apery = Apery.Instance;
            this._disposable.Add(this._apery);

            this.TeacherNodes = 1000000;
            this.ProcessorThread = 1;
            this.ThreadMaxValue = GetProcessorCount.Count();

            this.ButtonContent = this._apery
                .ObserveProperty(o => o.IsAperyIdle)
                .Select(x => x ? "作成開始" : "作成中")
                .ToReactiveProperty()
                .AddTo(this._disposable);

            this.LogText = this._apery
                .ObserveProperty(o => o.Log)
                .ToReactiveProperty()
                .AddTo(this._disposable);


            this.ButtonClickCommand = this._apery
                .ObserveProperty(o => o.IsAperyIdle)
                .ToReactiveCommand()
                .AddTo(this._disposable);


            this.ButtonClickCommand.Subscribe(_ => this._apery.RunProcessAsync(ProcessorThread, TeacherNodes));

        }

        ~ShellViewModel()
        {
            this._disposable.Dispose();
        }
    }
}

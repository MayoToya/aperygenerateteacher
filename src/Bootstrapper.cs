using AperyGenerateTeacherGUI.Views;
using Microsoft.Practices.Unity;
using Prism.Unity;
using System.Windows;

namespace AperyGenerateTeacherGUI
{
    class Bootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            // this.ContainerでUnityのコンテナが取得できるので
            // そこからShellを作成する
            return this.Container.Resolve<Shell>();
        }

        protected override void InitializeShell()
        {
            // Shellを表示する
            ((Window)this.Shell).Show();
        }

    }
}

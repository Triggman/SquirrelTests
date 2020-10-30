using Prism.Services.Dialogs;
using RemoteVisionConsole.Module.ChangeTracking;
using RemoteVisionConsole.Module.Models;
using System.Collections.Generic;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class CommitViewerDialogViewModel : DialogViewModelBase
    {
        private IEnumerable<Commit> _commits;

        public IEnumerable<Commit> Commits
        {
            get => _commits;
            set => SetProperty(ref _commits, value);
        }


        public override void OnDialogOpened(IDialogParameters parameters)
        {
            Commits = parameters.GetValue<IEnumerable<Commit>>("Commits");

            base.OnDialogOpened(parameters);
        }

    }

    public class CommitViewerDialogViewModel_DesignTime : CommitViewerDialogViewModel
    {
        public CommitViewerDialogViewModel_DesignTime()
        {
            Commits = new[] { new Commit_DesignTime(), new Commit_DesignTime(), };
        }
    }
}

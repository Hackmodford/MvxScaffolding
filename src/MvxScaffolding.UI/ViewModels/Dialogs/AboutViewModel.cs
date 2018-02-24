﻿//---------------------------------------------------------------------------------
// Copyright © 2018, Jonathan Froon, Plac3hold3r+github@outlook.com
// MvxScaffolding is licensed using the MIT License
//---------------------------------------------------------------------------------

using System.Windows.Input;
using MvxScaffolding.Core.Configuration;
using MvxScaffolding.Core.Template;
using MvxScaffolding.UI.Commands;

namespace MvxScaffolding.UI.ViewModels.Dialogs
{
    public class AboutViewModel : BaseViewModel
    {
        public ICommand GoToGitHubCommand { get; }
        public ICommand GoToAuthorGitHubCommand { get; }
        public ICommand GoToPrivacyPolicyCommand { get; }

        public AboutViewModel()
        {
            GoToGitHubCommand = new RelayCommand(GoToGitHubLink);
            GoToAuthorGitHubCommand = new RelayCommand(GoToAuthorGitHubLink);
            GoToPrivacyPolicyCommand = new RelayCommand(GoToPrivacyPolicyLink);
        }

        private void GoToGitHubLink()
        {
            OpenLink(Config.Current.GitHubUri, TemplateLinks.GitHub);
        }

        private void GoToAuthorGitHubLink()
        {
            OpenLink(Config.Current.AuthorGitHubUri, TemplateLinks.AuthorGitHub);
        }

        private void GoToPrivacyPolicyLink()
        {
            OpenLink(Config.Current.PrivacyPolicyUri, TemplateLinks.PrivacyPolicy);
        }
    }
}

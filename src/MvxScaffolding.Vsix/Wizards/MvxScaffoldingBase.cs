//---------------------------------------------------------------------------------
// Copyright © 2018, Jonathan Froon, Plac3hold3r+github@outlook.com
// MvxScaffolding is licensed using the MIT License
//---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using MvxScaffolding.Core.Contexts;
using MvxScaffolding.Core.Diagnostics;
using MvxScaffolding.Core.Files;
using MvxScaffolding.Core.Tasks;
using MvxScaffolding.Core.Template;
using MvxScaffolding.UI;
using MvxScaffolding.UI.Threading;
using MvxScaffolding.UI.Utils;
using MvxScaffolding.Vsix.Constants;

namespace MvxScaffolding.Vsix.Wizards
{
    public abstract class MvxScaffoldingBase : IWizard
    {
        protected MvxScaffoldingBase(TemplateType templateType)
        {
            MvxScaffoldingContext.CurrentTemplateType = templateType;
        }

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
            // Method intentionally left empty.
        }

        public void ProjectFinishedGenerating(Project project)
        {
            // Method intentionally left empty.
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            // Method intentionally left empty.
        }

        public void RunFinished()
        {
            // Method intentionally left empty.
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            if (runKind == WizardRunKind.AsNewProject || runKind == WizardRunKind.AsMultiProject)
            {
                try
                {
                    MvxScaffoldingContext.WizardVersion = new Version(ThisAssembly.Vsix.Version);
                    MvxScaffoldingContext.WizardName = ThisAssembly.Vsix.Name;
                    MvxScaffoldingContext.ProjectName = replacementsDictionary[VSTemplateKeys.ProjectName];
                    MvxScaffoldingContext.SafeProjectName = replacementsDictionary[VSTemplateKeys.SafeProjectName];
                    MvxScaffoldingContext.CanCreateSolutionDirectory = !string.IsNullOrWhiteSpace(replacementsDictionary[VSTemplateKeys.SpecifiedSolutionName]);
                    MvxScaffoldingContext.SolutionName = replacementsDictionary[VSTemplateKeys.SpecifiedSolutionName];

                    MvxScaffoldingContext.RemoveOldSolutionDirectoryStatus = RemoveOldSolutionDirectory(automationObject, replacementsDictionary);

                    ShowModal(Startup.FirstView());

                    MvxScaffoldingContext.RunningTimer.Stop();

                    if (MvxScaffoldingContext.UserSelectedOptions is null)
                    {
                        Logger.Current.Telemetry.TrackWizardCancelledAsync(MvxScaffoldingContext.RunningTimer.Elapsed.TotalSeconds)
                            .FireAndForget();

                        throw new WizardBackoutException();
                    }
                    else
                    {
                        Logger.Current.Telemetry.TrackProjectGenAsync(MvxScaffoldingContext.UserSelectedOptions, MvxScaffoldingContext.RunningTimer.Elapsed.TotalSeconds)
                            .FireAndForget();

                        AddParameters(replacementsDictionary);
                        UpdateReplacementsDictionary(replacementsDictionary);

                        MvxScaffoldingContext.UserSelectedOptions = null;
                    }
                }
                catch (Exception ex) when (ex.GetType() != typeof(WizardBackoutException))
                {
                    Logger.Current.Exception.TrackAsync(ex, "Error running wizard")
                        .FireAndForget();

                    ExceptionHandler.ShowErrorDialog(ex);

                    throw new WizardBackoutException("Error running wizard, closing application", ex);
                }
                finally
                {
                    Logger.Current.Telemetry.TrackEndSessionAsync()
                        .FireAndForget();
                }
            }
        }

        private static FileDeleteStatus RemoveOldSolutionDirectory(object automationObject, Dictionary<string, string> replacementsDictionary)
        {
            var dte = (DTE)automationObject;
            var solution = (Solution2)dte.Solution;
            solution.Close();

            var oldDestinationDirectory = replacementsDictionary[VSTemplateKeys.DestinationDirectory];

            var solutionRootDirectory = string.IsNullOrWhiteSpace(replacementsDictionary[VSTemplateKeys.SpecifiedSolutionName])
                ? oldDestinationDirectory
                : Path.GetFullPath(Path.Combine(oldDestinationDirectory, @"..\"));

            FileDeleteStatus deleteStatus = FileSystemUtils.SafeDeleteDirectory(solutionRootDirectory);

            var rootFolderDictionary = Path.GetFullPath(Path.Combine(solutionRootDirectory, @"..\"));
            replacementsDictionary[VSTemplateKeys.DestinationDirectory] = rootFolderDictionary;
            replacementsDictionary[VSTemplateKeys.SolutionDirectory] = rootFolderDictionary;

            return deleteStatus;
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        protected virtual void AddParameters(Dictionary<string, string> replacementsDictionary)
        {
            replacementsDictionary.AddParameter(TemplateOptions.HasAndroidProject, MvxScaffoldingContext.UserSelectedOptions.HasAndroid);
            replacementsDictionary.AddParameter(TemplateOptions.HasIosProject, MvxScaffoldingContext.UserSelectedOptions.HasIos);
            replacementsDictionary.AddParameter(TemplateOptions.HasUwpProject, MvxScaffoldingContext.UserSelectedOptions.HasUwp);

            replacementsDictionary.AddParameter(TemplateOptions.HasCoreTestProject, MvxScaffoldingContext.UserSelectedOptions.HasCoreUnitTestProject);
            replacementsDictionary.AddParameter(TemplateOptions.HasAndroidTestProject, MvxScaffoldingContext.UserSelectedOptions.HasAndroidUnitTestProject);
            replacementsDictionary.AddParameter(TemplateOptions.HasIosTestProject, MvxScaffoldingContext.UserSelectedOptions.HasIosUnitTestProject);
            replacementsDictionary.AddParameter(TemplateOptions.HasUwpTestProject, MvxScaffoldingContext.UserSelectedOptions.HasUwpUnitTestProject);
            replacementsDictionary.AddParameter(TemplateOptions.HasUwpUITestProject, MvxScaffoldingContext.UserSelectedOptions.HasUwpUiTestProject);

            replacementsDictionary.AddParameter(TemplateOptions.HasEditorConfig, MvxScaffoldingContext.UserSelectedOptions.HasEditorConfig);
            replacementsDictionary.AddParameter(TemplateOptions.SolutionProjectGrouping, MvxScaffoldingContext.UserSelectedOptions.SelectedProjectGrouping);

            replacementsDictionary.AddParameter(TemplateOptions.AppId, MvxScaffoldingContext.UserSelectedOptions.AppId);
            replacementsDictionary.AddParameter(TemplateOptions.AppName, MvxScaffoldingContext.UserSelectedOptions.AppName);
            replacementsDictionary.AddParameter(TemplateOptions.SolutionName, MvxScaffoldingContext.UserSelectedOptions.SolutionName);
            replacementsDictionary.AddParameter(TemplateOptions.NetStandardVersion, MvxScaffoldingContext.UserSelectedOptions.SelectedNetStandard);

            replacementsDictionary.AddParameter(TemplateOptions.AndroidMinSdkVersion, MvxScaffoldingContext.UserSelectedOptions.SelectedMinAndroidSDK);

            replacementsDictionary.AddParameter(TemplateOptions.IosMinSdkVersion, MvxScaffoldingContext.UserSelectedOptions.SelectedMinIosSDK);

            replacementsDictionary.AddParameter(TemplateOptions.UwpMinSdkVersion, MvxScaffoldingContext.UserSelectedOptions.SelectedMinUwpSDK);
            replacementsDictionary.AddParameter(TemplateOptions.UwpAppDescription, MvxScaffoldingContext.UserSelectedOptions.UwpDescription);

            replacementsDictionary.AddParameter(TemplateOptions.ScaffoldType, MvxScaffoldingContext.UserSelectedOptions.SelectedScaffoldType.ScaffoldType.AsParameter());
        }

        public void UpdateReplacementsDictionary(Dictionary<string, string> replacementsDictionary)
        {
            if (MvxScaffoldingContext.UserSelectedOptions.CanCreateSolutionDirectory)
            {
                replacementsDictionary[VSTemplateKeys.SpecifiedSolutionName] = MvxScaffoldingContext.UserSelectedOptions.SolutionName;
                replacementsDictionary[VSTemplateKeys.SolutionDirectory] += MvxScaffoldingContext.UserSelectedOptions.SolutionName + "\\";
                replacementsDictionary[VSTemplateKeys.DestinationDirectory] += MvxScaffoldingContext.UserSelectedOptions.SolutionName + "\\";
            }
            else
            {
                replacementsDictionary[VSTemplateKeys.SpecifiedSolutionName] = "";
            }
        }

        public void ShowModal(System.Windows.Window dialog)
        {
            SafeThreading.JoinableTaskFactory.Run(async delegate
            {
                await SafeThreading.JoinableTaskFactory.SwitchToMainThreadAsync();

                UIShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

                Assumes.Present(UIShell);
                UIShell.GetDialogOwnerHwnd(out IntPtr hwnd);

                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                UIShell.EnableModeless(0);

                try
                {
                    WindowHelper.ShowModal(dialog, hwnd);
                }
                finally
                {
                    UIShell.EnableModeless(1);
                }
            });
        }

        protected IVsUIShell UIShell { get; private set; }
    }
}

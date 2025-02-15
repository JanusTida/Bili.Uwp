﻿// Copyright (c) Richasy. All rights reserved.

using Bili.ViewModels.Interfaces.Home;
using ReactiveUI;
using Splat;

namespace Bili.App.Controls
{
    /// <summary>
    /// 问题面板.
    /// </summary>
    public sealed partial class QuestionPanel : QuestionPanelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuestionPanel"/> class.
        /// </summary>
        public QuestionPanel()
        {
            InitializeComponent();
            ViewModel = Splat.Locator.Current.GetService<IHelpPageViewModel>();
        }
    }

    /// <summary>
    /// <see cref="QuestionPanel"/> 的基类.
    /// </summary>
    public class QuestionPanelBase : ReactiveUserControl<IHelpPageViewModel>
    {
    }
}

﻿// Copyright (c) Richasy. All rights reserved.

using Bili.ViewModels.Interfaces.Core;
using Bili.ViewModels.Interfaces.Home;
using ReactiveUI;
using Splat;

namespace Bili.App.Controls
{
    /// <summary>
    /// 设置区块基类.
    /// </summary>
    public class SettingSectionControl : ReactiveUserControl<ISettingsPageViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingSectionControl"/> class.
        /// </summary>
        public SettingSectionControl()
        {
            ViewModel = Locator.Current.GetService<ISettingsPageViewModel>();
            IsTabStop = false;
        }

        /// <summary>
        /// 核心视图模型.
        /// </summary>
        public IAppViewModel CoreViewModel { get; } = Locator.Current.GetService<IAppViewModel>();
    }
}

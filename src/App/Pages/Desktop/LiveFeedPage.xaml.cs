﻿// Copyright (c) Richasy. All rights reserved.

using Bili.App.Pages.Base;

namespace Bili.App.Pages.Desktop
{
    /// <summary>
    /// 直播推荐页面.
    /// </summary>
    public sealed partial class LiveFeedPage : LiveFeedPageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LiveFeedPage"/> class.
        /// </summary>
        public LiveFeedPage() => InitializeComponent();

        /// <inheritdoc/>
        protected override void OnPageLoaded()
            => Bindings.Update();

        /// <inheritdoc/>
        protected override void OnPageUnloaded()
            => Bindings.StopTracking();
    }
}

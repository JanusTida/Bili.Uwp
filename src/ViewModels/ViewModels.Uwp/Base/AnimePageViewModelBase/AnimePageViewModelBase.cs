﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Bili.Lib.Interfaces;
using Bili.Models.App.Args;
using Bili.Models.App.Other;
using Bili.Models.Data.Community;
using Bili.Models.Data.Pgc;
using Bili.Models.Enums;
using Bili.Models.Enums.App;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces.Common;
using Bili.ViewModels.Interfaces.Core;
using Bili.ViewModels.Interfaces.Pgc;
using Bili.ViewModels.Interfaces.Video;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace Bili.ViewModels.Uwp.Base
{
    /// <summary>
    /// 动漫页面视图模型基类.
    /// </summary>
    public partial class AnimePageViewModelBase : ViewModelBase, IAnimePageViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnimePageViewModelBase"/> class.
        /// </summary>
        /// <param name="pgcProvider">PGC 服务提供工具.</param>
        /// <param name="authorizeProvider">授权服务提供工具.</param>
        /// <param name="homeProvider">主页数据服务提供工具.</param>
        /// <param name="resourceToolkit">资源管理工具.</param>
        /// <param name="type">所属类型.</param>
        internal AnimePageViewModelBase(
            IPgcProvider pgcProvider,
            IAuthorizeProvider authorizeProvider,
            IHomeProvider homeProvider,
            IResourceToolkit resourceToolkit,
            INavigationViewModel navigationViewModel,
            PgcType type)
        {
            _pgcProvider = pgcProvider;
            _authorizeProvider = authorizeProvider;
            _homeProvider = homeProvider;
            _resourceToolkit = resourceToolkit;
            _navigationViewModel = navigationViewModel;
            _type = type;

            Title = _type switch
            {
                PgcType.Bangumi => _resourceToolkit.GetLocaleString(LanguageNames.Bangumi),
                PgcType.Domestic => _resourceToolkit.GetLocaleString(LanguageNames.DomesticAnime),
                _ => string.Empty
            };

            _viewCaches = new Dictionary<Partition, PgcPageView>();
            _videoCaches = new Dictionary<string, IEnumerable<Models.Data.Video.VideoInformation>>();
            Banners = new ObservableCollection<IBannerViewModel>();
            Ranks = new ObservableCollection<IPgcRankViewModel>();
            Playlists = new ObservableCollection<IPgcPlaylistViewModel>();
            Videos = new ObservableCollection<IVideoItemViewModel>();
            Partitions = new ObservableCollection<Partition>();

            IsLoggedIn = _authorizeProvider.State == AuthorizeState.SignedIn;
            _authorizeProvider.StateChanged += OnAuthorizeStateChanged;

            Banners.CollectionChanged += OnBannersCollectionChanged;
            Ranks.CollectionChanged += OnRanksCollectionChanged;
            Playlists.CollectionChanged += OnPlaylistsCollectionChanged;

            InitializeCommand = ReactiveCommand.CreateFromTask(InitializeAsync);
            ReloadCommand = ReactiveCommand.CreateFromTask(ReloadAsync);
            IncrementalCommand = ReactiveCommand.CreateFromTask(IncrementalAsync);
            SelectPartitionCommand = ReactiveCommand.CreateFromTask<Partition>(SetPartitionAsync);
            GotoFavoritePageCommand = ReactiveCommand.Create(GotoFavoritePage);
            GotoIndexPageCommand = ReactiveCommand.Create(GotoIndexPage);
            GotoTimeLinePageCommand = ReactiveCommand.Create(GotoTimelinePage);

            InitializeCommand.IsExecuting
                .Merge(ReloadCommand.IsExecuting)
                .ToPropertyEx(this, x => x.IsReloading);

            IncrementalCommand.IsExecuting
                .Merge(IncrementalCommand.IsExecuting)
                .ToPropertyEx(this, x => x.IsIncrementalLoading);

            ReloadCommand.ThrownExceptions
                .Merge(InitializeCommand.ThrownExceptions)
                .Subscribe(DisplayException);

            IncrementalCommand.ThrownExceptions.Subscribe(LogException);
        }

        /// <inheritdoc/>
        public void DisplayException(Exception exception)
        {
            IsError = true;
            var msg = exception is ServiceException se
                ? se.GetMessage()
                : exception.Message;
            ErrorText = $"{_resourceToolkit.GetLocaleString(LanguageNames.RequestPgcFailed)}\n{msg}";
            LogException(exception);
        }

        private void OnAuthorizeStateChanged(object sender, AuthorizeStateChangedEventArgs e)
            => IsLoggedIn = e.NewState == AuthorizeState.SignedIn;

        private async Task InitializeAsync()
        {
            if (IsReloading)
            {
                return;
            }

            if (Partitions.Count > 0)
            {
                TryClear(Videos);
                await FakeLoadingAsync();

                if (!string.IsNullOrEmpty(_currentVideoPartitionId))
                {
                    _homeProvider.ResetSubPartitionState(_currentVideoPartitionId);
                    await LoadVideosAsync();
                }

                return;
            }

            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            if (IsReloading)
            {
                return;
            }

            TryClear(Partitions);
            var tabs = await _pgcProvider.GetAnimeTabsAsync(_type);
            tabs.ToList().ForEach(p => Partitions.Add(p));

            await FakeLoadingAsync();
            await SetPartitionAsync(Partitions.First());
        }

        private async Task SetPartitionAsync(Partition partition)
        {
            await FakeLoadingAsync();
            CurrentPartition = partition;
            TryClear(Banners);
            TryClear(Ranks);
            TryClear(Playlists);
            TryClear(Videos);
            IsShowVideo = false;
            _currentVideoPartitionId = string.Empty;
            _pgcProvider.ResetPageStatus(_type);

            if (!_viewCaches.TryGetValue(partition, out var view) || view == null)
            {
                view = await _pgcProvider.GetPageDetailAsync(partition.Id);
                _viewCaches.Remove(partition);
                _viewCaches.Add(partition, view);
            }

            await LoadPageViewAsync(view);
        }

        private async Task LoadPageViewAsync(PgcPageView view)
        {
            if (view.Banners.Count() > 0)
            {
                view.Banners.ToList().ForEach(p =>
                {
                    var vm = Locator.Current.GetService<IBannerViewModel>();
                    vm.InjectData(p);
                    Banners.Add(vm);
                });
            }

            if (view.Ranks.Count > 0)
            {
                foreach (var item in view.Ranks)
                {
                    var vm = Locator.Current.GetService<IPgcRankViewModel>();
                    vm.SetData(item.Key, item.Value);
                    Ranks.Add(vm);
                }
            }

            if (view.Playlists.Count() > 0)
            {
                foreach (var item in view.Playlists)
                {
                    var playlistVM = Locator.Current.GetService<IPgcPlaylistViewModel>();
                    playlistVM.InjectData(item);
                    Playlists.Add(playlistVM);
                }
            }

            IsShowVideo = !string.IsNullOrEmpty(view.VideoPartitionId);
            if (IsShowVideo)
            {
                _currentVideoPartitionId = view.VideoPartitionId;
                await LoadVideosAsync();
            }
        }

        private async Task LoadVideosAsync()
        {
            if (!_videoCaches.TryGetValue(_currentVideoPartitionId, out var videos))
            {
                var data = await _homeProvider.GetVideoSubPartitionDataAsync(_currentVideoPartitionId, false);
                videos = data.Videos.ToList();
            }

            foreach (var item in videos)
            {
                if (Videos.Any(p => p.Data.Equals(item)))
                {
                    continue;
                }

                var videoVM = Splat.Locator.Current.GetService<IVideoItemViewModel>();
                videoVM.InjectData(item);
                Videos.Add(videoVM);
            }
        }

        private async Task IncrementalAsync()
        {
            if (IsReloading
                || IsIncrementalLoading
                || string.IsNullOrEmpty(_currentVideoPartitionId))
            {
                return;
            }

            await LoadVideosAsync();
        }

        private void GotoFavoritePage()
            => _navigationViewModel.NavigateToSecondaryView(PageIds.Favorite, FavoriteType.Anime);

        private void GotoIndexPage()
            => _navigationViewModel.NavigateToSecondaryView(PageIds.PgcIndex, _type);

        private void GotoTimelinePage()
            => _navigationViewModel.NavigateToSecondaryView(PageIds.TimeLine, _type);

        private void OnBannersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => IsShowBanner = Banners.Count > 0;

        private void OnRanksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => IsShowRank = Ranks.Count > 0;

        private void OnPlaylistsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => IsShowPlaylist = Playlists.Count > 0;
    }
}

﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Bili.Lib.Interfaces;
using Bili.Models.Data.Community;
using Bili.Models.Enums.App;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces.Account;
using Bili.ViewModels.Interfaces.Community;
using Bili.ViewModels.Uwp.Base;
using ReactiveUI;
using Splat;
using Windows.UI.Core;

namespace Bili.ViewModels.Uwp.Community
{
    /// <summary>
    /// 消息页面视图模型.
    /// </summary>
    public sealed partial class MessagePageViewModel : InformationFlowViewModelBase<IMessageItemViewModel>, IMessagePageViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePageViewModel"/> class.
        /// </summary>
        public MessagePageViewModel(
            IAccountProvider accountProvider,
            IResourceToolkit resourceToolkit,
            IAccountViewModel accountViewModel,
            CoreDispatcher dispatcher)
            : base(dispatcher)
        {
            _accountProvider = accountProvider;
            _resourceToolkit = resourceToolkit;
            _accountViewModel = accountViewModel;

            _caches = new Dictionary<MessageType, (IEnumerable<MessageInformation> Items, bool IsEnd)>();
            MessageTypes = new ObservableCollection<IMessageHeaderViewModel>
            {
                GetMessageHeader(MessageType.Reply),
                GetMessageHeader(MessageType.At),
                GetMessageHeader(MessageType.Like),
            };

            InitializeMessageCount();

            this.WhenAnyValue(x => x._accountViewModel.UnreadInformation)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => InitializeMessageCount());

            SelectTypeCommand = ReactiveCommand.CreateFromTask<IMessageHeaderViewModel>(SelectTypeAsync);
        }

        /// <inheritdoc/>
        protected override void BeforeReload()
        {
            _accountProvider.ClearMessageStatus();
            if (_shouldClearCache)
            {
                _caches.Clear();
            }

            _isEnd = false;
            IsEmpty = false;
            _shouldClearCache = true;
        }

        /// <inheritdoc/>
        protected override async Task GetDataAsync()
        {
            if (_caches.Count == 0)
            {
                await FakeLoadingAsync();
                CurrentType = MessageTypes.First();
            }

            if (_isEnd)
            {
                return;
            }

            var view = await _accountProvider.GetMyMessagesAsync(CurrentType.Type);
            _isEnd = view.IsFinished;
            foreach (var item in view.Messages)
            {
                var messageVM = Splat.Locator.Current.GetService<IMessageItemViewModel>();
                messageVM.InjectData(item);
                Items.Add(messageVM);
            }

            _caches.Remove(CurrentType.Type);
            _caches.Add(CurrentType.Type, new(Items.Select(p => p.Data).ToList(), _isEnd));
            IsEmpty = Items.Count == 0;
            _accountViewModel.InitializeUnreadCommand.Execute().Subscribe();
        }

        /// <inheritdoc/>
        protected override string FormatException(string errorMsg)
            => $"{_resourceToolkit.GetLocaleString(Models.Enums.LanguageNames.RequestMessageFailed)}\n{errorMsg}";

        private async Task SelectTypeAsync(IMessageHeaderViewModel type)
        {
            await FakeLoadingAsync();
            TryClear(Items);
            _isEnd = false;
            CurrentType = type;
            if (_caches.TryGetValue(CurrentType.Type, out var data) && data.Items.Count() > 0)
            {
                foreach (var item in data.Items)
                {
                    var messageVM = Locator.Current.GetService<IMessageItemViewModel>();
                    messageVM.InjectData(item);
                    Items.Add(messageVM);
                }

                _isEnd = data.IsEnd;
                IsEmpty = Items.Count == 0;
            }
            else
            {
                _shouldClearCache = false;
                InitializeCommand.Execute().Subscribe();
            }
        }

        private void InitializeMessageCount()
        {
            var unreadInfo = _accountViewModel.UnreadInformation;
            if (unreadInfo != null)
            {
                MessageTypes.First(p => p.Type == MessageType.Reply).Count = unreadInfo.ReplyCount;
                MessageTypes.First(p => p.Type == MessageType.At).Count = unreadInfo.AtCount;
                MessageTypes.First(p => p.Type == MessageType.Like).Count = unreadInfo.LikeCount;
            }
            else
            {
                foreach (var item in MessageTypes)
                {
                    item.Count = 0;
                }
            }
        }

        private IMessageHeaderViewModel GetMessageHeader(MessageType type)
        {
            var vm = Locator.Current.GetService<IMessageHeaderViewModel>();
            vm.SetData(type);
            return vm;
        }
    }
}

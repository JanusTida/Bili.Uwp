﻿// Copyright (c) Richasy. All rights reserved.

using Bili.Adapter.Interfaces;
using Bili.Models.Data.Appearance;

namespace Bili.Adapter
{
    /// <summary>
    /// 图片适配器，将视频封面、用户头像等转换为 <see cref="Image"/>.
    /// </summary>
    public class ImageAdapter : IImageAdapter
    {
        /// <inheritdoc/>
        public Image ConvertToImage(string uri)
            => new Image(uri);

        /// <inheritdoc/>
        public Image ConvertToImage(string uri, double width, double height)
            => new Image(uri, width, height, (w, h) => $"@{w}w_{h}h_1c_100q.jpg");
    }
}

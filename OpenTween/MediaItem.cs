// OpenTween - Client of Twitter
// Copyright (c) 2015 spx (@5px)
// All rights reserved.
//
// This file is part of OpenTween.
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation; either version 3 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <http://www.gnu.org/licenses/>, or write to
// the Free Software Foundation, Inc., 51 Franklin Street - Fifth Floor,
// Boston, MA 02110-1301, USA.

#nullable enable
#pragma warning disable SA1649

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace OpenTween
{
    public interface IMediaItem : IDisposable
    {
        /// <summary>
        /// メディアのID
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// メディアが既に破棄されているかを示す真偽値
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// メディアへの絶対パス
        /// </summary>
        string Path { get; }

        /// <summary>
        /// メディア名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// メディアの拡張子
        /// </summary>
        string Extension { get; }

        /// <summary>
        /// メディアが存在するかどうかを示す真偽値
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// メディアのサイズ（バイト単位）
        /// </summary>
        long Size { get; }

        /// <summary>
        /// 代替テキスト (アップロード先が対応している必要がある)
        /// </summary>
        string? AltText { get; set; }

        /// <summary>
        /// 表示用の MemoryImage を作成する
        /// </summary>
        /// <remarks>
        /// 呼び出し側にて破棄すること
        /// </remarks>
        MemoryImage CreateImage();

        /// <summary>
        /// メディアの内容を読み込むための Stream を開く
        /// </summary>
        /// <remarks>
        /// 呼び出し側にて閉じること
        /// </remarks>
        Stream OpenRead();

        /// <summary>
        /// メディアの内容を Stream へ書き込む
        /// </summary>
        void CopyTo(Stream stream);
    }

    /// <summary>
    /// ファイル用の MediaItem クラス
    /// </summary>
    public class FileMediaItem : IMediaItem
    {
        public FileInfo FileInfo { get; }

        public string? AltText { get; set; }

        public FileMediaItem(string path)
            => this.FileInfo = new FileInfo(path);

        public FileMediaItem(FileInfo fileInfo)
            : this(fileInfo.FullName)
        {
        }

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsDisposed { get; private set; } = false;

        public string Path
            => this.FileInfo.FullName;

        public string Name
            => this.FileInfo.Name;

        public string Extension
            => this.FileInfo.Extension;

        public bool Exists
            => this.FileInfo.Exists;

        public long Size
            => this.FileInfo.Length;

        public MemoryImage CreateImage()
        {
            using var fs = this.FileInfo.OpenRead();
            return MemoryImage.CopyFromStream(fs);
        }

        public Stream OpenRead()
            => this.FileInfo.OpenRead();

        public void CopyTo(Stream stream)
        {
            using var fs = this.FileInfo.OpenRead();
            fs.CopyTo(stream);
        }

        public void Dispose()
        {
            if (this.IsDisposed)
                return;

            this.IsDisposed = true;
        }
    }

    /// <summary>
    /// MemoryImage 用の MediaItem クラス
    /// </summary>
    /// <remarks>
    /// 用途の関係上、メモリ使用量が大きくなるため、不要になればできるだけ破棄すること
    /// </remarks>
    public class MemoryImageMediaItem : IMediaItem
    {
        public const string PathPrefix = "<>MemoryImage://";
        private static int fileNumber = 0;
        private readonly MemoryImage image;

        public bool IsDisposed { get; private set; } = false;

        public MemoryImageMediaItem(MemoryImage image)
        {
            this.image = image ?? throw new ArgumentNullException(nameof(image));

            var num = Interlocked.Increment(ref fileNumber);
            this.Path = PathPrefix + num + this.image.ImageFormatExt;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public string Path { get; }

        public string? AltText { get; set; }

        public string Name
            => this.Path.Substring(PathPrefix.Length);

        public string Extension
            => this.image.ImageFormatExt;

        public bool Exists
            => this.image != null;

        public long Size
            => this.image.Stream.Length;

        public MemoryImage CreateImage()
            => this.image.Clone();

        public Stream OpenRead()
        {
            MemoryStream? memstream = null;
            try
            {
                // コピーを作成する
                memstream = new MemoryStream();

                this.image.Stream.WriteTo(memstream);
                memstream.Seek(0, SeekOrigin.Begin);

                return memstream;
            }
            catch
            {
                memstream?.Dispose();
                throw;
            }
        }

        public void CopyTo(Stream stream)
            => this.image.Stream.WriteTo(stream);

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed) return;

            if (disposing)
            {
                this.image.Dispose();
            }

            this.IsDisposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);

            // 明示的にDisposeが呼ばれた場合はファイナライザを使用しない
            GC.SuppressFinalize(this);
        }

        ~MemoryImageMediaItem()
            => this.Dispose(false);
    }
}

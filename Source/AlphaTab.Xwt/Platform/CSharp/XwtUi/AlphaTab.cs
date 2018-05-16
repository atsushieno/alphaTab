/*
 * This file is part of alphaTab.
 * Copyright © 2017, Daniel Kuschny and Contributors, All rights reserved.
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or at your option any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaTab.Model;
using AlphaTab.Rendering;
using SkiaSharp;
using Xwt;
using Xwt.Drawing;
using System.IO;

namespace AlphaTab.Platform.CSharp.XwtUi
{
    public class AlphaTab : ScrollView
    {
        private readonly AlphaTabLayoutPanel _contentPanel;
        private bool _initialRenderCompleted;
        private bool _isRendering;
        private bool _redrawPending;

	/*
        public static readonly BindableProperty TracksProperty = BindableProperty.Create("Tracks", typeof(IEnumerable<Track>), typeof(AlphaTab), propertyChanged: OnTracksChanged);
        private static void OnTracksChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            ((AlphaTab)bindable).InvalidateTracks();
        }
        public IEnumerable<Track> Tracks
        {
            get { return (IEnumerable<Track>)GetValue(TracksProperty); }
            set { SetValue(TracksProperty, value); }
        }
        */

		IEnumerable<Track> tracks;
		public IEnumerable<Track> Tracks {
			get {
				return tracks;
			}
			set {
				tracks = value;
				InvalidateTracks ();
			}
		}

        private readonly ScoreRenderer _renderer;

        public AlphaTab()
        {
            _contentPanel = new AlphaTabLayoutPanel();
			_contentPanel.HorizontalPlacement = WidgetPlacement.Fill;
			_contentPanel.VerticalPlacement = WidgetPlacement.Fill;

			this.HorizontalScrollPolicy = ScrollPolicy.Automatic;
			this.VerticalScrollPolicy = ScrollPolicy.Automatic;

            Content = _contentPanel;

            var settings = Settings.Defaults;
            settings.Engine = "skia";
            settings.Width = 970;
            settings.Scale = 0.8f;
            settings.StretchForce = 0.8f;

            _renderer = new ScoreRenderer(settings);
            _renderer.PreRender += result =>
            {
                lock (this)
                {
                    this.InvokeAsync(() =>
                    {
                        ClearPartialResults();
                        AddPartialResult(result);
                    });
                }
            };
            _renderer.PartialRenderFinished += result =>
            {
                lock (this)
                {
                    this.InvokeAsync(() =>
                    {
                        AddPartialResult(result);
                    });
                }
            };
            _renderer.RenderFinished += result =>
            {
                this.InvokeAsync(() =>
                {
                    _initialRenderCompleted = true;
                    _isRendering = false;
                    if (_redrawPending)
                    {
                        Resize((int)Size.Width);
                    }
                    OnRenderFinished(result);
                });
            };
        }

        private void ClearPartialResults()
        {
            _contentPanel.Clear();
        }

        private void AddPartialResult(RenderFinishedEventArgs result)
        {
            lock (this)
            {
                _contentPanel.WidthRequest = result.TotalWidth;
                _contentPanel.HeightRequest = result.TotalHeight;

                if (result.RenderResult != null)
                {
                    using (var image = (SKImage)result.RenderResult)
                    {
						var stream = new MemoryStream (image.Encode (SKEncodedImageFormat.Png, 100).ToArray ());
						_contentPanel.PackStart (new ImageView (Image.FromStream (stream)) {
							WidthRequest = result.Width,
							HeightRequest = result.Height
						});
                    }
                }
            }
        }

        private void InvalidateTracks()
        {
            if (Tracks == null) return;

            if (Size.Width > 0)
            {
                _renderer.Settings.Width = (int)Size.Width;
                _initialRenderCompleted = false;
                _isRendering = true;
                var tracks = Tracks.ToArray();
                if (tracks.Length > 0)
                {
                    ModelUtils.ApplyPitchOffsets(_renderer.Settings, tracks[0].Score);
                    Task.Factory.StartNew(() =>
                    {
                        _renderer.Render(tracks[0].Score, tracks.Select(t => t.Index).ToArray());
                    });
                }
            }
            else
            {
                _initialRenderCompleted = false;
                _redrawPending = true;
            }
        }

		protected override void OnBoundsChanged ()
		{
			base.OnBoundsChanged ();
			Resize ((int) this.Size.Width);
		}

		/*
		protected override void OnSizeAllocated(double width, double height)
		{
		    Resize((int)width);
		    base.OnSizeAllocated(width, height);
		}
		*/

        private void Resize(int width)
        {
            if (_isRendering)
            {
                _redrawPending = true;
            }
            else if (width > 0)
            {
                _redrawPending = false;

                if (!_initialRenderCompleted)
                {
                    InvalidateTracks();
                }
                else
                {
                    if (width != _renderer.Settings.Width)
                    {
                        _renderer.Settings.Width = width;
                        _isRendering = true;
                        Task.Factory.StartNew(() =>
                        {
                            _renderer.Resize(width);
                        });
                    }
                }
            }
        }

        public event EventHandler<RenderFinishedEventArgs> RenderFinished;
        protected virtual void OnRenderFinished(RenderFinishedEventArgs e)
        {
            RenderFinished?.Invoke(this, e);
        }
    }
}

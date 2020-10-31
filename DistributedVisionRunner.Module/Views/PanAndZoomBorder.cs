using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DistributedVisionRunner.Module.Views
{
    public class PanAndZoomBorder : Border
    {
        private UIElement child;
        private Point origin;
        private Point start;
        private static Point DefaultRTO = new Point(0.5, 0.5);
        private List<PanAndZoomBorder> Links = new List<PanAndZoomBorder>();

        /// <summary>Indicates whether element is zoomed in or not.</summary>
        public bool IsZoomed
        {
            get
            {
                ScaleTransform scaleTransform = this.GetScaleTransform();
                return scaleTransform.ScaleX != 1.0 || scaleTransform.ScaleY != 1.0;
            }
        }

        private TranslateTransform GetTranslateTransform() => (TranslateTransform)((TransformGroup)this.child.RenderTransform).Children.First<Transform>((Func<Transform, bool>)(tr => tr is TranslateTransform));

        private ScaleTransform GetScaleTransform() => (ScaleTransform)((TransformGroup)this.child.RenderTransform).Children.First<Transform>((Func<Transform, bool>)(tr => tr is ScaleTransform));

        public override UIElement Child
        {
            get => base.Child;
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            this.ClipToBounds = true;
            if (this.child == null)
                return;
            TransformGroup transformGroup = new TransformGroup();
            ScaleTransform scaleTransform = new ScaleTransform();
            transformGroup.Children.Add((Transform)scaleTransform);
            TranslateTransform translateTransform = new TranslateTransform();
            transformGroup.Children.Add((Transform)translateTransform);
            this.child.RenderTransform = (Transform)transformGroup;
            this.child.RenderTransformOrigin = PanAndZoomBorder.DefaultRTO;
            this.MouseWheel += new MouseWheelEventHandler(this.child_MouseWheel);
            this.MouseLeftButtonDown += new MouseButtonEventHandler(this.child_MouseLeftButtonDown);
            this.MouseLeftButtonDown += new MouseButtonEventHandler(this.LeftMouseDownLinked);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(this.child_MouseLeftButtonUp);
            this.MouseMove += new MouseEventHandler(this.child_MouseMove);
            this.MouseMove += new MouseEventHandler(this.MouseMoveLinked);
            this.MouseRightButtonDown += new MouseButtonEventHandler(this.child_MouseRightButtonDown);
        }

        public void Reset()
        {
            if (this.child == null)
                return;
            ScaleTransform scaleTransform = this.GetScaleTransform();
            scaleTransform.ScaleX = 1.0;
            scaleTransform.ScaleY = 1.0;
            this.child.RenderTransformOrigin = PanAndZoomBorder.DefaultRTO;
            TranslateTransform translateTransform = this.GetTranslateTransform();
            translateTransform.X = 0.0;
            translateTransform.Y = 0.0;
        }

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.child == null)
                return;
            ScaleTransform scaleTransform = this.GetScaleTransform();
            double num1 = e.Delta > 0 ? 0.2 : -0.2;
            double num2 = scaleTransform.ScaleX + num1;
            double num3 = scaleTransform.ScaleY + num1;
            bool flag = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            if (sender == this)
            {
                if (!flag)
                {
                    this.child.RenderTransformOrigin = PanAndZoomBorder.DefaultRTO;
                }
                else
                {
                    Point position = e.GetPosition((IInputElement)this.child);
                    UIElement child = this.child;
                    double x1 = position.X;
                    Size renderSize = this.child.RenderSize;
                    double width = renderSize.Width;
                    double x2 = x1 / width;
                    double y1 = position.Y;
                    renderSize = this.child.RenderSize;
                    double height = renderSize.Height;
                    double y2 = y1 / height;
                    Point point = new Point(x2, y2);
                    child.RenderTransformOrigin = point;
                }
            }
            if (this.Links.Count > 0)
            {
                foreach (PanAndZoomBorder link in this.Links)
                {
                    if (link.child != null)
                        link.child.RenderTransformOrigin = this.child.RenderTransformOrigin;
                }
            }
            if (num2 < 1.0)
                num2 = 1.0;
            if (num3 < 1.0)
                num3 = 1.0;
            scaleTransform.ScaleX = num2;
            scaleTransform.ScaleY = num3;
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.child == null || !this.CanPan())
                return;
            TranslateTransform translateTransform = this.GetTranslateTransform();
            this.start = e.GetPosition((IInputElement)this);
            this.origin = new Point(translateTransform.X, translateTransform.Y);
            this.Cursor = Cursors.ScrollAll;
            this.child.CaptureMouse();
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.child == null)
                return;
            this.child.ReleaseMouseCapture();
            this.Cursor = Cursors.Arrow;
        }

        private void child_MouseRightButtonDown(object sender, MouseButtonEventArgs e) => this.Reset();

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.child == null || !this.child.IsMouseCaptured && sender == this)
                return;
            this.child_MouseMoveLinked(this.start - e.GetPosition((IInputElement)this));
        }

        private void child_MouseMoveLinked(Vector relative)
        {
            TranslateTransform translateTransform = this.GetTranslateTransform();
            translateTransform.X = this.origin.X - relative.X;
            translateTransform.Y = this.origin.Y - relative.Y;
        }

        /// <summary>
        /// Links the transforms of a border to this one so they zoom and pan in tandem.
        /// </summary>
        /// <param name="borderToLink">Border to link to this one.</param>
        public void Link(PanAndZoomBorder borderToLink)
        {
            if (this.Links.Contains(borderToLink) || borderToLink.Links.Contains(this))
                return;
            this.Links.Add(borderToLink);
            borderToLink.Links.Add(this);
            ScaleTransform scaleTransform = this.GetScaleTransform();
            TranslateTransform translateTransform = this.GetTranslateTransform();
            borderToLink.GetTranslateTransform().X = translateTransform.X;
            borderToLink.GetTranslateTransform().Y = translateTransform.Y;
            borderToLink.GetScaleTransform().ScaleX = scaleTransform.ScaleX;
            borderToLink.GetScaleTransform().ScaleY = scaleTransform.ScaleY;
            this.MouseWheel += new MouseWheelEventHandler(borderToLink.child_MouseWheel);
            this.MouseLeftButtonDown += new MouseButtonEventHandler(borderToLink.child_MouseLeftButtonDown);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(borderToLink.child_MouseLeftButtonUp);
            this.MouseRightButtonDown += new MouseButtonEventHandler(borderToLink.child_MouseRightButtonDown);
            borderToLink.MouseWheel += new MouseWheelEventHandler(this.child_MouseWheel);
            borderToLink.MouseLeftButtonDown += new MouseButtonEventHandler(this.child_MouseLeftButtonDown);
            borderToLink.MouseLeftButtonUp += new MouseButtonEventHandler(this.child_MouseLeftButtonUp);
            borderToLink.MouseRightButtonDown += new MouseButtonEventHandler(this.child_MouseRightButtonDown);
        }

        /// <summary>Unlinks a border from this one to move independently.</summary>
        /// <param name="linkedBorder">Border to remove link from.</param>
        public void Unlink(PanAndZoomBorder linkedBorder)
        {
            this.Links.Remove(linkedBorder);
            linkedBorder.Links.Remove(this);
            this.MouseWheel -= new MouseWheelEventHandler(linkedBorder.child_MouseWheel);
            this.MouseLeftButtonDown -= new MouseButtonEventHandler(linkedBorder.child_MouseLeftButtonDown);
            this.MouseLeftButtonUp -= new MouseButtonEventHandler(linkedBorder.child_MouseLeftButtonUp);
            this.MouseRightButtonDown -= new MouseButtonEventHandler(linkedBorder.child_MouseRightButtonDown);
            linkedBorder.MouseWheel -= new MouseWheelEventHandler(this.child_MouseWheel);
            linkedBorder.MouseLeftButtonDown -= new MouseButtonEventHandler(this.child_MouseLeftButtonDown);
            linkedBorder.MouseLeftButtonUp -= new MouseButtonEventHandler(this.child_MouseLeftButtonUp);
            linkedBorder.MouseRightButtonDown -= new MouseButtonEventHandler(this.child_MouseRightButtonDown);
        }

        private void MouseMoveLinked(object sender, MouseEventArgs e)
        {
            if (this.Links.Count == 0 || this.child == null || !this.child.IsMouseCaptured)
                return;
            Vector relative = this.start - e.GetPosition((IInputElement)this);
            foreach (PanAndZoomBorder link in this.Links)
                link.child_MouseMoveLinked(relative);
        }

        private void LeftMouseDownLinked(object sender, MouseEventArgs e)
        {
            if (this.child == null)
                return;
            if (this.CanPan())
                e.Handled = true;
            if (this.Links.Count == 0)
                return;
            foreach (PanAndZoomBorder link in this.Links)
            {
                TranslateTransform translateTransform = link.GetTranslateTransform();
                link.origin = new Point(translateTransform.X, translateTransform.Y);
            }
        }

        private bool CanPan()
        {
            ScaleTransform scaleTransform = this.GetScaleTransform();
            return scaleTransform.ScaleX != 1.0 || scaleTransform.ScaleY != 1.0;
        }
    }
}

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
                ScaleTransform scaleTransform = GetScaleTransform();
                return scaleTransform.ScaleX != 1.0 || scaleTransform.ScaleY != 1.0;
            }
        }

        private TranslateTransform GetTranslateTransform() => (TranslateTransform)((TransformGroup)child.RenderTransform).Children.First<Transform>((Func<Transform, bool>)(tr => tr is TranslateTransform));

        private ScaleTransform GetScaleTransform() => (ScaleTransform)((TransformGroup)child.RenderTransform).Children.First<Transform>((Func<Transform, bool>)(tr => tr is ScaleTransform));

        public override UIElement Child
        {
            get => base.Child;
            set
            {
                if (value != null && value != Child)
                    Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            child = element;
            ClipToBounds = true;
            if (child == null)
                return;
            TransformGroup transformGroup = new TransformGroup();
            ScaleTransform scaleTransform = new ScaleTransform();
            transformGroup.Children.Add((Transform)scaleTransform);
            TranslateTransform translateTransform = new TranslateTransform();
            transformGroup.Children.Add((Transform)translateTransform);
            child.RenderTransform = (Transform)transformGroup;
            child.RenderTransformOrigin = DefaultRTO;
            MouseWheel += new MouseWheelEventHandler(child_MouseWheel);
            MouseLeftButtonDown += new MouseButtonEventHandler(child_MouseLeftButtonDown);
            MouseLeftButtonDown += new MouseButtonEventHandler(LeftMouseDownLinked);
            MouseLeftButtonUp += new MouseButtonEventHandler(child_MouseLeftButtonUp);
            MouseMove += new MouseEventHandler(child_MouseMove);
            MouseMove += new MouseEventHandler(MouseMoveLinked);
            MouseRightButtonDown += new MouseButtonEventHandler(child_MouseRightButtonDown);
        }

        public void Reset()
        {
            if (child == null)
                return;
            ScaleTransform scaleTransform = GetScaleTransform();
            scaleTransform.ScaleX = 1.0;
            scaleTransform.ScaleY = 1.0;
            child.RenderTransformOrigin = DefaultRTO;
            TranslateTransform translateTransform = GetTranslateTransform();
            translateTransform.X = 0.0;
            translateTransform.Y = 0.0;
        }

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.child == null)
                return;
            ScaleTransform scaleTransform = GetScaleTransform();
            double num1 = e.Delta > 0 ? 0.2 : -0.2;
            double num2 = scaleTransform.ScaleX + num1;
            double num3 = scaleTransform.ScaleY + num1;
            bool flag = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            if (sender == this)
            {
                if (!flag)
                {
                    child.RenderTransformOrigin = DefaultRTO;
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
            if (Links.Count > 0)
            {
                foreach (PanAndZoomBorder link in Links)
                {
                    if (link.child != null)
                        link.child.RenderTransformOrigin = child.RenderTransformOrigin;
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
            if (child == null || !CanPan())
                return;
            TranslateTransform translateTransform = GetTranslateTransform();
            start = e.GetPosition((IInputElement)this);
            origin = new Point(translateTransform.X, translateTransform.Y);
            Cursor = Cursors.ScrollAll;
            child.CaptureMouse();
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child == null)
                return;
            child.ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
        }

        private void child_MouseRightButtonDown(object sender, MouseButtonEventArgs e) => Reset();

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child == null || !child.IsMouseCaptured && sender == this)
                return;
            child_MouseMoveLinked(start - e.GetPosition((IInputElement)this));
        }

        private void child_MouseMoveLinked(Vector relative)
        {
            TranslateTransform translateTransform = GetTranslateTransform();
            translateTransform.X = origin.X - relative.X;
            translateTransform.Y = origin.Y - relative.Y;
        }

        /// <summary>
        /// Links the transforms of a border to this one so they zoom and pan in tandem.
        /// </summary>
        /// <param name="borderToLink">Border to link to this one.</param>
        public void Link(PanAndZoomBorder borderToLink)
        {
            if (Links.Contains(borderToLink) || borderToLink.Links.Contains(this))
                return;
            Links.Add(borderToLink);
            borderToLink.Links.Add(this);
            ScaleTransform scaleTransform = GetScaleTransform();
            TranslateTransform translateTransform = GetTranslateTransform();
            borderToLink.GetTranslateTransform().X = translateTransform.X;
            borderToLink.GetTranslateTransform().Y = translateTransform.Y;
            borderToLink.GetScaleTransform().ScaleX = scaleTransform.ScaleX;
            borderToLink.GetScaleTransform().ScaleY = scaleTransform.ScaleY;
            MouseWheel += new MouseWheelEventHandler(borderToLink.child_MouseWheel);
            MouseLeftButtonDown += new MouseButtonEventHandler(borderToLink.child_MouseLeftButtonDown);
            MouseLeftButtonUp += new MouseButtonEventHandler(borderToLink.child_MouseLeftButtonUp);
            MouseRightButtonDown += new MouseButtonEventHandler(borderToLink.child_MouseRightButtonDown);
            borderToLink.MouseWheel += new MouseWheelEventHandler(child_MouseWheel);
            borderToLink.MouseLeftButtonDown += new MouseButtonEventHandler(child_MouseLeftButtonDown);
            borderToLink.MouseLeftButtonUp += new MouseButtonEventHandler(child_MouseLeftButtonUp);
            borderToLink.MouseRightButtonDown += new MouseButtonEventHandler(child_MouseRightButtonDown);
        }

        /// <summary>Unlinks a border from this one to move independently.</summary>
        /// <param name="linkedBorder">Border to remove link from.</param>
        public void Unlink(PanAndZoomBorder linkedBorder)
        {
            Links.Remove(linkedBorder);
            linkedBorder.Links.Remove(this);
            MouseWheel -= new MouseWheelEventHandler(linkedBorder.child_MouseWheel);
            MouseLeftButtonDown -= new MouseButtonEventHandler(linkedBorder.child_MouseLeftButtonDown);
            MouseLeftButtonUp -= new MouseButtonEventHandler(linkedBorder.child_MouseLeftButtonUp);
            MouseRightButtonDown -= new MouseButtonEventHandler(linkedBorder.child_MouseRightButtonDown);
            linkedBorder.MouseWheel -= new MouseWheelEventHandler(child_MouseWheel);
            linkedBorder.MouseLeftButtonDown -= new MouseButtonEventHandler(child_MouseLeftButtonDown);
            linkedBorder.MouseLeftButtonUp -= new MouseButtonEventHandler(child_MouseLeftButtonUp);
            linkedBorder.MouseRightButtonDown -= new MouseButtonEventHandler(child_MouseRightButtonDown);
        }

        private void MouseMoveLinked(object sender, MouseEventArgs e)
        {
            if (Links.Count == 0 || child == null || !child.IsMouseCaptured)
                return;
            Vector relative = start - e.GetPosition((IInputElement)this);
            foreach (PanAndZoomBorder link in Links)
                link.child_MouseMoveLinked(relative);
        }

        private void LeftMouseDownLinked(object sender, MouseEventArgs e)
        {
            if (child == null)
                return;
            if (CanPan())
                e.Handled = true;
            if (Links.Count == 0)
                return;
            foreach (PanAndZoomBorder link in Links)
            {
                TranslateTransform translateTransform = link.GetTranslateTransform();
                link.origin = new Point(translateTransform.X, translateTransform.Y);
            }
        }

        private bool CanPan()
        {
            ScaleTransform scaleTransform = GetScaleTransform();
            return scaleTransform.ScaleX != 1.0 || scaleTransform.ScaleY != 1.0;
        }
    }
}

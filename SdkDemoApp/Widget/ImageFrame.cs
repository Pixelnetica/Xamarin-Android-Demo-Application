using Android.Content;
using Android.Util;
using Android.Views;
using Android.Graphics;
using Android.Content.Res;

namespace App.Widget
{
    public class ImageFrame : View
    {
        // Attributes
        private Color frameColor = Color.Green;
        private float frameWidth = 4;
        private Color insideColor;
        private Color outsideColor;
        private bool showEmpty = true;

        // Frame data
        private PointF[] framePoints;
        private readonly Matrix imageMatrix = new Matrix();
        private RectF imageBounds;

        // Scaled
        private float[] frameDisplay;
        private float[] boundsDisplay;

        // Graphics
        private Paint framePaint;
        private Path framePath;
        private Path boundsPath;

        private Paint insidePaint;
        private Paint eraserPaint;
        private Paint outsidePaint;

        public ImageFrame(Context context, IAttributeSet attrs) :
            this(context, attrs, Resource.Style.ImageFrame)
        {
           
        }

        public ImageFrame(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            // Obtain custom attributes
            TypedArray ar = context.ObtainStyledAttributes(attrs, Resource.Styleable.ImageFrame, defStyle, 0);
            try
            {
                frameColor = ar.GetColor(Resource.Styleable.ImageFrame_frameColor, frameColor);
                frameWidth = ar.GetDimension(Resource.Styleable.ImageFrame_frameWidth, frameWidth);
                insideColor = ar.GetColor(Resource.Styleable.ImageFrame_insideColor, insideColor);
                outsideColor = ar.GetColor(Resource.Styleable.ImageFrame_outsideColor, outsideColor);
                showEmpty = ar.GetBoolean(Resource.Styleable.ImageFrame_showEmpty, showEmpty);
            }
            finally
            {
                ar.Recycle();
            }
            Initialize();
        }

        public Matrix ImageMatrix
        {
            get => imageMatrix; set
            {
                imageMatrix.Set(value);
                Update();
            }
        }

        public PointF [] FramePoints
        {
            get => framePoints;
            set
            {
                framePoints = value;
                Update();
            }
        }

        public RectF ImageBounds
        {
            get => imageBounds; set
            {
                if (!Equals(value, imageBounds))
                {
                    imageBounds = value;
                    Update();
                }
            }
        }

        public bool ShowEmpty
        {
            get => showEmpty; set
            {
                if (value != showEmpty)
                {
                    showEmpty = value;
                    Update();
                }
            }
        }

        private void Initialize()
        {
            if (frameColor.A != 0)
            {
                framePaint = new Paint(PaintFlags.AntiAlias);
                framePaint.SetStyle(Paint.Style.Stroke);
                framePaint.Color = frameColor;
                framePaint.StrokeWidth = frameWidth;
                framePaint.StrokeJoin = Paint.Join.Round;
            }

            if (outsideColor.A != 0)
            {
                SetLayerType(LayerType.Software, null);

                outsidePaint = new Paint(PaintFlags.AntiAlias);
                outsidePaint.SetStyle(Paint.Style.Fill);
                outsidePaint.Color = outsideColor;

                eraserPaint = new Paint(PaintFlags.AntiAlias);
                eraserPaint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));
                eraserPaint.SetStyle(Paint.Style.FillAndStroke);
                eraserPaint.StrokeWidth = frameWidth;
                eraserPaint.StrokeJoin = Paint.Join.Round;
            }

            if (insideColor.A != 0)
            {
                insidePaint = new Paint(PaintFlags.AntiAlias);
                insidePaint.SetStyle(Paint.Style.Fill);
                insidePaint.Color = insideColor;
                insidePaint.StrokeWidth = frameWidth;
            }
        }

        private void ApplyPadding(float [] pts)
        {
            for (int i = 0; i < pts.Length; i+= 2)
            {
                pts[i] += PaddingLeft;
            }
            for (int i = 1; i < pts.Length; i+= 2)
            {
                pts[i] += PaddingTop;
            }

        }

        private void BuildPath(Path path, float [] pts)
        {
            path.Reset();
            path.MoveTo(pts[pts.Length - 2], pts[pts.Length - 1]);
            for (int i = 0; i < pts.Length; i += 2)
            {
                path.LineTo(pts[i], pts[i + 1]);
            }
            path.Close();
        }

        private void Update()
        {
            if (framePoints != null)
            {
                // Map to image and offset by padding
                frameDisplay = new float[framePoints.Length*2];
                for (int i = 0; i < framePoints.Length; ++i)
                {
                    frameDisplay[i * 2] = framePoints[i].X;
                    frameDisplay[i * 2 + 1] = framePoints[i].Y;
                }
                imageMatrix.MapPoints(frameDisplay);
                ApplyPadding(frameDisplay);
            }
            else if (imageBounds != null && showEmpty)
            {
                frameDisplay = new float[]
                {
                    imageBounds.Left, imageBounds.Top,
                    imageBounds.Right, imageBounds.Top,
                    imageBounds.Right, imageBounds.Bottom,
                    imageBounds.Left, imageBounds.Bottom,
                };
                imageMatrix.MapPoints(frameDisplay);
                ApplyPadding(frameDisplay);
            }
            else
            {
                frameDisplay = null;
            }

            if (imageBounds != null)
            {
                boundsDisplay = new float[]
                {
                    imageBounds.Left, imageBounds.Top,
                    imageBounds.Right, imageBounds.Top,
                    imageBounds.Right, imageBounds.Bottom,
                    imageBounds.Left, imageBounds.Bottom,
                };
                imageMatrix.MapPoints(boundsDisplay);
                ApplyPadding(boundsDisplay);
            }
            else
            {
                boundsDisplay = null;
            }

            Prepare();
        }

        private void Prepare()
        {
            if (frameDisplay != null)
            {
                // Prepare path
                if (framePath == null)
                {
                    framePath = new Path();
                }
                BuildPath(framePath, frameDisplay);
            }
            else
            {
                framePath = null;
            }

            if (boundsDisplay != null)
            {
                if (boundsPath == null)
                {
                    boundsPath = new Path();
                }
                BuildPath(boundsPath, boundsDisplay);
            }

            Invalidate();
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (framePath != null)
            {
                // Fill image
                if (boundsDisplay != null)
                {
                    // Fill outside
                    if (outsidePaint != null)
                    {
                        canvas.DrawPath(boundsPath, outsidePaint);
                        canvas.DrawPath(framePath, eraserPaint);
                    }

                    // Fill inside
                   if (insidePaint != null)
                    {
                        canvas.DrawPath(framePath, insidePaint);
                    }
                }
                canvas.DrawPath(framePath, framePaint);
            }
        }
    }
}
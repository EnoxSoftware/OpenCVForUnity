using OpenCVForUnity.CoreModule;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Color object for tracking based on HSV color range.
    /// </summary>
    public class ColorObject
    {
        // Private Fields
        private int _xPos;
        private int _yPos;
        private string _type;
        private Scalar _hsvMin;
        private Scalar _hsvMax;
        private Scalar _color;

        // Public Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorObject"/> class.
        /// </summary>
        public ColorObject()
        {
            //set values for default constructor
            SetType("Object");
            SetColor(new Scalar(0, 0, 0));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorObject"/> class with specified color name.
        /// </summary>
        /// <param name="name">The color name.</param>
        public ColorObject(string name)
        {
            SetType(name);

            if (name == "blue")
            {

                //TODO: use "calibration mode" to find HSV min
                //and HSV max values

                SetHSVmin(new Scalar(92, 0, 0));
                SetHSVmax(new Scalar(124, 256, 256));

                //BGR value for Blue:
                SetColor(new Scalar(0, 0, 255));

            }
            if (name == "green")
            {

                //TODO: use "calibration mode" to find HSV min
                //and HSV max values

                SetHSVmin(new Scalar(34, 50, 50));
                SetHSVmax(new Scalar(80, 220, 200));

                //BGR value for Green:
                SetColor(new Scalar(0, 255, 0));

            }
            if (name == "yellow")
            {

                //TODO: use "calibration mode" to find HSV min
                //and HSV max values

                SetHSVmin(new Scalar(20, 124, 123));
                SetHSVmax(new Scalar(30, 256, 256));

                //BGR value for Yellow:
                SetColor(new Scalar(255, 255, 0));

            }
            if (name == "red")
            {

                //TODO: use "calibration mode" to find HSV min
                //and HSV max values

                SetHSVmin(new Scalar(0, 200, 0));
                SetHSVmax(new Scalar(19, 255, 255));

                //BGR value for Red:
                SetColor(new Scalar(255, 0, 0));

            }
        }

        /// <summary>
        /// Gets the X position.
        /// </summary>
        /// <returns>The X position.</returns>
        public int GetXPos()
        {
            return _xPos;
        }

        /// <summary>
        /// Sets the X position.
        /// </summary>
        /// <param name="x">The X position.</param>
        public void SetXPos(int x)
        {
            _xPos = x;
        }

        /// <summary>
        /// Gets the Y position.
        /// </summary>
        /// <returns>The Y position.</returns>
        public int GetYPos()
        {
            return _yPos;
        }

        /// <summary>
        /// Sets the Y position.
        /// </summary>
        /// <param name="y">The Y position.</param>
        public void SetYPos(int y)
        {
            _yPos = y;
        }

        /// <summary>
        /// Gets the HSV minimum values.
        /// </summary>
        /// <returns>The HSV minimum values.</returns>
        public Scalar GetHSVmin()
        {
            return _hsvMin;
        }

        /// <summary>
        /// Gets the HSV maximum values.
        /// </summary>
        /// <returns>The HSV maximum values.</returns>
        public Scalar GetHSVmax()
        {
            return _hsvMax;
        }

        /// <summary>
        /// Sets the HSV minimum values.
        /// </summary>
        /// <param name="min">The HSV minimum values.</param>
        public void SetHSVmin(Scalar min)
        {
            _hsvMin = min;
        }

        /// <summary>
        /// Sets the HSV maximum values.
        /// </summary>
        /// <param name="max">The HSV maximum values.</param>
        public void SetHSVmax(Scalar max)
        {
            _hsvMax = max;
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <returns>The type.</returns>
        public string GetObjectType()
        {
            return _type;
        }

        /// <summary>
        /// Sets the type.
        /// </summary>
        /// <param name="t">The type.</param>
        public void SetType(string t)
        {
            _type = t;
        }

        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <returns>The color.</returns>
        public Scalar GetColor()
        {
            return _color;
        }

        /// <summary>
        /// Sets the color.
        /// </summary>
        /// <param name="c">The color.</param>
        public void SetColor(Scalar c)
        {
            _color = c;
        }
    }
}

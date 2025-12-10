using System;
using System.Numerics;

namespace Kinect_Middleware.Scripts {
    /// <summary>
    /// Class representing rotations mainly used for transforming this representation into quaternions
    /// </summary>
    class Euler {
        double x;
        double y;
        double z;

        public Euler(
            double x,
            double y,
            double z
        ) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Quaternion ToQuaternion() {
            double Deg2Rad = Math.PI / 180;

            double xOver2 = x * Deg2Rad * 0.5f;
            double yOver2 = y * Deg2Rad * 0.5f;
            double zOver2 = z * Deg2Rad * 0.5f;

            double sinXOver2 = Math.Sin(xOver2);
            double cosXOver2 = Math.Cos(xOver2);
            double sinYOver2 = Math.Sin(yOver2);
            double cosYOver2 = Math.Cos(yOver2);
            double sinZOver2 = Math.Sin(zOver2);
            double cosZOver2 = Math.Cos(zOver2);

            Quaternion result;
            result.X = (float)(cosYOver2 * sinXOver2 * cosZOver2 + sinYOver2 * cosXOver2 * sinZOver2);
            result.Y = (float)(sinYOver2 * cosXOver2 * cosZOver2 - cosYOver2 * sinXOver2 * sinZOver2);
            result.Z = (float)(cosYOver2 * cosXOver2 * sinZOver2 - sinYOver2 * sinXOver2 * cosZOver2);
            result.W = (float)(cosYOver2 * cosXOver2 * cosZOver2 + sinYOver2 * sinXOver2 * sinZOver2);

            return result;
        }
    }
}

using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace DLMHelix
{
    public static class ControleCamera
    {
        public enum eCameraViews
        {
            //Fore,
            Back,
            Left,
            Right,
            Top,
            Bottom,
            Isometric_PPP,
            Isometric_MPP,
            Isometric_PMP,
            Isometric_MMP
        }
        private struct CameraViews
        {
            public struct StandardViews
            {
                public struct Normals
                {
                    public static Vector3D Fore
                    {
                        get { return new Vector3D(1, 0, 0); }
                    }
                    public static Vector3D Back
                    {
                        get { return new Vector3D(-1, 0, 0); }
                    }
                    public static Vector3D Left
                    {
                        get { return new Vector3D(0, 1, 0); }
                    }
                    public static Vector3D Right
                    {
                        get { return new Vector3D(0, -1, 0); }
                    }
                    public static Vector3D Top
                    {
                        get { return new Vector3D(0, 0, 1); }
                    }
                    public static Vector3D Bottom
                    {
                        get { return new Vector3D(0, 0, -1); }
                    }
                }
                public struct UpVectors
                {
                    public static Vector3D Fore
                    {
                        get { return new Vector3D(0, 0, 1); }
                    }
                    public static Vector3D Back
                    {
                        get { return new Vector3D(0, 0, 1); }
                    }
                    public static Vector3D Left
                    {
                        get { return new Vector3D(0, 0, 1); }
                    }
                    public static Vector3D Right
                    {
                        get { return new Vector3D(0, 0, 1); }
                    }
                    public static Vector3D Top
                    {
                        get { return new Vector3D(0, 1, 0); }
                    }
                    public static Vector3D Bottom
                    {
                        get { return new Vector3D(0, -1, 0); }
                    }
                }
            }
            public struct IsometricViews
            {
                public struct Normals
                {
                    public static Vector3D PPP
                    {
                        get { return new Vector3D(1, 1, 1); }
                    }
                    public static Vector3D MPP
                    {
                        get { return new Vector3D(-1, 1, 1); }
                    }
                    public static Vector3D PMP
                    {
                        get { return new Vector3D(1, -1, 1); }
                    }
                    public static Vector3D MMP
                    {
                        get { return new Vector3D(-1, -1, 1); }
                    }
                }
                public struct UpVectors
                {
                    public static Vector3D PPP
                    {
                        get { return new Vector3D(-1, -1, 1); }
                    }
                    public static Vector3D MPP
                    {
                        get { return new Vector3D(1, -1, 1); }
                    }
                    public static Vector3D PMP
                    {
                        get { return new Vector3D(-1, 1, 1); }
                    }
                    public static Vector3D MMP
                    {
                        get { return new Vector3D(1, 1, 1); }
                    }
                }
            }
        }
        private static Vector3D GetNormal(eCameraViews view)
        {
            switch (view)
            {
                case eCameraViews.Top: return CameraViews.StandardViews.Normals.Top;
                case eCameraViews.Bottom: return CameraViews.StandardViews.Normals.Bottom;
                case eCameraViews.Left: return CameraViews.StandardViews.Normals.Left;
                case eCameraViews.Right: return CameraViews.StandardViews.Normals.Right;
                case eCameraViews.Back: return CameraViews.StandardViews.Normals.Back;
                case eCameraViews.Isometric_PPP: return CameraViews.IsometricViews.Normals.PPP;
                case eCameraViews.Isometric_MPP: return CameraViews.IsometricViews.Normals.MPP;
                case eCameraViews.Isometric_PMP: return CameraViews.IsometricViews.Normals.PMP;
                case eCameraViews.Isometric_MMP: return CameraViews.IsometricViews.Normals.MMP;
                default:
                    return CameraViews.StandardViews.Normals.Top;
                    //throw new NotSupportedException();
            }
        }
        private static Vector3D GetUpVector(eCameraViews view)
        {
            switch (view)
            {
                case eCameraViews.Top: return CameraViews.StandardViews.UpVectors.Top;
                case eCameraViews.Bottom: return CameraViews.StandardViews.UpVectors.Bottom;
                case eCameraViews.Left: return CameraViews.StandardViews.UpVectors.Left;
                case eCameraViews.Back: return CameraViews.StandardViews.UpVectors.Back;
                case eCameraViews.Right: return CameraViews.StandardViews.UpVectors.Right;
                case eCameraViews.Isometric_PPP: return CameraViews.IsometricViews.UpVectors.PPP;
                case eCameraViews.Isometric_MPP: return CameraViews.IsometricViews.UpVectors.MPP;
                case eCameraViews.Isometric_PMP: return CameraViews.IsometricViews.UpVectors.PMP;
                case eCameraViews.Isometric_MMP: return CameraViews.IsometricViews.UpVectors.MMP;
                default:
                    return CameraViews.StandardViews.UpVectors.Top;
                    //throw new NotSupportedException();
            }
        }
        public static void Setar(HelixViewport3D viewPort, eCameraViews view, double animationTime)
        {
            Vector3D faceNormal = GetNormal(view);
            Vector3D faceUp = GetUpVector(view);
            Vector3D lookDirection = -faceNormal;
            Vector3D upDirection = faceUp;
            lookDirection.Normalize();
            upDirection.Normalize();
            ProjectionCamera camera = viewPort.Camera as ProjectionCamera;
            if (camera != null)
            {
                Point3D target = camera.Position + camera.LookDirection;
                double distance = camera.LookDirection.Length;
                lookDirection *= distance;
                Point3D newPosition = target - lookDirection;
                viewPort.SetView(newPosition, lookDirection, upDirection, animationTime);
            }
        }
    }
}

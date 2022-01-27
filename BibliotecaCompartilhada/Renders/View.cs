using System.Collections.Generic;

namespace DLMHelix.Renders
{
    public static class View
    {
        public static void Faces(List<DLMCam.Face> faces)
        {
            ViewFaces pp = new ViewFaces(faces);
            pp.Show();
        }
        public static void Cam(DLMCam.ReadCam cam)
        {
            ViewFaces pp = new ViewFaces(cam);
            pp.Show();
        }
    }
}

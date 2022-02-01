using System.Collections.Generic;

namespace DLMHelix.Renders
{
    public static class View
    {
        public static void Faces(List<DLMcam.Face> faces)
        {
            ViewFaces pp = new ViewFaces(faces);
            pp.Show();
        }
        public static void Cam(DLMcam.ReadCam cam)
        {
            ViewFaces pp = new ViewFaces(cam);
            pp.Show();
        }
    }
}

using System.Collections.Generic;

namespace DLM.helix.Renders
{
    public static class View
    {
        public static void Faces(List<DLM.cam.Face> faces)
        {
            ViewFaces pp = new ViewFaces(faces);
            pp.Show();
        }
        public static void Cam(DLM.cam.ReadCam cam)
        {
            ViewFaces pp = new ViewFaces(cam);
            pp.Show();
        }
    }
}

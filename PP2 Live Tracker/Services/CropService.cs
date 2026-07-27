using System.Drawing;

namespace PP2_Live_Tracker.Services
{
    public static class CropService
    {
        /// <summary>
        /// Palauttaa alueen, josta kalailmoitukset luetaan.
        /// Arvot ovat suhteellisia peli-ikkunan kokoon,
        /// joten ne toimivat eri resoluutioilla.
        /// </summary>
        public static Rectangle GetFishNotificationArea(int windowWidth, int windowHeight)
        {
            int x = (int)(windowWidth * 0.48);
            int y = (int)(windowHeight * 0.02);

            int width = (int)(windowWidth * 0.46);
            int height = (int)(windowHeight * 0.16);

            return new Rectangle(x, y, width, height);
        }
    }
}
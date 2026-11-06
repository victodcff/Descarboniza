namespace Descarboniza.Helpers
{
    public static class FormatoHelper
    {
        // 5     -> "5h"
        // 5.5   -> "5h30min"
        // 0.25  -> "15min"
        public static string Horas(double horas)
        {
            var totalMinutos = (int)Math.Round(horas * 60, MidpointRounding.AwayFromZero);

            if (totalMinutos < 0)
                totalMinutos = 0;

            var h = totalMinutos / 60;
            var m = totalMinutos % 60;

            if (m == 0)
                return $"{h}h";

            if (h == 0)
                return $"{m}min";

            return $"{h}h{m:00}min";
        }
    }
}

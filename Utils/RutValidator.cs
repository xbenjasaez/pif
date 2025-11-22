using System.Text.RegularExpressions;

namespace BibliotecaVirtualWeb.Utils
{
    public static class RutValidator
    {
        public static bool ValidarRUT(string rut)
        {
            if (string.IsNullOrEmpty(rut))
                return false;

            // Limpiar el RUT
            rut = rut.Replace(".", "").Replace("-", "").Trim();
            
            if (rut.Length < 2)
                return false;

            // Separar número y dígito verificador
            string numero = rut[..^1];
            string dv = rut[^1..].ToUpper();

            // Validar que el número sea numérico
            if (!Regex.IsMatch(numero, @"^\d+$"))
                return false;

            // Validar dígito verificador
            string dvCalculado = CalcularDVRUT(numero);
            return dv == dvCalculado;
        }

        public static string CalcularDVRUT(string numero)
        {
            int suma = 0;
            int multiplicador = 2;

            for (int i = numero.Length - 1; i >= 0; i--)
            {
                suma += int.Parse(numero[i].ToString()) * multiplicador;
                multiplicador = multiplicador == 7 ? 2 : multiplicador + 1;
            }

            int resto = suma % 11;
            int dv = 11 - resto;

            if (dv == 11) return "0";
            if (dv == 10) return "K";
            return dv.ToString();
        }

        public static string FormatearRUT(string rut)
        {
            if (string.IsNullOrEmpty(rut))
                return string.Empty;

            // Limpiar el RUT
            rut = rut.Replace(".", "").Replace("-", "").Trim();
            
            if (rut.Length < 2)
                return rut;

            string numero = rut[..^1];
            string dv = rut[^1..];

            // Formatear número con puntos
            string numeroFormateado = "";
            for (int i = numero.Length - 1, contador = 0; i >= 0; i--, contador++)
            {
                if (contador > 0 && contador % 3 == 0)
                    numeroFormateado = "." + numeroFormateado;
                numeroFormateado = numero[i] + numeroFormateado;
            }

            return $"{numeroFormateado}-{dv}";
        }
    }
}
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text;

namespace BibliotecaVirtualWeb.Helpers
{
    public static class Ean13Helper
    {
        private const int BaseLength = 12;

        public static bool TryNormalize(string? input, out string codigoEan13, out string error)
        {
            codigoEan13 = string.Empty;
            error = string.Empty;

            var digits = ExtraerDigitos(input);
            if (string.IsNullOrWhiteSpace(digits))
            {
                error = "Debes ingresar al menos un dígito.";
                return false;
            }

            if (digits.Length < BaseLength)
            {
                digits = digits.PadLeft(BaseLength, '0');
            }

            if (digits.Length == BaseLength)
            {
                codigoEan13 = CalcularCodigoDesdeBase(digits);
                return true;
            }

            if (digits.Length == BaseLength + 1)
            {
                if (!EsCodigoValido(digits))
                {
                    error = "El dígito verificador EAN13 no es válido.";
                    return false;
                }

                codigoEan13 = digits;
                return true;
            }

            error = "El código EAN13 debe tener 12 o 13 dígitos.";
            return false;
        }

        public static bool EsCodigoValido(string codigoEan13)
        {
            if (string.IsNullOrWhiteSpace(codigoEan13) || codigoEan13.Length != BaseLength + 1 || !codigoEan13.All(char.IsDigit))
            {
                return false;
            }

            var esperado = CalcularChecksum(codigoEan13[..BaseLength]);
            var recibido = codigoEan13[^1] - '0';
            return esperado == recibido;
        }

        public static string CalcularCodigoDesdeBase(string base12)
        {
            if (string.IsNullOrWhiteSpace(base12) || base12.Length != BaseLength || !base12.All(char.IsDigit))
            {
                throw new ArgumentException("La base EAN13 debe contener exactamente 12 dígitos.", nameof(base12));
            }

            var checksum = CalcularChecksum(base12);
            return $"{base12}{checksum}";
        }

        public static async Task<string> GenerarCodigoUnicoAsync(Func<string, Task<bool>> existeAsync)
        {
            string codigo;
            bool existe;

            do
            {
                var base12 = GenerarBase12();
                codigo = CalcularCodigoDesdeBase(base12);
                existe = await existeAsync(codigo);
            } while (existe);

            return codigo;
        }

        private static string GenerarBase12()
        {
            Span<byte> buffer = stackalloc byte[BaseLength];
            RandomNumberGenerator.Fill(buffer);

            var sb = new StringBuilder(BaseLength);
            for (int i = 0; i < BaseLength; i++)
            {
                var digit = (buffer[i] % 10).ToString();
                sb.Append(digit);
            }

            if (sb[0] == '0')
            {
                sb[0] = (char)('1' + buffer[0] % 9);
            }

            return sb.ToString();
        }

        private static string ExtraerDigitos(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var filtered = input.Where(char.IsDigit);
            return string.Concat(filtered);
        }

        private static int CalcularChecksum(string base12)
        {
            var suma = 0;
            for (int i = 0; i < BaseLength; i++)
            {
                var digit = base12[i] - '0';
                suma += (i % 2 == 0) ? digit : digit * 3;
            }

            var modulo = suma % 10;
            return (10 - modulo) % 10;
        }
    }
}


using System;
using System.Linq;

namespace Wild.Utils
{
    /// <summary>
    /// Utilidades para validación de inputs y datos comunes.
    /// </summary>
    public static class ValidationUtils
    {
        /// <summary>
        /// Valida un nombre genérico (personaje o mundo).
        /// </summary>
        public static (bool isValid, string feedback) ValidateName(string name, int minLength = 3, int maxLength = 20)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "El nombre no puede estar vacío");
            
            if (name.Length < minLength)
                return (false, $"El nombre debe tener al menos {minLength} caracteres");
            
            if (name.Length > maxLength)
                return (false, $"El nombre no puede tener más de {maxLength} caracteres");
            
            // Verificar caracteres permitidos (letras, números y espacios)
            if (!name.All(c => char.IsLetterOrDigit(c) || c == ' '))
                return (false, "El nombre solo puede contener letras, números y espacios");
            
            return (true, "Nombre válido");
        }
    }
}

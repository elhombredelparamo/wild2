using System;

namespace Wild.Data
{
    /// <summary>
    /// Representa los datos básicos de un mundo de juego.
    /// </summary>
    public class Mundo
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string nombre { get; set; } = "";
        public string semilla { get; set; } = "";
        public DateTime fecha_creacion { get; set; } = DateTime.Now;
        public DateTime ultimo_acceso { get; set; } = DateTime.Now;

        public Mundo() { }

        public Mundo(string nombre, string semilla)
        {
            this.nombre = nombre;
            this.semilla = semilla;
            this.fecha_creacion = DateTime.Now;
            this.ultimo_acceso = DateTime.Now;
        }

        public int GetSeedInt()
        {
            if (int.TryParse(semilla, out int result))
                return result;
            return semilla.GetHashCode();
        }
    }
}

// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Datos de Personajes
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - codigo/core/personajes.pseudo - Diseño técnico de gestión de personajes
// - contexto/personajes.md - Sistema de gestión de personajes realista
// 
// DESCRIPCIÓN:
// Clase de datos del personaje simplificada con información básica.
// Solo contiene apodo, ID único y género (hombre/mujer).
// 
// -----------------------------------------------------------------------------
using Godot;
using System;
using System.Collections.Generic;

namespace Wild.Data
{
    // -------------------------------------------------------------------------
    // CLASE PRINCIPAL: Personaje
    // -------------------------------------------------------------------------
    public class Personaje
    {
        // -----------------------------------------------------------------
        // DATOS BÁSICOS DEL PERSONAJE
        // -----------------------------------------------------------------
        public string id { get; set; } = "";                    // ID único (generado al crear)
        public string apodo { get; set; } = "";                 // Apodo del personaje
        public string genero { get; set; } = "hombre";          // Género (hombre/mujer)
        public DateTime fecha_creacion { get; set; }            // Fecha de creación
        public DateTime ultimo_acceso { get; set; }            // Último acceso
        public DateTime ultima_seleccion { get; set; } = DateTime.Now; // Última selección

        // -------------------------------------------------------------------------
        // PROPIEDADES DE MODELO
        // -------------------------------------------------------------------------
        public string modelo_ruta { get; set; } = "";            // Ruta del modelo 3D
        
        // Método para obtener la ruta según género
        public string ObtenerRutaModelo()
        {
            string ruta = genero.ToLower() switch
            {
                "hombre" => "res://scenes/player/animado_con_tree.tscn", // Nuevo archivo con AnimationTree
                "mujer" => "res://scenes/player/animada_con_tree.tscn", // Nuevo archivo con AnimationTree
                _ => "res://scenes/player/animado_con_tree.tscn" // Default por seguridad
            };
            Wild.Utils.Logger.LogDebug($"Personaje: Ruta de modelo resuelta: {ruta} (genero: {genero})");
            return ruta;
        }

        // -----------------------------------------------------------------
        // CONSTRUCTOR
        // -----------------------------------------------------------------
        public Personaje()
        {
            id = Guid.NewGuid().ToString();
            fecha_creacion = DateTime.Now;
            ultimo_acceso = DateTime.Now;

            Wild.Utils.Logger.LogInfo($"Personaje: Nuevo personaje creado con ID {id}");
        }

        // -----------------------------------------------------------------
        // VALIDACIÓN
        // -----------------------------------------------------------------
        public bool EsValido()
        {
            try
            {
                // Validar apodo
                if (string.IsNullOrEmpty(apodo) || apodo.Length < 3 || apodo.Length > 20)
                {
                    Wild.Utils.Logger.LogWarning($"Personaje: Apodo inválido: {apodo}");
                    return false;
                }

                // Validar ID
                if (string.IsNullOrEmpty(id))
                {
                    Wild.Utils.Logger.LogWarning("Personaje: ID vacío");
                    return false;
                }

                // Validar género
                if (genero != "hombre" && genero != "mujer")
                {
                    Wild.Utils.Logger.LogWarning($"Personaje: Género inválido: {genero}");
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"Personaje: Error en validación: {ex.Message}");
                return false;
            }
        }

        // -----------------------------------------------------------------
        // MÉTODOS DE UTILIDAD
        // -----------------------------------------------------------------
        public override string ToString()
        {
            return $"Personaje[{id}] {apodo} - {genero}";
        }
    }

    // -------------------------------------------------------------------------
    // CLASE: ResultadoCreacionPersonaje
    // -------------------------------------------------------------------------
    public class ResultadoCreacionPersonaje
    {
        public bool exito { get; set; } = false;
        public string error { get; set; } = "";
        public Personaje personaje { get; set; } = null;

        public ResultadoCreacionPersonaje()
        {
            Wild.Utils.Logger.LogDebug("ResultadoCreacionPersonaje: Nueva instancia creada");
        }

        public ResultadoCreacionPersonaje(bool exito, string error = "", Personaje personaje = null)
        {
            this.exito = exito;
            this.error = error;
            this.personaje = personaje;

            if (exito && personaje != null)
            {
                Wild.Utils.Logger.LogInfo($"ResultadoCreacionPersonaje: Personaje {personaje.apodo} creado exitosamente");
            }
            else if (!exito)
            {
                Wild.Utils.Logger.LogWarning($"ResultadoCreacionPersonaje: Fallo en creación: {error}");
            }
        }
    }
}

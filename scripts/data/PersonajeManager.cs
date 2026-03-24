// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Gestión de Personajes
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - codigo/core/personajes.pseudo - Diseño técnico de gestión de personajes
// - contexto/personajes.md - Sistema de gestión de personajes realista
// - fdd/active/current-feature.md - Feature 3: Sistema de Personajes
// 
// DESCRIPCIÓN:
// Gestor centralizado de personajes con persistencia local, validación
// y manejo robusto de errores. Implementa patrón Singleton.
// 
// -----------------------------------------------------------------------------
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Wild.Core;
using Wild.Utils;

namespace Wild.Data
{
    // -------------------------------------------------------------------------
    // CLASE PRINCIPAL: PersonajeManager (Singleton)
    // -------------------------------------------------------------------------
    public partial class PersonajeManager : Node
    {
        // -------------------------------------------------------------------------
        // SINGLETON
        // -------------------------------------------------------------------------
        private static PersonajeManager _instance;
        public static PersonajeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Wild.Utils.Logger.LogError("PersonajeManager: Instance no inicializada. Usar Inicializar() primero.");
                }
                return _instance;
            }
        }

        // -------------------------------------------------------------------------
        // PROPIEDADES
        // -------------------------------------------------------------------------
        private Dictionary<string, Personaje> _personajes;
        private string _personajeActualId = "";
        private string _directorioPersonajes;
        private const int MAX_PERSONAJES = 10;

        // -------------------------------------------------------------------------
        // INICIALIZACIÓN
        // -------------------------------------------------------------------------
        public override void _Ready()
        {
            Wild.Utils.Logger.Inicializar();
            if (_instance == null)
            {
                _instance = this;
                _InicializarInterno("user://characters/"); // Ruta por defecto para Autoload
                Wild.Utils.Logger.LogInfo("PersonajeManager: Autoload inicializado automáticamente.");
            }
        }

        public override void _ExitTree()
        {
            if (_instance == this)
            {
                // _instance = null;
                Wild.Utils.Logger.LogInfo("PersonajeManager: Autoload persistente.");
            }
        }

        private void _InicializarInterno(string directorio)
        {
            try
            {
                _directorioPersonajes = ProjectSettings.GlobalizePath(directorio);
                _personajes = new Dictionary<string, Personaje>();

                // Crear directorio si no existe
                CrearDirectorioPersonajes();

                // Cargar personajes existentes
                CargarPersonajesLocales();

                // Cargar última selección persistida
                CargarSeleccion();

                // Si tras cargar selección no hay personaje actual, intentar garantizar uno
                if (!HayPersonajeActual)
                {
                    GarantizarPersonajePorDefecto();

                    // Si aún no hay (porque no hubo creación), seleccionar el primero si existe
                    if (!HayPersonajeActual && _personajes.Count > 0)
                    {
                        SeleccionarPrimerPersonajeDisponible();
                    }
                }

                Wild.Utils.Logger.LogInfo($"PersonajeManager: Inicializado con {_personajes.Count} personajes");
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en _InicializarInterno(): {ex.Message}");
                throw;
            }
        }

        // -------------------------------------------------------------------------
        // GESTIÓN DE PERSONAJES
        // -------------------------------------------------------------------------
        public ResultadoCreacionPersonaje CrearPersonaje(string apodo, string genero = "hombre")
        {
            try
            {
                var resultado = new ResultadoCreacionPersonaje();

                // Validar apodo
                if (string.IsNullOrEmpty(apodo) || apodo.Length < 3 || apodo.Length > 20)
                {
                    resultado.error = "El apodo debe tener entre 3 y 20 caracteres y solo puede contener letras, números, espacios, guiones bajos y guiones";
                    return resultado;
                }

                // Validar género
                if (genero != "hombre" && genero != "mujer")
                {
                    Wild.Utils.Logger.LogError($"PersonajeManager: Género inválido: {genero}");
                    return new ResultadoCreacionPersonaje(false, $"Género inválido: {genero}");
                }

                // Verificar límite de personajes
                if (_personajes.Count >= MAX_PERSONAJES)
                {
                    resultado.error = $"Límite de personajes alcanzado ({MAX_PERSONAJES})";
                    return resultado;
                }

                // Verificar nombre duplicado
                foreach (var personaje in _personajes.Values)
                {
                    if (personaje.apodo.ToLower() == apodo.ToLower())
                    {
                        resultado.error = "Ya existe un personaje con ese apodo";
                        return resultado;
                    }
                }

                // Generar ID único
                string idPersonaje = GenerarIDPersonaje(apodo);

                // Crear personaje
                var nuevoPersonaje = new Personaje
                {
                    id = idPersonaje,
                    apodo = apodo,
                    genero = genero,
                    fecha_creacion = DateTime.Now,
                    ultimo_acceso = DateTime.Now
                };

                // Validar personaje
                if (!nuevoPersonaje.EsValido())
                {
                    resultado.error = "Datos del personaje inválidos";
                    return resultado;
                }

                // Guardar personaje localmente
                GuardarPersonajeLocal(nuevoPersonaje);

                // Agregar a lista
                _personajes[idPersonaje] = nuevoPersonaje;

                resultado.exito = true;
                resultado.personaje = nuevoPersonaje;

                Wild.Utils.Logger.LogInfo($"PersonajeManager: Personaje creado exitosamente: {apodo} (ID: {idPersonaje})");
                return resultado;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en CrearPersonaje(): {ex.Message}");
                return new ResultadoCreacionPersonaje(false, $"Error interno: {ex.Message}");
            }
        }

        public bool EliminarPersonaje(string idPersonaje)
        {
            try
            {
                if (!_personajes.ContainsKey(idPersonaje))
                {
                    Wild.Utils.Logger.LogWarning($"PersonajeManager: Personaje no encontrado: {idPersonaje}");
                    return false;
                }

                // No permitir eliminar si es el único personaje
                if (_personajes.Count <= 1)
                {
                    Wild.Utils.Logger.LogWarning("PersonajeManager: No se puede eliminar el único personaje");
                    return false;
                }

                // Eliminar archivo local
                string rutaPersonaje = ObtenerRutaPersonaje(idPersonaje);
                if (File.Exists(rutaPersonaje))
                {
                    File.Delete(rutaPersonaje);
                }

                // Eliminar de lista
                _personajes.Remove(idPersonaje);

                // Si era el personaje actual, limpiar selección
                if (_personajeActualId == idPersonaje)
                {
                    _personajeActualId = "";
                    SeleccionarPrimerPersonajeDisponible();
                    GuardarSeleccion();
                }

                Wild.Utils.Logger.LogInfo($"PersonajeManager: Personaje eliminado: {idPersonaje}");
                return true;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en EliminarPersonaje(): {ex.Message}");
                return false;
            }
        }

        public bool SeleccionarPersonaje(string idPersonaje)
        {
            try
            {
                if (!_personajes.ContainsKey(idPersonaje))
                {
                    Wild.Utils.Logger.LogWarning($"PersonajeManager: Personaje no encontrado: {idPersonaje}");
                    return false;
                }

                _personajeActualId = idPersonaje;
                _personajes[idPersonaje].ultima_seleccion = DateTime.Now;
                _personajes[idPersonaje].ultimo_acceso = DateTime.Now;

                // Guardar cambio de datos del personaje
                GuardarPersonajeLocal(_personajes[idPersonaje]);
                
                // Guardar selección persistente
                GuardarSeleccion();

                Wild.Utils.Logger.LogInfo($"PersonajeManager: Personaje seleccionado y persistido: {idPersonaje}");
                return true;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en SeleccionarPersonaje(): {ex.Message}");
                return false;
            }
        }

        // -------------------------------------------------------------------------
        // OBTENCIÓN DE DATOS
        // -------------------------------------------------------------------------
        public Personaje ObtenerPersonajeActual()
        {
            try
            {
                if (!string.IsNullOrEmpty(_personajeActualId) && _personajes.ContainsKey(_personajeActualId))
                {
                    return _personajes[_personajeActualId];
                }

                Wild.Utils.Logger.LogWarning("PersonajeManager: No hay personaje actual seleccionado");
                return null;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en ObtenerPersonajeActual(): {ex.Message}");
                return null;
            }
        }

        public List<Personaje> ObtenerTodosPersonajes()
        {
            try
            {
                var personajes = new List<Personaje>();
                foreach (var personaje in _personajes.Values)
                {
                    personajes.Add(personaje);
                }
                return personajes;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en ObtenerTodosPersonajes(): {ex.Message}");
                return new List<Personaje>();
            }
        }

        public Personaje ObtenerPersonaje(string idPersonaje)
        {
            try
            {
                if (_personajes.ContainsKey(idPersonaje))
                {
                    return _personajes[idPersonaje];
                }

                Wild.Utils.Logger.LogWarning($"PersonajeManager: Personaje no encontrado: {idPersonaje}");
                return null;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en ObtenerPersonaje(): {ex.Message}");
                return null;
            }
        }

        // -------------------------------------------------------------------------
        // PERSISTENCIA
        // -------------------------------------------------------------------------
        private void CargarPersonajesLocales()
        {
            try
            {
                if (!Directory.Exists(_directorioPersonajes))
                {
                    Wild.Utils.Logger.LogInfo($"PersonajeManager: Directorio no existe: {_directorioPersonajes}");
                    return;
                }

                var archivos = Directory.GetFiles(_directorioPersonajes, "*.json");

                foreach (var archivo in archivos)
                {
                    try
                    {
                        string datosJson = File.ReadAllText(archivo);
                        var personaje = JsonSerializer.Deserialize<Personaje>(datosJson);

                        if (personaje != null && personaje.EsValido())
                        {
                            _personajes[personaje.id] = personaje;
                            Wild.Utils.Logger.LogDebug($"PersonajeManager: Personaje cargado: {personaje.apodo}");
                        }
                        else
                        {
                            Wild.Utils.Logger.LogWarning($"PersonajeManager: Personaje inválido en archivo: {archivo}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Wild.Utils.Logger.LogError($"PersonajeManager: Error cargando personaje desde {archivo}: {ex.Message}");
                    }
                }

                Wild.Utils.Logger.LogInfo($"PersonajeManager: Cargados {_personajes.Count} personajes locales");
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en CargarPersonajesLocales(): {ex.Message}");
            }
        }

        private void GuardarPersonajeLocal(Personaje personaje)
        {
            try
            {
                CrearDirectorioPersonajes();

                string rutaPersonaje = ObtenerRutaPersonaje(personaje.id);
                var opciones = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                string datosJson = JsonSerializer.Serialize(personaje, opciones);
                File.WriteAllText(rutaPersonaje, datosJson);

                Wild.Utils.Logger.LogDebug($"PersonajeManager: Personaje guardado: {personaje.apodo}");
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en GuardarPersonajeLocal(): {ex.Message}");
            }
        }

        private void CrearDirectorioPersonajes()
        {
            try
            {
                if (!Directory.Exists(_directorioPersonajes))
                {
                    Directory.CreateDirectory(_directorioPersonajes);
                    Wild.Utils.Logger.LogInfo($"PersonajeManager: Directorio creado: {_directorioPersonajes}");
                }
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en CrearDirectorioPersonajes(): {ex.Message}");
            }
        }

        private string ObtenerRutaPersonaje(string idPersonaje)
        {
            return Path.Combine(_directorioPersonajes, $"{idPersonaje}.json");
        }

        private string ObtenerRutaSeleccion()
        {
            return Path.Combine(_directorioPersonajes, "selected.dat");
        }

        private void GuardarSeleccion()
        {
            try
            {
                string ruta = ObtenerRutaSeleccion();
                File.WriteAllText(ruta, _personajeActualId);
                Wild.Utils.Logger.LogDebug($"PersonajeManager: Selección persistida en {ruta}");
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error al guardar selección: {ex.Message}");
            }
        }

        private void CargarSeleccion()
        {
            try
            {
                string ruta = ObtenerRutaSeleccion();
                if (File.Exists(ruta))
                {
                    string idPersistido = File.ReadAllText(ruta).Trim();
                    if (_personajes.ContainsKey(idPersistido))
                    {
                        _personajeActualId = idPersistido;
                        Wild.Utils.Logger.LogInfo($"PersonajeManager: Selección cargada: {_personajeActualId}");
                    }
                    else
                    {
                        Wild.Utils.Logger.LogWarning($"PersonajeManager: ID persistido no válido o no encontrado: {idPersistido}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error al cargar selección: {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // VALIDACIÓN
        // -------------------------------------------------------------------------
        private bool ValidarNombrePersonaje(string nombre)
        {
            try
            {
                // Verificar longitud
                if (string.IsNullOrEmpty(nombre) || nombre.Length < 3 || nombre.Length > 20)
                {
                    return false;
                }

                // Verificar caracteres permitidos
                foreach (char c in nombre)
                {
                    if (!char.IsLetterOrDigit(c) && c != ' ' && c != '_' && c != '-')
                    {
                        return false;
                    }
                }

                // Verificar que no empiece o termine con espacio
                if (nombre[0] == ' ' || nombre[nombre.Length - 1] == ' ')
                {
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en ValidarNombrePersonaje(): {ex.Message}");
                return false;
            }
        }

        private string GenerarIDPersonaje(string apodo)
        {
            try
            {
                // Usar timestamp y apodo para generar ID único
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string datos = $"{apodo.ToLower()}{timestamp}{Guid.NewGuid().ToString("N")[..8]}";
                
                // Generar hash simple
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(datos));
                    return Convert.ToHexString(hash)[..16].ToLower();
                }
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en GenerarIDPersonaje(): {ex.Message}");
                return Guid.NewGuid().ToString("N")[..16];
            }
        }

        // -------------------------------------------------------------------------
        // MÉTODOS DE GARANTÍA
        // -------------------------------------------------------------------------
        private void GarantizarPersonajePorDefecto()
        {
            try
            {
                if (_personajes.Count == 0)
                {
                    Wild.Utils.Logger.LogInfo("PersonajeManager: No hay personajes, creando personaje por defecto");
                    
                    var resultado = CrearPersonaje("Aventurero", "hombre");
                    if (resultado.exito)
                    {
                        SeleccionarPersonaje(resultado.personaje.id);
                        Wild.Utils.Logger.LogInfo("PersonajeManager: Personaje por defecto creado y seleccionado");
                    }
                    else
                    {
                        Wild.Utils.Logger.LogError($"PersonajeManager: Error creando personaje por defecto: {resultado.error}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en GarantizarPersonajePorDefecto(): {ex.Message}");
            }
        }

        private void SeleccionarPrimerPersonajeDisponible()
        {
            try
            {
                if (_personajes.Count > 0)
                {
                    var primerPersonaje = _personajes.Values.First();
                    SeleccionarPersonaje(primerPersonaje.id);
                    Wild.Utils.Logger.LogInfo($"PersonajeManager: Seleccionado automáticamente personaje: {primerPersonaje.apodo}");
                }
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en SeleccionarPrimerPersonajeDisponible(): {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // PROPIEDADES PÚBLICAS
        // -------------------------------------------------------------------------
        public int CantidadPersonajes => _personajes.Count;
        public string PersonajeActualId => _personajeActualId;
        public bool HayPersonajeActual => !string.IsNullOrEmpty(_personajeActualId) && _personajes.ContainsKey(_personajeActualId);


        // -------------------------------------------------------------------------
        // MÉTODOS DE CARGA DE MODELOS
        // -------------------------------------------------------------------------
        public PackedScene CargarModeloPersonaje(Personaje personaje)
        {
            try
            {
                string rutaModelo = personaje.ObtenerRutaModelo();
                
                // Intentar obtener de la caché del GameLoader primero
                var cached = GameLoader.Instance?.GetResource<PackedScene>(rutaModelo);
                if (cached != null)
                {
                    Wild.Utils.Logger.LogDebug($"PersonajeManager: Reutilizando modelo precargado de {rutaModelo}");
                    return cached;
                }

                Wild.Utils.Logger.LogInfo($"PersonajeManager: Cargando modelo (fallback) desde {rutaModelo}");
                var escena = GD.Load<PackedScene>(rutaModelo);
                if (escena == null)
                {
                    Wild.Utils.Logger.LogError($"PersonajeManager: No se pudo cargar el modelo {rutaModelo}");
                    return null;
                }
                
                Wild.Utils.Logger.LogInfo($"PersonajeManager: Modelo cargado exitosamente: {rutaModelo}");
                return escena;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en CargarModeloPersonaje(): {ex.Message}");
                return null;
            }
        }

        public Node3D InstanciarPersonaje(Personaje personaje)
        {
            try
            {
                var escenaModelo = CargarModeloPersonaje(personaje);
                if (escenaModelo == null)
                {
                    Wild.Utils.Logger.LogError($"PersonajeManager: No se puede instanciar personaje {personaje.apodo}");
                    return null;
                }
                
                var instancia = escenaModelo.Instantiate<Node3D>();
                
                // Buscar AnimationPlayer para depuración
                var animPlayer = instancia.FindChild("AnimationPlayer", true) as AnimationPlayer;
                if (animPlayer != null)
                {
                    var animList = string.Join(", ", animPlayer.GetAnimationList());
                    Wild.Utils.Logger.LogInfo($"PersonajeManager: Animaciones encontradas: [{animList}]");
                }
                else
                {
                    Wild.Utils.Logger.LogWarning("PersonajeManager: No se encontró AnimationPlayer en el modelo.");
                    Wild.Utils.Logger.LogInfo("PersonajeManager: Estructura del modelo:");
                    LogNodeHierarchy(instancia, 0);
                }

                // Limpiar nodos basura (luces, entornos) que vienen de Blender
                LimpiarEscenaImportada(instancia);

                // Aplicar materiales externos forzadamente para evitar modelos grises
                AplicarMaterialesExternos(instancia, personaje.genero);

                Wild.Utils.Logger.LogInfo($"PersonajeManager: Personaje {personaje.apodo} instanciado exitosamente");
                return instancia;
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en InstanciarPersonaje(): {ex.Message}");
                return null;
            }
        }

        private void AplicarMaterialesExternos(Node3D instancia, string genero)
        {
            try
            {
                // Cargar materiales externos
                string rutaCuerpo = genero == "mujer" ? "res://assets/models/human/fem.tres" : "res://assets/models/human/male.tres";
                Material matCuerpo = GD.Load<Material>(rutaCuerpo);
                Material matOjos = GD.Load<Material>("res://assets/models/human/eyeball.tres");
                Material matMandibula = GD.Load<Material>("res://assets/models/human/jaw.tres");

                Wild.Utils.Logger.LogInfo($"PersonajeManager: Aplicando materiales externos forzados ({genero})");

                // Para los nuevos modelos animados, intentamos ser más selectivos
                AsignarMaterialRecursivo(instancia, matCuerpo, matOjos, matMandibula);
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en AplicarMaterialesExternos: {ex.Message}");
            }
        }

        private void LimpiarEscenaImportada(Node3D instancia)
        {
            try
            {
                // Buscamos luces y WorldEnvironments que sobran
                EliminarNodosPorTipoRecursivo(instancia);
            }
            catch (System.Exception ex)
            {
                Wild.Utils.Logger.LogError($"PersonajeManager: Error en LimpiarEscenaImportada: {ex.Message}");
            }
        }

        private void EliminarNodosPorTipoRecursivo(Node node)
        {
            // Recorremos los hijos en reversa para poder borrar sin problemas de índice
            for (int i = node.GetChildCount() - 1; i >= 0; i--)
            {
                Node child = node.GetChild(i);
                
                if (child is DirectionalLight3D || child is WorldEnvironment || child is OmniLight3D || child is SpotLight3D)
                {
                    Wild.Utils.Logger.LogInfo($"PersonajeManager: Eliminando nodo basura del modelo: {child.Name} ({child.GetType().Name})");
                    child.QueueFree();
                }
                else
                {
                    EliminarNodosPorTipoRecursivo(child);
                }
            }
        }

        private void AsignarMaterialRecursivo(Node node, Material cuerpo, Material ojos, Material mandibula)
        {
            if (node is MeshInstance3D meshInstance)
            {
                // El modelo tiene varias superficies. Intentamos asignar por nombre si es posible, 
                // o por defecto el de cuerpo a todas las superficies que parezcan piel.
                for (int i = 0; i < meshInstance.GetSurfaceOverrideMaterialCount(); i++)
                {
                    Material surfaceMat = meshInstance.Mesh.SurfaceGetMaterial(i);
                    string matName = surfaceMat?.ResourceName.ToLower() ?? "";
                    
                    if (matName.Contains("eye")) 
                        meshInstance.SetSurfaceOverrideMaterial(i, ojos);
                    else if (matName.Contains("jaw")) 
                        meshInstance.SetSurfaceOverrideMaterial(i, mandibula);
                    else if (matName.Contains("skin") || matName.Contains("body") || matName.Contains("cuerpo"))
                        meshInstance.SetSurfaceOverrideMaterial(i, cuerpo);
                    else
                        Wild.Utils.Logger.LogDebug($"PersonajeManager: Manteniendo material original para superficie {i}: {matName}");
                }
            }

            foreach (Node child in node.GetChildren())
            {
                AsignarMaterialRecursivo(child, cuerpo, ojos, mandibula);
            }
        }

        private void LogNodeHierarchy(Node node, int level)
        {
            if (node == null) return;
            string indent = new string(' ', level * 2);
            Wild.Utils.Logger.LogInfo($"{indent}- {node.Name} ({node.GetType().Name})");
            foreach (Node child in node.GetChildren())
            {
                LogNodeHierarchy(child, level + 1);
            }
        }
    }
}

using Godot;
using System;
using Wild.Utils;
using Wild.Core.Terrain;
using Wild.Data;

namespace Wild.Core.Player
{
    /// <summary>
    /// Controlador principal del jugador con física CharacterBody3D.
    /// Implementa movimiento, salto y rotación de cámara.
    /// </summary>
    public partial class JugadorController : CharacterBody3D
    {
        [ExportGroup("Movimiento")]
        [Export] public float VelocidadCaminar = 5.0f;
        [Export] public float VelocidadCorrer = 8.0f;
        [Export] public float FuerzaSalto = 4.5f;
        [Export] public float Gravedad = 9.8f;
        [Export] public float SensibilidadRaton = 0.002f;

        [ExportGroup("Persistencia")]
        public string WorldId { get; set; } = "";
        public string PersonajeId { get; set; } = "";

        [ExportGroup("Referencias")]
        [Export] public Camera3D Camara;
        [Export] public StatsJugador Stats;

        [Export] public bool PhysicsEnabled = true;
        [Export] public float EscalaVisual = 0.11f; // Ajustado: 0.1 era un pelín bajo, probando 0.11
        [Export] public float RotacionVisualY = 180f; 
        [Export] public float AlturaCamara = 1.5f; // Altura de los ojos

        private Node3D _visualsContainer;
        private AnimationPlayer _animationPlayer;
        private Skeleton3D _skeleton;
        private float _proceduralAnimTime = 0f;
        private System.Collections.Generic.Dictionary<string, int> _boneIndices = new();

        [ExportGroup("Animaciones")]
        [Export] public string AnimIdle = "idle";
        [Export] public string AnimWalk = "walk";
        [Export] public float AnimBlendTime = 0.2f;

        [ExportGroup("Animacion Procedural")]
        [Export] public float WalkBobAmplitude = 0.05f;
        [Export] public float WalkBobSpeed = 10f;
        [Export] public float WalkGaitImpulse = 0.5f; // Fuerza del "rebote" al caminar
        [Export] public float LimbSwingAmplitude = 0.4f;
        [Export] public float ArmRelaxAngle = 0.8f; // Reducido de 1.2 para que no se crucen
        
        private float _gaitStepCycle = 0f;
        
        private Vector3 _velocity = Vector3.Zero;
        private float _rotationX = 0f;
        private bool _keepAnimationPlaying = false;
        private float _airTime = 0f;
        private float _stuckTimer = 0f;
        private Vector3 _lastSafePosition;
        private float _safePositionTimer = 0f;
        private float _collisionStuckTimer = 0f;
        private float _physicsDisableTimer = 0f;
        private TerrainManager _cachedTerrainManager;
        private TerrainGenerator _fallbackGenerator;
        private int _lastFallbackSeed = -1;

        public override void _Ready()
        {
            if (Camara == null)
            {
                Camara = GetNodeOrNull<Camera3D>("Camera3D");
            }

            // Crear contenedor para el modelo visual
            _visualsContainer = new Node3D();
            _visualsContainer.Name = "Visuals";
            AddChild(_visualsContainer);

            if (Camara != null)
            {
                Camara.Position = new Vector3(Camara.Position.X, AlturaCamara, Camara.Position.Z);
            }

            Input.MouseMode = Input.MouseModeEnum.Captured;
            
            // Intentar cachear el TerrainManager al inicio
            _cachedTerrainManager = GetTree().Root.FindChild("TerrainManager", true, false) as TerrainManager;
            
            // Aumentar margen de seguridad para evitar atravesar el suelo (0.001 -> 0.01)
            SafeMargin = 0.01f;

            Logger.LogWithContext("PLAYER", "JugadorController inicializado (Gait & SafeMargin enabled)", "INIT");
        }

        public void SetVisualModel(Node3D model)
        {
            if (_visualsContainer == null) return;

            // Limpiar visuales anteriores si los hubiera
            foreach (var child in _visualsContainer.GetChildren())
            {
                child.QueueFree();
            }

            if (model != null)
            {
                _visualsContainer.AddChild(model);
                _visualsContainer.Scale = Vector3.One * EscalaVisual;
                _visualsContainer.RotationDegrees = new Vector3(0, RotacionVisualY, 0);
                
                // Buscar AnimationPlayer recursivamente
                _animationPlayer = model.FindChild("AnimationPlayer", true) as AnimationPlayer;
                
                // Buscar Skeleton3D recursivamente
                _skeleton = model.FindChild("Skeleton3D", true) as Skeleton3D;
                if (_skeleton != null)
                {
                    Logger.LogInfo($"JugadorController: Skeleton3D detectado con {_skeleton.GetBoneCount()} huesos.");
                    MapBones();
                }

                if (_animationPlayer != null)
                {
                    var animList = string.Join(", ", _animationPlayer.GetAnimationList());
                    Logger.LogInfo($"JugadorController: AnimationPlayer detectado. Animaciones: [{animList}]");
                    _animationPlayer.Play(AnimIdle);
                }
                else
                {
                    Logger.LogWarning("JugadorController: NO se encontró AnimationPlayer. Usando fallback esquelético procedural.");
                }

                // Loggear siempre la estructura para ver por qué es invisible
                Logger.LogInfo("JugadorController: Estructura y estado de visibilidad:");
                LogNodeHierarchy(model, 0);

                Logger.LogInfo($"JugadorController: Modelo visual asignado con escala {EscalaVisual} y rotación {RotacionVisualY}.");
            }
        }

        private void LogNodeHierarchy(Node node, int level)
        {
            if (node == null) return;
            string indent = new string(' ', level * 2);
            string extra = "";
            
            if (node is VisualInstance3D vi)
            {
                extra += $" | Visible: {vi.Visible}";
                if (node is MeshInstance3D mi)
                {
                    extra += $" | Mesh: {(mi.Mesh != null ? mi.Mesh.ResourceName : "NULL")}";
                }
            }
            
            Logger.LogInfo($"{indent}- {node.Name} ({node.GetType().Name}){extra}");
            
            foreach (Node child in node.GetChildren())
            {
                LogNodeHierarchy(child, level + 1);
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (!PhysicsEnabled) return;

            if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                // Rotación horizontal (Y) del cuerpo
                RotateY(-mouseMotion.Relative.X * SensibilidadRaton);

                // Rotación vertical (X) de la cámara
                if (Camara != null)
                {
                    _rotationX -= mouseMotion.Relative.Y * SensibilidadRaton;
                    _rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
                    Camara.Rotation = new Vector3(_rotationX, Camara.Rotation.Y, Camara.Rotation.Z);
                }
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (PhysicsEnabled)
            {
                _velocity = Velocity;

                // Aplicar gravedad (Omitir durante el tiempo de eyección/rescate)
                if (!IsOnFloor() && _physicsDisableTimer <= 0)
                {
                    _velocity.Y -= Gravedad * (float)delta;
                }
                else if (_physicsDisableTimer > 0)
                {
                    // Mientras estemos en eyección, mantenemos la velocidad vertical de impulso
                    // sin que la gravedad la tire hacia abajo inmediatamente rompiendo la parábola
                }

                // Manejar salto robusto
                bool jumpPressed = Input.IsActionJustPressed("ui_accept") || Input.IsKeyPressed(Key.Space);
                
                // Si estamos en el suelo o hemos estado en el suelo hace muy poco (tolerancia a atascos suaves)
                if (jumpPressed && (IsOnFloor() || _airTime < 0.1f))
                {
                    _velocity.Y = FuerzaSalto;
                    
                    // Pequeño truco: si saltas, te subimos un pelín por si estás hundido en la malla
                    Position = new Vector3(Position.X, Position.Y + 0.05f, Position.Z);
                    
                    Logger.LogDebug("JugadorController: Salto ejecutado (Impulso preventivo aplicado)");
                }

                // Actualizar contador de tiempo en el aire para coyote time / tolerancia
                if (IsOnFloor()) _airTime = 0;
                else _airTime += (float)delta;

                // Obtener dirección de input (WASD)
                Vector2 inputDir = Vector2.Zero;
                if (Input.IsKeyPressed(Key.W)) inputDir.Y -= 1;
                if (Input.IsKeyPressed(Key.S)) inputDir.Y += 1;
                if (Input.IsKeyPressed(Key.A)) inputDir.X -= 1;
                if (Input.IsKeyPressed(Key.D)) inputDir.X += 1;
                inputDir = inputDir.Normalized();

                Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

                float velocidadActual = (Input.IsActionPressed("ui_shift") || Input.IsKeyPressed(Key.Shift)) ? VelocidadCorrer : VelocidadCaminar;

                if (direction != Vector3.Zero)
                {
                    _velocity.X = direction.X * velocidadActual;
                    _velocity.Z = direction.Z * velocidadActual;

                    // MECANICA DE GAIT (Caminata Humana)
                    // Aplicar pequeños impulsos verticales para simular el paso
                    if (IsOnFloor())
                    {
                        float prevCycle = _gaitStepCycle;
                        _gaitStepCycle += (float)delta * WalkBobSpeed;
                        
                        // En cada paso (frecuencia doble que el ciclo completo)
                        if (Mathf.Sin(prevCycle) < 0 && Mathf.Sin(_gaitStepCycle) >= 0)
                        {
                            // Estamos en el punto de contacto/impulso
                            _velocity.Y = WalkGaitImpulse * (Input.IsActionPressed("ui_shift") ? 1.5f : 1.0f);
                        }
                    }
                }
                else
                {
                    _velocity.X = Mathf.MoveToward(Velocity.X, 0, velocidadActual);
                    _velocity.Z = Mathf.MoveToward(Velocity.Z, 0, velocidadActual);
                    _gaitStepCycle = 0f;
                }

                Velocity = _velocity;
                MoveAndSlide();

                // --- CLAMP MATEMÁTICO DE SEGURIDAD (ANTI-TUNNELING) ---
                // Si move_and_slide falla y atravesamos la malla física, el ruido matemático nos rescata
                float hardGround = GetBestHeight(GlobalPosition.X, GlobalPosition.Z);
                if (hardGround < 999f && GlobalPosition.Y < hardGround - 0.05f) 
                {
                    // Solo corregimos si el error es pequeño (tunneling) o si estamos cayendo al vacío real
                    bool isFallingThrough = GlobalPosition.Y < hardGround - 0.5f;
                    if (isFallingThrough || IsOnFloor() == false)
                    {
                        GlobalPosition = new Vector3(GlobalPosition.X, hardGround, GlobalPosition.Z);
                        _velocity.Y = 0;
                        Velocity = _velocity;
                    }
                }

                // --- SISTEMA DE RESCATE AVANZADO (UNSTUCK V2) ---
                if (_physicsDisableTimer > 0)
                {
                    _physicsDisableTimer -= (float)delta;
                    if (_physicsDisableTimer <= 0)
                    {
                        CollisionLayer = 1; // Restaurar layer
                        CollisionMask = 1;  // Restaurar mask
                        Logger.LogDebug("JugadorController: Físicas restauradas.");
                    }
                }

                // 1. Red de Seguridad Robusta (Anti-Vacío v4)
                if (GlobalPosition.Y < -50.0f || (IsOnFloor() == false && _cachedTerrainManager != null && GlobalPosition.Y < _cachedTerrainManager.GetHeightAt(GlobalPosition.X, GlobalPosition.Z) - 2.0f))
                {
                    float groundHeight = GetBestHeight(GlobalPosition.X, GlobalPosition.Z);
                    
                    if (GlobalPosition.Y < groundHeight - 2.0f || GlobalPosition.Y < -50.0f)
                    {
                        Logger.LogWarning($"JugadorController: RESCATE COORDINADO (Y: {GlobalPosition.Y:F2}). Altura estimada: {groundHeight:F2}");
                        
                        // Teletransportar 30cm sobre el suelo calculado
                        GlobalPosition = new Vector3(GlobalPosition.X, groundHeight + 0.3f, GlobalPosition.Z);
                        
                        _velocity = Vector3.Zero;
                        Velocity = _velocity;
                        _collisionStuckTimer = 0f;
                        return; 
                    }
                }

                // 2. Detección de Bloqueo (Unstuck) con Filtro de Suelo
                bool representsIntent = Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.S) || 
                                       Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.D);
                
                // Analizar si hay colisiones laterales (paredes) ignorando el suelo
                bool hasWallCollision = false;
                for (int i = 0; i < GetSlideCollisionCount(); i++)
                {
                    var col = GetSlideCollision(i);
                    // Si la normal apunta hacia arriba (>0.7), es suelo o rampa suave. 
                    // Si es menor, es una pared o bloqueo lateral.
                    if (col.GetNormal().Y < 0.7f) 
                    {
                        hasWallCollision = true;
                        break;
                    }
                }

                float currentSpeed = Velocity.Length();
                
                // Si hay intención clara de moverse pero estamos bloqueados lateralmente
                if (representsIntent && (hasWallCollision || (currentSpeed < 0.1f && !IsOnFloor())))
                {
                    _collisionStuckTimer += (float)delta;
                    
                    if (_collisionStuckTimer > 1.2f) // Un poco más de margen
                    {
                        Logger.LogWarning("JugadorController: Bloqueo lateral persistente. Ejecutando eyección.");
                        
                        CollisionLayer = 0;
                        CollisionMask = 0;
                        _physicsDisableTimer = 0.3f;

                        Vector3 backward = -Transform.Basis.Z;
                        Vector3 ejectionVector = (backward + Vector3.Up * 2.0f).Normalized();
                        _velocity = ejectionVector * FuerzaSalto * 1.2f;
                        Position += (Vector3.Up * 0.5f) + (backward * 0.3f);
                        
                        _collisionStuckTimer = 0f;
                    }
                }
                else
                {
                    _collisionStuckTimer = Mathf.MoveToward(_collisionStuckTimer, 0, (float)delta * 2.0f);
                    
                    // Guardar posición segura periódicamente
                    if (IsOnFloor() && _physicsDisableTimer <= 0)
                    {
                        _safePositionTimer += (float)delta;
                        if (_safePositionTimer > 1.0f)
                        {
                            _lastSafePosition = GlobalPosition;
                            _safePositionTimer = 0f;
                        }
                    }
                }
            }
            
            UpdateAnimations();
        }

        private void UpdateAnimations()
        {
            float delta = (float)GetPhysicsProcessDeltaTime();
            
            // Calcular velocidad horizontal real o forzada por Free Cam
            Vector2 horizontalVelocity = new Vector2(Velocity.X, Velocity.Z);
            bool isMoving = (horizontalVelocity.Length() > 0.1f && IsOnFloor()) || (!PhysicsEnabled && _keepAnimationPlaying);

            if (_animationPlayer != null && _animationPlayer.HasAnimation(AnimWalk))
            {
                if (isMoving)
                    PlayAnimation(AnimWalk);
                else
                    PlayAnimation(AnimIdle);
            }
            else if (_skeleton != null)
            {
                UpdateSkeletalAnimations(delta, isMoving);
            }
        }

        private void MapBones()
        {
            if (_skeleton == null) return;
            _boneIndices.Clear();
            
            for (int i = 0; i < _skeleton.GetBoneCount(); i++)
            {
                string name = _skeleton.GetBoneName(i).ToLower();
                _boneIndices[name] = i;
                Logger.LogDebug($"JugadorController: Hueso detectado: {name} (idx: {i})");
            }
        }

        private void UpdateSkeletalAnimations(float delta, bool isMoving)
        {
            if (_skeleton == null) return;

            if (isMoving)
            {
                // Sincronizar con el ciclo físico de pasos
                _proceduralAnimTime = _gaitStepCycle;
                float swing = Mathf.Sin(_proceduralAnimTime) * LimbSwingAmplitude;
                float bob = Mathf.Abs(Mathf.Cos(_proceduralAnimTime)) * WalkBobAmplitude;

                // Aplicar movimiento a las piernas (oscilación eje X)
                RotateBoneIfExists("thigh.l", swing);
                RotateBoneIfExists("shin.l", swing * 0.3f); 
                RotateBoneIfExists("thigh.r", -swing);
                RotateBoneIfExists("shin.r", -swing * 0.3f);

                // Oscilación de brazos y muñecas
                RotateBoneIfExists("upper_arm.l", swing * 0.8f, true);
                RotateBoneIfExists("forearm.l", swing * 0.4f, true);
                RotateBoneIfExists("hand.l", 0.0f, true);
                RotateBoneIfExists("upper_arm.r", swing * 0.8f, true); 
                RotateBoneIfExists("forearm.r", swing * 0.4f, true); 
                RotateBoneIfExists("hand.r", 0.0f, true);

                // Bobbing vertical (Y)
                _visualsContainer.Position = new Vector3(0, -bob, 0);
            }
            else
            {
                // Resetear suavemente
                _proceduralAnimTime = Mathf.Lerp(_proceduralAnimTime, 0, delta * 5f);
                _visualsContainer.Position = _visualsContainer.Position.Lerp(Vector3.Zero, delta * 5f);
                
                // Resetear cada hueso a su pose de descanso (rest pose) de forma suave
                foreach (int idx in _boneIndices.Values)
                {
                    string name = _skeleton.GetBoneName(idx).ToLower();
                    Quaternion restRot = _skeleton.GetBoneRest(idx).Basis.GetRotationQuaternion();
                    Quaternion targetRot = restRot;

                    // Si es un brazo o mano, mantener la pose HUD de supervivencia
                    if (name.Contains("upper_arm") || name.Contains("forearm") || name.Contains("hand"))
                    {
                        float side = name.Contains(".l") ? -1.0f : 1.0f;
                        bool isUpperArm = name.Contains("upper_arm");
                        bool isForearm = name.Contains("forearm");
                        bool isHand = name.Contains("hand");
                        
                        // --- DOCUMENTACION DE ROTACIONES EMPIRICAS EN ESTE RIG ---
                        // UPPER ARM (Hombro): 
                        //   X neg (-): Hombro cae hacia abajo (hacia el cuerpo). Pitch negativo.
                        //   X pos (+): Hombro sube (menos util).
                        //   Y: Roll del hombro sobre su propio eje (no usado todavia).
                        //   Z (con sideZ): Balancea el brazo hacia adelante/atras. Su eje principal de swing.
                        // FOREARM (Codo):
                        //   X neg (-): El codo se dobla hacia ATRAS (fuera de la pantalla). No sirve.
                        //   X pos (+): El codo abre hacia los lados como "alas de pollo". No sirve.
                        //   Y: Roll del antebrazo sobre su eje longitudinal. Util para compensar retorcimiento.
                        //   Z (con sideZ): La UNICA bisagra que dobla el codo hacia ADELANTE.
                        //      Efecto secundario: retuerce la malla del antebrazo (papel caramelo).
                        //   Y + sideY (mismo signo que sideZ): Dobla el brazo hacia ATRAS (manos en caderas). Mal.
                        //   Y + sideY (signo OPUESTO a sideZ): Reduce el retorcimiento! Pero a 1.5 flarea el brazo.
                        //      -> La amplitud de Y hay que calibrarla. Probamos Y=-0.7 (sideY invertido).
                        // HAND (Muneca):
                        //   Z (con sideZ): Gira-tuerce la muneca. Puede compensar el papel de caramelo del antebrazo.
                        //   X: Flexion/Extension de la muneca (arriba/abajo). A explorar.
                        //   Y: Desviacion radial/cubital de la muneca (lateral). A explorar.
                        // ---------------------------------------------------------
                        
                        // Paso 1: Bajar brazos relajados (X) + swing forward (Z) en el upper arm.
                        // Las manos solo se ven mirando hacia abajo: necesitamos menos drop(X) y mas forward(Z).
                        // FPS survival target: brazos a nivel del pecho, visibles mirando recto.
                        float xRot = isUpperArm ? -0.3f : 0.0f;  // Reducido: menos caida
                        float zRotUpperArm = isUpperArm ? 1.0f : 0.0f; // 0.8->1.0: mas avance hacia la camara
                        
                        // Paso 2: Bisagra del codo con los 3 ejes.
                        float zRot = isForearm ? 0.7f : 0.0f;
                        float yRotForearm = isForearm ? -0.5f : 0.0f;
                        float xRotForearm = isForearm ? -0.3f : 0.0f;
                        
                        float sideZMultiplier = name.Contains(".l") ? -1.0f : 1.0f;
                        float sideYMultiplier = name.Contains(".l") ? 1.0f : -1.0f;
                        
                        // Paso 3: Mano. Todos a 0 como baseline. Iremos ajustando uno a uno.
                        // zRotHand: tuerce la muneca (roll). xRotHand: flexion muneca. yRotHand: desviacion lateral.
                        float zRotHand = 0.0f;
                        float xRotHand = 0.0f;
                        float yRotHand = 0.0f;
                        
                        Quaternion relaxX = new Quaternion(new Vector3(1, 0, 0), xRot + xRotForearm + xRotHand);
                        Quaternion relaxY = new Quaternion(new Vector3(0, 1, 0), (yRotForearm + yRotHand) * sideYMultiplier);
                        Quaternion relaxZ = new Quaternion(new Vector3(0, 0, 1), (zRot + zRotHand + zRotUpperArm) * sideZMultiplier);
                        
                        targetRot = restRot * relaxX * relaxY * relaxZ;
                    }

                    Quaternion current = _skeleton.GetBonePoseRotation(idx);
                    _skeleton.SetBonePoseRotation(idx, current.Slerp(targetRot, delta * 5f));
                }
            }
        }

        private void RotateBoneIfExists(string partialName, float angle, bool isArm = false)
        {
            foreach (var kvp in _boneIndices)
            {
                if (kvp.Key.Contains(partialName))
                {
                    int boneIdx = kvp.Value;
                    Quaternion restRotation = _skeleton.GetBoneRest(boneIdx).Basis.GetRotationQuaternion();
                    
                    // Rotación de balanceo (eje X para piernas, eje Z para brazos)
                    Quaternion swingRot = new Quaternion(isArm ? new Vector3(0, 0, 1) : new Vector3(1, 0, 0), angle);
                    
                    if (isArm)
                    {
                        // Pose HUD de supervivencia
                        float side = kvp.Key.Contains(".l") ? -1.0f : 1.0f;
                        bool isUpperArm = kvp.Key.Contains("upper_arm");
                        bool isForearm = kvp.Key.Contains("forearm");
                        bool isHand = kvp.Key.Contains("hand");
                        
                        // Resumen de ejes: Hombro baja en X, Codo (Z + Y opuesto sideY), Mano (0)
                        float xRot = isUpperArm ? -0.3f : 0.0f;
                        float zRotUpperArm = isUpperArm ? 1.0f : 0.0f;
                        
                        float zRot = isForearm ? 0.7f : 0.0f; 
                        float yRotForearm = isForearm ? -0.5f : 0.0f;
                        float xRotForearm = isForearm ? -0.3f : 0.0f;
                        
                        float sideZMultiplier = kvp.Key.Contains(".l") ? -1.0f : 1.0f;
                        float sideYMultiplier = kvp.Key.Contains(".l") ? 1.0f : -1.0f;
                        
                        float zRotHand = 0.0f; 
                        float xRotHand = 0.0f;
                        float yRotHand = 0.0f; 
                        
                        Quaternion relaxX = new Quaternion(new Vector3(1, 0, 0), xRot + xRotForearm + xRotHand);
                        Quaternion relaxY = new Quaternion(new Vector3(0, 1, 0), (yRotForearm + yRotHand) * sideYMultiplier);
                        Quaternion relaxZ = new Quaternion(new Vector3(0, 0, 1), (zRot + zRotHand + zRotUpperArm) * sideZMultiplier);
                        
                        _skeleton.SetBonePoseRotation(boneIdx, restRotation * relaxX * relaxY * relaxZ * swingRot);
                    }
                    else
                    {
                        _skeleton.SetBonePoseRotation(boneIdx, restRotation * swingRot);
                    }
                }
            }
        }

        private void PlayAnimation(string animName)
        {
            if (_animationPlayer == null || string.IsNullOrEmpty(animName)) return;
            
            if (_animationPlayer.CurrentAnimation != animName)
            {
                if (_animationPlayer.HasAnimation(animName))
                {
                    _animationPlayer.Play(animName, AnimBlendTime);
                }
            }
        }

        public Vector3 GetGlobalPosition() => GlobalPosition;

        /// <summary>
        /// Obtiene la mejor altura disponible del terreno (vía Manager o Fallback).
        /// </summary>
        private float GetBestHeight(float x, float z)
        {
            // Intentar vía TerrainManager (colisiones reales o ruido cacheado)
            if (_cachedTerrainManager != null && IsInstanceValid(_cachedTerrainManager))
            {
                return _cachedTerrainManager.GetHeightAt(x, z);
            }

            // Fallback: Re-buscar manager o usar generador local
            _cachedTerrainManager = GetTree().Root.FindChild("TerrainManager", true, false) as TerrainManager;
            if (_cachedTerrainManager != null)
            {
                return _cachedTerrainManager.GetHeightAt(x, z);
            }

            // Fallback Extremo: Generador matemático local
            EnsureFallbackGenerator();
            if (_fallbackGenerator != null)
            {
                return _fallbackGenerator.GetNoiseHeight(x, z);
            }

            return 1000f; // Último recurso si nada funciona
        }

        private void EnsureFallbackGenerator()
        {
            var mundoActual = MundoManager.Instance.ObtenerMundoActual();
            int currentSeed = mundoActual?.GetSeedInt() ?? 12345;

            if (_fallbackGenerator == null || _lastFallbackSeed != currentSeed)
            {
                _lastFallbackSeed = currentSeed;
                var noise = new NoiseGenerator(_lastFallbackSeed);
                var biomas = new Wild.Core.Biomes.BiomaManager(_lastFallbackSeed);
                _fallbackGenerator = new TerrainGenerator(noise, biomas);
                Logger.LogInfo($"JugadorController: Generador de respaldo inicializado (Seed: {_lastFallbackSeed})");
            }
        }

        public void SetFrozen(bool frozen)
        {
            PhysicsEnabled = !frozen;
            if (frozen)
            {
                // Capturar si estaba moviéndose al activar Free Cam
                Vector2 horizontalVelocity = new Vector2(Velocity.X, Velocity.Z);
                _keepAnimationPlaying = horizontalVelocity.Length() > 0.1f;
                
                Velocity = Vector3.Zero;
                Logger.LogInfo($"JugadorController: Jugador congelado. Mantener anim: {_keepAnimationPlaying}");
            }
            else
            {
                _keepAnimationPlaying = false;
                Logger.LogInfo("JugadorController: Jugador descongelado.");
            }
        }
    }
}

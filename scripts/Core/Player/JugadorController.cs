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
        [Export] public float RotacionVisualY = 180f; 
        
        private IModeloConfig _modeloConfig;
        public string TipoPersonaje { get; set; } = "hombre1";

        private Node3D _visualsContainer;
        private AnimationPlayer _animationPlayer;
        private AnimationTree _animationTree;
        private AnimationNodeStateMachinePlayback _stateMachinePlayback;
        private Skeleton3D _skeleton;
        private Vector2 _lastInputMotion = Vector2.Zero;

        [ExportGroup("Animaciones")]
        [Export] public string AnimIdle = "idle";
        [Export] public string AnimWalk = "walk";
        [Export] public string AnimJump = "jump";
        [Export] public float AnimBlendTime = 0.2f;
        
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
                UpdateCameraPosition();
                // Ocultar Capa 2 (Cabeza) para la cámara del jugador (Capa 2 = bit index 1)
                Camara.CullMask &= ~(1u << 1); 
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
                _modeloConfig = ModeloRegistry.GetConfig(TipoPersonaje);
                
                _visualsContainer.AddChild(model);
                _visualsContainer.Scale = Vector3.One * _modeloConfig.EscalaBase;
                _visualsContainer.RotationDegrees = new Vector3(0, RotacionVisualY, 0);
                
                model.Visible = true;
                if (model is Node3D n3d) n3d.Show();

                UpdateCameraPosition();

                _animationPlayer = model.FindChild("AnimationPlayer", true) as AnimationPlayer;
                _skeleton = model.FindChild("Skeleton3D", true) as Skeleton3D;
                if (_skeleton != null)
                {
                    Logger.LogInfo($"JugadorController: Skeleton3D detectado con {_skeleton.GetBoneCount()} huesos.");
                }

                if (_animationPlayer != null)
                {
                    _animationPlayer.PlaybackActive = true;
                    var animList = _animationPlayer.GetAnimationList();
                    foreach (var anim in animList)
                    {
                        string animLower = anim.ToString().ToLower();
                        if (animLower.Contains("walk") || animLower.Contains("run")) AnimWalk = anim;
                        if (animLower.Contains("idle")) AnimIdle = anim;
                        if (animLower.Contains("jump")) AnimJump = anim;
                    }

                    _animationTree = model.FindChild("AnimationTree", true, false) as AnimationTree;
                    if (_animationTree == null) _animationTree = model.GetParent()?.FindChild("AnimationTree", true, false) as AnimationTree;
                    
                    if (_animationTree != null)
                    {
                        _animationTree.Active = true;
                        _stateMachinePlayback = _animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
                        Logger.LogInfo($"JugadorController: AnimationTree '{_animationTree.Name}' detectado. Playback: {(_stateMachinePlayback != null ? "OK" : "NULL")}");
                    }
                    
                    if (_animationTree == null)
                    {
                        ForzarLoopsAnimacion(_animationPlayer);
                        if (_animationPlayer.HasAnimation(AnimIdle)) _animationPlayer.Play(AnimIdle);
                    }
                }

                Logger.LogInfo($"JugadorController: Modelo visual asignado ({TipoPersonaje}).");
                _modeloConfig.AplicarConfiguracion(model);
            }
        }

        private void UpdateCameraPosition()
        {
            if (Camara == null || _modeloConfig == null) return;
            
            float height = _modeloConfig.AlturaCamara;
            float offset = _modeloConfig.OffsetCamaraFrontal;
            
            Camara.Position = new Vector3(Camara.Position.X, height, -offset);
        }

        private void ForzarLoopsAnimacion(AnimationPlayer player)
        {
            foreach (var aName in player.GetAnimationList())
            {
                if (aName.ToString().ToLower().Contains("walk") || aName.ToString().ToLower().Contains("idle"))
                {
                    foreach (var libName in player.GetAnimationLibraryList())
                    {
                        var lib = player.GetAnimationLibrary(libName);
                        if (lib != null && lib.HasAnimation(aName))
                        {
                            var anim = lib.GetAnimation(aName).Duplicate() as Animation;
                            if (anim != null)
                            {
                                anim.LoopMode = Animation.LoopModeEnum.Linear;
                                lib.RemoveAnimation(aName);
                                lib.AddAnimation(aName, anim);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Solución True First Person: duplica el material de las mallas (solo para jugador local)
        /// y activa el Backface Culling (CullMode.Back). Esto hace que las caras interiores
        /// de la piel no se rendericen, permitiendo que la cámara dentro de la cabeza
        /// no vea la textura por dentro, pero manteniendo los brazos visibles.
        /// </summary>

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
                    
                    // Disparar animación de salto (A: Player)
                    if (_animationTree == null && _animationPlayer != null && _animationPlayer.HasAnimation(AnimJump))
                        _animationPlayer.Play(AnimJump);

                    // Pequeño truco: si saltas, te subimos un pelín por si estás hundido en la malla
                    Position = new Vector3(Position.X, Position.Y + 0.05f, Position.Z);
                    
                    Logger.LogDebug("JugadorController: Salto ejecutado (Impulso preventivo aplicado)");
                }

                // Actualizar contador de tiempo en el aire para coyote time / tolerancia
                if (IsOnFloor()) _airTime = 0;
                else _airTime += (float)delta;

                Vector2 inputDir = Vector2.Zero;
                if (Input.IsKeyPressed(Key.W)) inputDir.Y -= 1;
                if (Input.IsKeyPressed(Key.S)) inputDir.Y += 1;
                if (Input.IsKeyPressed(Key.A)) inputDir.X -= 1;
                if (Input.IsKeyPressed(Key.D)) inputDir.X += 1;
                inputDir = inputDir.Normalized();
                _lastInputMotion = inputDir;

                Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

                float velocidadActual = (Input.IsActionPressed("ui_shift") || Input.IsKeyPressed(Key.Shift)) ? VelocidadCorrer : VelocidadCaminar;

                if (direction != Vector3.Zero)
                {
                    _velocity.X = direction.X * velocidadActual;
                    _velocity.Z = direction.Z * velocidadActual;
                }
                else
                {
                    _velocity.X = Mathf.MoveToward(Velocity.X, 0, velocidadActual);
                    _velocity.Z = Mathf.MoveToward(Velocity.Z, 0, velocidadActual);
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
            // isMoving: Usamos el INPUT en lugar de la velocidad física para evitar "cortes" al chocar con paredes (Punto 1)
            bool isMoving = _lastInputMotion.Length() > 0.05f || (!PhysicsEnabled && _keepAnimationPlaying);

            // Temporizador sutil de log para no saturar
            if (isMoving && Engine.GetProcessFrames() % 60 == 0)
                Logger.LogDebug($"JugadorController Animation: isMoving={isMoving} input={_lastInputMotion} (Tree:{_animationTree != null})");

            // Opción A: Tenemos un AnimationTree con StateMachine → usar Travel() (más robusto que condiciones)
            if (_animationTree != null && _animationTree.Active && _stateMachinePlayback != null)
            {
                string targetState;
                if (!IsOnFloor())
                    targetState = "jump";
                else if (isMoving)
                    targetState = "walk";
                else
                    targetState = "idle";

                // Travel() navega al estado destino usando las transiciones existentes
                if (_stateMachinePlayback.GetCurrentNode() != targetState)
                    _stateMachinePlayback.Travel(targetState);

                return;
            }

            // Opción B: Control manual por AnimationPlayer (Fallback robusto)
            if (_animationPlayer != null && _animationPlayer.HasAnimation(AnimWalk))
            {
                if (isMoving)
                    PlayAnimation(AnimWalk);
                else
                    PlayAnimation(AnimIdle);
                
                return;
            }

            // Opción C: Animación Procedural (Respaldo total para modelos sin animaciones)
            if (_modeloConfig != null && _skeleton != null)
            {
                _modeloConfig.ActualizarAnimacionProcedural(_visualsContainer, _skeleton, (float)GetProcessDeltaTime(), isMoving);
            }
        }

        private void PlayAnimation(string animName)
        {
            if (_animationPlayer == null || string.IsNullOrEmpty(animName)) return;
            
            // Si es una animación distinta, o si es la misma pero se ha detenido (no era loop)
            if (_animationPlayer.CurrentAnimation != animName || !_animationPlayer.IsPlaying())
            {
                if (_animationPlayer.HasAnimation(animName))
                {
                    // Intento de forzar loop en animaciones de movimiento/reposo. 
                    // Si el recurso es read-only (típico de .glb), fallará silenciosamente,
                    // pero el IsPlaying() de arriba actuará como salvavidas reiniciándola cada vez que acabe.
                    var anim = _animationPlayer.GetAnimation(animName);
                    if (anim != null && (animName.Contains("walk") || animName.Contains("idle") || animName.Contains("run")))
                    {
                        if (anim.LoopMode == Animation.LoopModeEnum.None)
                        {
                            try { anim.LoopMode = Animation.LoopModeEnum.Linear; } catch {}
                        }
                    }

                    // Se reproduce de forma inmediata.
                    _animationPlayer.Play(animName);
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

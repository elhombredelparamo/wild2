// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Pantalla de Inicio
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - contexto/menus-y-escenas.md - Sistema de menús y escenas
// - codigo/core/ui.pseudo - Diseño de interfaz de usuario
// - contexto/calidad.md - Adaptación a niveles de calidad
// 
// DESCRIPCIÓN:
// Pantalla de inicio con fade automático, pantalla completa forzada y
// transición al menú principal. Soporta imágenes responsive y saltar.
// 
// -----------------------------------------------------------------------------
using Godot;
using System;

/// <summary>
/// Splash screen inicial con fade automático y opción de saltar
/// </summary>
public partial class SplashScreen : Control
{
    private TextureRect _splashImage = null!;
    private Godot.Timer _timer = null!;
    private const float FadeDuration = 1.0f;
    private const float DisplayDuration = 3.0f;
    
    public override void _Ready()
    {
        // Inicializar sistema de logging
        Wild.Utils.Logger.Inicializar();
        Wild.Utils.Logger.LogInfo("[SYSTEM] Aplicando configuración de pantalla...");
        
        int targetW = Wild.Data.SessionData.Instance.ResolutionWidth;
        int targetH = Wild.Data.SessionData.Instance.ResolutionHeight;
        
        if (targetW <= 0 || targetH <= 0)
        {
            // Auto-detectar nativa si no hay guardada
            Vector2I nativeSize = DisplayServer.ScreenGetSize();
            targetW = nativeSize.X;
            targetH = nativeSize.Y;
            Wild.Utils.Logger.LogInfo($"[SYSTEM] No hay resolución guardada. Usando nativa: {targetW}x{targetH}");
        }
        else
        {
            Wild.Utils.Logger.LogInfo($"[SYSTEM] Cargando resolución guardada: {targetW}x{targetH}");
        }

        // Implementar escalado proporcional al inicio
        float scaleFactor = (float)targetH / 720.0f;
        GetWindow().ContentScaleSize = new Vector2I(targetW, targetH);
        GetWindow().ContentScaleFactor = scaleFactor;
        GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
        GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Keep;
        
        Vector2I screenSize = DisplayServer.ScreenGetSize();
        var mode = (targetW == screenSize.X && targetH == screenSize.Y) 
            ? DisplayServer.WindowMode.Fullscreen 
            : DisplayServer.WindowMode.ExclusiveFullscreen;

        DisplayServer.WindowSetMode(mode);
        DisplayServer.WindowSetSize(new Vector2I((int)screenSize.X, (int)screenSize.Y));
        
        Wild.Utils.Logger.LogInfo($"[SYSTEM] Pantalla configurada: {targetW}x{targetH} (Escala UI: {scaleFactor:F2})");
        
        // Verificar estados reales
        var renderRes = GetWindow().ContentScaleSize;
        var windowRes = DisplayServer.WindowGetSize();
        Wild.Utils.Logger.LogInfo($"[MOTOR: GODOT] Renderizado Init: {renderRes.X}x{renderRes.Y} | Ventana Física: {windowRes.X}x{windowRes.Y}");
        
        // Configurar pantalla completa
        AnchorLeft = 0;
        AnchorTop = 0;
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetLeft = 0;
        OffsetTop = 0;
        OffsetRight = 0;
        OffsetBottom = 0;

        // Crear imagen del splash
        _splashImage = new TextureRect();
        _splashImage.AnchorLeft = 0;
        _splashImage.AnchorTop = 0;
        _splashImage.AnchorRight = 1;
        _splashImage.AnchorBottom = 1;
        _splashImage.OffsetLeft = 0;
        _splashImage.OffsetTop = 0;
        _splashImage.OffsetRight = 0;
        _splashImage.OffsetBottom = 0;
        _splashImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _splashImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        AddChild(_splashImage);

        var splashTexture = GD.Load<Texture2D>("res://assets/ui/splash.png");
        if (splashTexture != null)
        {
            _splashImage.Texture = splashTexture;
        }

        // Configurar timer para el display
        _timer = new Godot.Timer();
        _timer.WaitTime = DisplayDuration;
        _timer.OneShot = true;
        _timer.Timeout += TransitionToMainMenu;
        AddChild(_timer);
        
        // Iniciar con fade-in
        Modulate = Colors.Transparent;
        CreateTween().TweenProperty(this, "modulate", Colors.White, FadeDuration);
        
        // Iniciar el timer después del fade-in
        GetTree().CreateTimer(FadeDuration).Timeout += () => {
            _timer.Start();
        };
    }
    
    public override void _Input(InputEvent @event)
    {
        // Permitir saltar el splash con cualquier tecla o clic
        if (@event is InputEventKey or InputEventMouseButton)
        {
            if (@event.IsPressed())
            {
                TransitionToMainMenu();
            }
        }
    }
    
    private void TransitionToMainMenu()
    {
        // Fade-out y transición al menú principal
        CreateTween().TweenProperty(this, "modulate", Colors.Transparent, FadeDuration);
        GetTree().CreateTimer(FadeDuration).Timeout += () => {
            try
            {
                GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Error cambiando a main_menu.tscn: {ex.Message}");
                GetTree().Quit();
            }
        };
    }
}

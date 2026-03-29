@tool
extends SceneTree

# Script de diagnóstico para verificar el recurso Skin y el Skeleton binding en Godot 4.
# Uso: godot --headless -s scripts/debug_skin.gd

func _init():
	print("\n--- INICIANDO DIAGNÓSTICO DE SKIN Y SKELETON ---")
	var scene_path = "res://scenes/player/animada_con_tree.tscn"
	var scene = load(scene_path)
	
	if not scene:
		printerr("ERROR: No se pudo cargar la escena en " + scene_path)
		quit(1)
		return

	var instance = scene.instantiate()
	inspect_node(instance, 0)
	
	instance.queue_free()
	print("--- DIAGNÓSTICO FINALIZADO ---\n")
	quit()

func inspect_node(node: Node, level: int):
	var indent = ""
	for i in range(level): indent += "  "
	
	var info = indent + "- " + node.name + " (" + node.get_class() + ")"
	
	if node is MeshInstance3D:
		var has_skin = node.skin != null
		var skeleton_path = node.skeleton
		var skeleton_node = node.get_node_or_null(skeleton_path) if skeleton_path else null
		
		info += " [MESH]"
		info += " | Skin: " + ("OK" if has_skin else "MISSING!")
		info += " | SkeletonPath: " + str(skeleton_path)
		info += " | SkeletonNode: " + ("FOUND" if skeleton_node else "NOT FOUND")
		
		if node.mesh:
			info += " | Surfacews: " + str(node.mesh.get_surface_count())
			for i in range(node.mesh.get_surface_count()):
				var mat = node.get_surface_override_material(i)
				if not mat: mat = node.mesh.surface_get_material(i)
				info += "\n" + indent + "    Surface " + str(i) + " Mat: " + (mat.resource_name if mat and mat.resource_name != "" else str(mat))

	elif node is Skeleton3D:
		info += " [SKELETON] | Bones: " + str(node.get_bone_count())

	elif node is AnimationTree:
		info += " [ANIM_TREE] | Active: " + str(node.active) + " | RootMotion: " + str(node.root_motion_track)

	print(info)
	
	for child in node.get_children():
		inspect_node(child, level + 1)

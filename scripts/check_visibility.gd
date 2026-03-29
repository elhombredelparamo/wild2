@tool
extends SceneTree

func _init():
	var scene = load("res://assets/models/human/animada.glb")
	if scene:
		var instance = scene.instantiate()
		var mesh_node = instance.find_child("char1", true, false)
		if mesh_node and mesh_node is MeshInstance3D:
			print("Skeleton path: " + str(mesh_node.skeleton))
			if mesh_node.skeleton:
				var skeleton_node = mesh_node.get_node_or_null(mesh_node.skeleton)
				print("Resolved Skeleton Node: " + str(skeleton_node))
			else:
				print("No skeleton path!")
			
			if mesh_node.skin:
				print("Skin resource found!")
			else:
				print("Skin resource MISSING!")
		else:
			print("Mesh 'char1' not found anywhere in GLB!")
			
		instance.queue_free()
	else:
		printerr("Error loading GLB.")
	quit()

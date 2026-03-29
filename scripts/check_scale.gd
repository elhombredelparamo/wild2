@tool
extends SceneTree

func _init():
	var scene = load("res://scenes/player/animada_con_tree.tscn")
	if scene:
		var instance = scene.instantiate()
		var mesh_instance = null
		
		# Find the mesh instance manually based on the structure we saw
		var armature = instance.get_node("Armature")
		if armature:
			var skeleton = armature.get_node("GeneralSkeleton")
			if skeleton:
				mesh_instance = skeleton.get_node("char1")
				
		if mesh_instance and mesh_instance is MeshInstance3D:
			print("Mesh found!")
			var aabb = mesh_instance.get_aabb()
			print("Local AABB Size: " + str(aabb.size))
			
			# Calculate global AABB considering the Armature transform
			var global_transform = mesh_instance.global_transform
			print("Armature Scale: " + str(armature.scale))
			print("Global AABB Size (approx): " + str(aabb.size * armature.scale))
		else:
			print("Mesh 'char1' not found in expected path.")
		
		instance.queue_free()
	else:
		printerr("Error loading scene.")
	quit()

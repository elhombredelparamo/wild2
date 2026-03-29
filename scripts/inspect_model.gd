extends SceneTree

func _init():
	print("Iniciando inspección del modelo...")
	var scene = load("res://scenes/player/animada_con_tree.tscn")
	if scene:
		var instance = scene.instantiate()
		print_tree(instance, 0)
		instance.queue_free()
		print("Inspección finalizada.")
	else:
		printerr("Error: No se pudo cargar la escena.")
	quit()

func print_tree(node: Node, level: int):
	var indent = ""
	for i in range(level * 2):
		indent += " "
	
	print(indent + "- " + node.name + " (" + node.get_class() + ")")
	
	if node is MeshInstance3D:
		var mesh_name = "null"
		if node.mesh:
			mesh_name = node.mesh.resource_name
			if mesh_name == "":
				mesh_name = "Unnamed Mesh"
		
		print(indent + "  Mesh: " + mesh_name)
		print(indent + "  Layers: " + str(node.layers))
		
		if node.mesh:
			for i in range(node.mesh.get_surface_count()):
				var mat = node.mesh.surface_get_material(i)
				if mat:
					print(indent + "  Surface " + str(i) + " Material: " + mat.resource_name)
				else:
					print(indent + "  Surface " + str(i) + " Material: null")
	
	for child in node.get_children():
		print_tree(child, level + 1)

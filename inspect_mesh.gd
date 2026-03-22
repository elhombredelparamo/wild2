extends SceneTree

func _init():
	var mesh = Mesh.new()
	print("Inspecting Mesh methods:")
	for method in mesh.get_method_list():
		var name = method["name"]
		if "surface" in name.to_lower() and "name" in name.to_lower():
			print("Found method: ", name)
	quit()

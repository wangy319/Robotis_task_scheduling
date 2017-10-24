package objects;

import java.util.ArrayList;

public class Component extends AssemblyObject {
	public ArrayList<Component> subcomponents;

	public Component(String id) {
		super(id);
		subcomponents = new ArrayList<>();
	}

	public void addSubcomponent(Component component) {
		subcomponents.add(component);
	}

	public ArrayList<String> getSubComponents() {
		ArrayList<String> out = new ArrayList<>();
		if (subcomponents.isEmpty()) {
			out.add(id);
		} else {
			for (Component c : subcomponents) {
				out.addAll(c.getSubComponents());
				out.add(c.id);
			}
		}
		return out;
	}

}
